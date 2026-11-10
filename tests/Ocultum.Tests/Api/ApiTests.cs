using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ocultum.Tests.Support;

namespace Ocultum.Tests.Api;

public class ApiTests : IClassFixture<OcultumApiFactory>
{
    private readonly HttpClient _client;
    private readonly OcultumApiFactory _factory;

    public ApiTests(OcultumApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static ByteArrayContent FileContent(string name)
    {
        var content = new ByteArrayContent(File.ReadAllBytes(TestPaths.Sample(name)));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return content;
    }

    private static MultipartFormDataContent Form(params (string Name, HttpContent Content, string? FileName)[] parts)
    {
        var form = new MultipartFormDataContent();
        foreach (var (name, content, fileName) in parts)
        {
            if (fileName is null)
            {
                form.Add(content, name);
            }
            else
            {
                form.Add(content, name, fileName);
            }
        }

        return form;
    }

    private static (string, HttpContent, string?) Text(string name, string value) => (name, new StringContent(value), null);

    private async Task<JsonElement> HideMessageAsync(string message, string passphrase, string cover = "cover.jpg")
    {
        using var form = Form(("cover", FileContent(cover), cover), Text("message", message), Text("passphrase", passphrase));
        var response = await _client.PostAsync("/api/v1/stego-files", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [WindowsFact]
    public async Task Full_round_trip_through_the_api()
    {
        var created = await HideMessageAsync("Olá, mundo — 日本語", "chave");
        var id = created.GetProperty("id").GetString();
        created.GetProperty("isPasswordProtected").GetBoolean().Should().BeTrue();
        created.GetProperty("sha256").GetString().Should().HaveLength(64);

        var download = await _client.GetAsync($"/api/v1/stego-files/{id}/content");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        var stegoBytes = await download.Content.ReadAsByteArrayAsync();

        using var extractForm = Form(
            ("stegoFile", new ByteArrayContent(stegoBytes), "stego.jpg"), Text("passphrase", "chave"));
        var extraction = await _client.PostAsync("/api/v1/extractions", extractForm);

        extraction.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await extraction.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("kind").GetString().Should().Be("text");
        body.GetProperty("text").GetString().Should().Be("Olá, mundo — 日本語");
    }

    [WindowsFact]
    public async Task Responses_carry_no_hypermedia_links()
    {
        using var form = Form(("cover", FileContent("cover.jpg"), "cover.jpg"), Text("message", "x"));
        var response = await _client.PostAsync("/api/v1/stego-files", form);
        var raw = await response.Content.ReadAsStringAsync();

        response.Headers.Location.Should().BeNull();
        raw.Should().NotContain("_links").And.NotContain("href").And.NotContain("http");
    }

    [WindowsFact]
    public async Task Wrong_passphrase_returns_a_problem_json_with_a_stable_error_code()
    {
        var created = await HideMessageAsync("segredo", "certa");
        var stego = await _client.GetByteArrayAsync($"/api/v1/stego-files/{created.GetProperty("id").GetString()}/content");

        using var form = Form(("stegoFile", new ByteArrayContent(stego), "s.jpg"), Text("passphrase", "errada"));
        var response = await _client.PostAsync("/api/v1/extractions", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("WRONG_PASSPHRASE_OR_NO_DATA");
        problem.GetProperty("status").GetInt32().Should().Be(422);
        problem.TryGetProperty("type", out _).Should().BeFalse();
        problem.TryGetProperty("traceId", out _).Should().BeFalse();
    }

    [WindowsFact]
    public async Task Capacity_exceeded_returns_422_with_numbers()
    {
        var big = new ByteArrayContent(System.Security.Cryptography.RandomNumberGenerator.GetBytes(200_000));
        using var form = Form(("cover", FileContent("cover.au"), "cover.au"), ("payloadFile", big, "big.bin"));

        var response = await _client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("CAPACITY_EXCEEDED");
        problem.GetProperty("detail").GetString().Should().Contain("Payload is");
        problem.GetProperty("capacityBytes").GetInt64().Should().BeGreaterThan(0);
    }

    [WindowsFact]
    public async Task Cover_with_a_wrong_signature_returns_415()
    {
        using var form = Form(("cover", new StringContent("definitely not a jpeg"), "photo.jpg"), Text("message", "x"));

        var response = await _client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString()
            .Should().Be("UNSUPPORTED_COVER_FORMAT");
    }

    [WindowsFact]
    public async Task Missing_fields_return_400_with_validation_details()
    {
        using var form = Form(Text("message", "no cover here"));

        var response = await _client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty("cover", out _).Should().BeTrue();
    }

    [WindowsFact]
    public async Task Empty_payload_returns_422()
    {
        using var form = Form(("cover", FileContent("cover.jpg"), "cover.jpg"));

        var response = await _client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString().Should().Be("EMPTY_PAYLOAD");
    }

    [WindowsFact]
    public async Task Invalid_compression_level_returns_400()
    {
        using var form = Form(("cover", FileContent("cover.jpg"), "cover.jpg"), Text("message", "x"), Text("compressionLevel", "12"));

        var response = await _client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [WindowsFact]
    public async Task Oversized_cover_returns_413()
    {
        using var small = new OcultumApiFactory { MaxCoverBytes = 5_000 };
        using var client = small.CreateClient();
        using var form = Form(("cover", FileContent("cover.jpg"), "cover.jpg"), Text("message", "x"));

        var response = await client.PostAsync("/api/v1/stego-files", form);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString().Should().Be("FILE_TOO_LARGE");
    }

    [WindowsFact]
    public async Task Unknown_and_malformed_ids_return_404()
    {
        (await _client.GetAsync("/api/v1/stego-files/0123456789abcdef0123456789abcdef/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync("/api/v1/extractions/not-an-id/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync("/api/v1/stego-files/not-an-id")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [WindowsFact]
    public async Task Stego_file_can_be_discarded_early_and_is_then_gone()
    {
        var created = await HideMessageAsync("temporario", "");
        var id = created.GetProperty("id").GetString();

        (await _client.DeleteAsync($"/api/v1/stego-files/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetAsync($"/api/v1/stego-files/{id}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [WindowsFact]
    public async Task A_stego_id_cannot_be_used_as_an_extraction_id()
    {
        var created = await HideMessageAsync("x", "");

        var response = await _client.GetAsync($"/api/v1/extractions/{created.GetProperty("id").GetString()}/content");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [WindowsFact]
    public async Task Cover_inspection_reports_format_and_capacity()
    {
        using var form = Form(("cover", FileContent("cover.bmp"), "cover.bmp"));

        var response = await _client.PostAsync("/api/v1/cover-inspections", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("format").GetString().Should().Be("bmp");
        body.GetProperty("capacityBytes").GetInt64().Should().BeGreaterThan(1000);
    }

    [WindowsFact]
    public async Task Ciphers_are_listed()
    {
        var ciphers = await _client.GetFromJsonAsync<JsonElement>("/api/v1/ciphers");

        ciphers.EnumerateArray().Select(c => c.GetProperty("name").GetString()).Should().Contain("rijndael-128");
    }

    [WindowsFact]
    public async Task Analysis_and_robustness_endpoints_work_together()
    {
        var created = await HideMessageAsync(new string('a', 1500), "abc");
        var stego = await _client.GetByteArrayAsync($"/api/v1/stego-files/{created.GetProperty("id").GetString()}/content");

        using var analysisForm = Form(("cover", FileContent("cover.jpg"), "cover.jpg"), ("stego", new ByteArrayContent(stego), "stego.jpg"));
        var analysis = await _client.PostAsync("/api/v1/analyses", analysisForm);
        analysis.StatusCode.Should().Be(HttpStatusCode.OK);
        var analysisBody = await analysis.Content.ReadFromJsonAsync<JsonElement>();
        analysisBody.GetProperty("cover").GetProperty("histogram").GetProperty("red").GetArrayLength().Should().Be(256);
        analysisBody.GetProperty("differenceImage").GetString().Should().NotBeNullOrEmpty();

        using var robustnessForm = Form(("stegoFile", new ByteArrayContent(stego), "stego.jpg"), Text("passphrase", "abc"), Text("quality", "85"));
        var robustness = await _client.PostAsync("/api/v1/robustness-tests", robustnessForm);
        robustness.StatusCode.Should().Be(HttpStatusCode.OK);
        (await robustness.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("survived").GetBoolean().Should().BeFalse();
    }

    [WindowsFact]
    public async Task Non_loopback_host_names_are_refused()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ciphers");
        request.Headers.Host = "evil.example.com";

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [WindowsFact]
    public async Task Cross_origin_writes_are_refused()
    {
        using var form = Form(("cover", FileContent("cover.jpg"), "cover.jpg"), Text("message", "x"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/stego-files") { Content = form };
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [WindowsFact]
    public async Task OpenApi_document_is_published()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("/api/v1/stego-files");
    }
}
