using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;

namespace Ocultum.Infrastructure.Steghide;

public sealed class SteghideClient : ISteghideClient, IDisposable
{
    private const string MaskedPassphrase = "********";

    private readonly SteghideOptions _options;
    private readonly ILogger<SteghideClient> _logger;
    private readonly SemaphoreSlim _concurrency;

    public SteghideClient(IOptions<SteghideOptions> options, ILogger<SteghideClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _concurrency = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrency));
    }

    public async Task<SteghideInfo> InfoAsync(string workDir, string fileName, string passphrase, CancellationToken cancellationToken)
    {
        // Passing -p also keeps Steghide from prompting on the terminal, which would block a redirected stdin.
        var result = await RunAsync(workDir, ["info", fileName, "-p", passphrase], cancellationToken);

        if (SteghideOutputParser.TryParseInfo(result.StdOut, out var format, out var capacity, out var embedded))
        {
            return new SteghideInfo(format, capacity, embedded, result.CommandLine);
        }

        throw SteghideErrorMapper.Map(result);
    }

    public async Task<SteghideCommandResult> EmbedAsync(
        string workDir,
        string coverFile,
        string payloadFile,
        string stegoFile,
        EmbedOptions options,
        CancellationToken cancellationToken)
    {
        var args = new List<string> { "embed", "-cf", coverFile, "-ef", payloadFile, "-sf", stegoFile, "-p", options.Passphrase };

        if (options.Cipher is not null)
        {
            args.Add("-e");
            args.Add(options.Cipher.ToLowerInvariant());
            if (options.Mode is not null && !options.Mode.Equals("stream", StringComparison.OrdinalIgnoreCase)
                && !options.Cipher.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                args.Add(options.Mode.ToLowerInvariant());
            }
        }

        if (options.CompressionLevel is 0)
        {
            args.Add("-Z");
        }
        else if (options.CompressionLevel is { } level)
        {
            args.Add("-z");
            args.Add(level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!options.EmbedChecksum)
        {
            args.Add("-K");
        }

        if (!options.EmbedFileName)
        {
            args.Add("-N");
        }

        args.Add("-f");
        args.Add("-q");

        return await RunCheckedAsync(workDir, args, cancellationToken);
    }

    public async Task<SteghideCommandResult> ExtractAsync(
        string workDir,
        string stegoFile,
        string outputFile,
        string passphrase,
        CancellationToken cancellationToken)
    {
        return await RunCheckedAsync(
            workDir,
            ["extract", "-sf", stegoFile, "-xf", outputFile, "-p", passphrase, "-f", "-q"],
            cancellationToken);
    }

    public async Task<IReadOnlyList<CipherInfo>> GetCiphersAsync(CancellationToken cancellationToken)
    {
        var result = await RunCheckedAsync(Path.GetTempPath(), ["encinfo"], cancellationToken);
        return SteghideOutputParser.ParseCiphers(result.StdOut);
    }

    public void Dispose() => _concurrency.Dispose();

    private async Task<SteghideCommandResult> RunCheckedAsync(string workDir, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var result = await RunAsync(workDir, args, cancellationToken);
        return result.ExitCode == 0 ? result : throw SteghideErrorMapper.Map(result);
    }

    private async Task<SteghideCommandResult> RunAsync(string workDir, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var executable = ResolveExecutable();
        var commandLine = BuildDisplayCommand(args);

        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Stable, English messages for the error mapper.
        startInfo.Environment["LANG"] = "C";
        startInfo.Environment["LC_ALL"] = "C";

        await _concurrency.WaitAsync(cancellationToken);
        try
        {
            using var process = new Process { StartInfo = startInfo };
            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                throw new SteghideUnavailableException(
                    "Steghide could not be started.",
                    $"Check the Steghide:ExecutablePath setting ({executable}).",
                    ex);
            }

            // Closing stdin guarantees Steghide can never wait for an answer.
            process.StandardInput.Close();
            var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            try
            {
                await process.WaitForExitAsync(linked.Token);
            }
            catch (OperationCanceledException)
            {
                KillQuietly(process);
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new SteghideExecutionException("Steghide timed out.", $"No answer after {_options.TimeoutSeconds} seconds.");
            }

            var result = new SteghideCommandResult(process.ExitCode, await stdout, await stderr, commandLine);
            _logger.LogInformation("Steghide {Command} finished with exit code {ExitCode}", commandLine, result.ExitCode);
            return result;
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private string ResolveExecutable()
    {
        var path = Path.IsPathRooted(_options.ExecutablePath)
            ? _options.ExecutablePath
            : Path.Combine(AppContext.BaseDirectory, _options.ExecutablePath);

        return File.Exists(path)
            ? path
            : throw new SteghideUnavailableException(
                "Steghide was not found.",
                $"Expected the executable at {path}. Check the Steghide:ExecutablePath setting.");
    }

    private static string BuildDisplayCommand(IReadOnlyList<string> args)
    {
        var builder = new StringBuilder("steghide");
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            builder.Append(' ');

            if (arg.StartsWith('-'))
            {
                builder.Append(arg);
            }
            else
            {
                var isPassphrase = i > 0 && args[i - 1] == "-p";
                builder.Append('"').Append(isPassphrase && arg.Length > 0 ? MaskedPassphrase : arg).Append('"');
            }
        }

        return builder.ToString();
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // The process had already exited.
        }
    }
}
