using Microsoft.Extensions.Primitives;

namespace Ocultum.Web.Security;

/// <summary>
/// The app is a local tool, so it only answers to loopback host names (against DNS rebinding) and refuses
/// state-changing requests that a page from another origin triggers (against cross-site request forgery).
/// </summary>
public sealed class LocalRequestGuardMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> LoopbackHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "127.0.0.1", "localhost", "[::1]",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!LoopbackHosts.Contains(context.Request.Host.Host))
        {
            await Reject(context, StatusCodes.Status400BadRequest, "INVALID_HOST", "Requests must use a loopback host name.");
            return;
        }

        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && IsCrossOrigin(context.Request))
        {
            await Reject(context, StatusCodes.Status403Forbidden, "CROSS_ORIGIN_BLOCKED", "Cross-origin requests are not allowed.");
            return;
        }

        await next(context);
    }

    private static bool IsCrossOrigin(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Sec-Fetch-Site", out var site) && site == "cross-site")
        {
            return true;
        }

        if (!request.Headers.TryGetValue("Origin", out StringValues origin) || StringValues.IsNullOrEmpty(origin))
        {
            return false;
        }

        return !Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri)
            || !string.Equals(uri.Authority, request.Host.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static Task Reject(HttpContext context, int status, string errorCode, string title)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            new Dictionary<string, object?> { ["status"] = status, ["title"] = title, ["errorCode"] = errorCode },
            options: null,
            contentType: "application/problem+json");
    }
}
