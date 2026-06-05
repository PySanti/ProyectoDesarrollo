using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu34ExpectedQrDecodeContractTests : IClassFixture<BdtApiFactory>
{
    private readonly HttpClient _client;

    public Hu34ExpectedQrDecodeContractTests(BdtApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DecodeExpectedQr_Should_Match_Readable_Response_Shape()
    {
        var request = CreateDecodeRequest("image/png", Encoding.UTF8.GetBytes("QR:QR-ETAPA-1"));

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Decodificado", document.RootElement.GetProperty("estadoProcesamiento").GetString());
        Assert.Equal("QR-ETAPA-1", document.RootElement.GetProperty("qrDecodificado").GetString());
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Fact]
    public async Task DecodeExpectedQr_Should_Match_Unreadable_Response_Shape()
    {
        var request = CreateDecodeRequest("image/png", Encoding.UTF8.GetBytes("not-a-qr"));

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("NoLegible", document.RootElement.GetProperty("estadoProcesamiento").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("qrDecodificado").ValueKind);
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Fact]
    public async Task DecodeExpectedQr_Should_Match_Unauthorized_Status()
    {
        using var content = CreateMultipartContent("image/png", Encoding.UTF8.GetBytes("QR:QR"));

        var response = await _client.PostAsync("/api/bdt/stages/expected-qr/decode", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("text/plain", HttpStatusCode.UnsupportedMediaType)]
    [InlineData("image/jpeg", HttpStatusCode.RequestEntityTooLarge)]
    public async Task DecodeExpectedQr_Should_Match_Image_Constraint_Statuses(string contentType, HttpStatusCode expected)
    {
        var bytes = expected == HttpStatusCode.RequestEntityTooLarge
            ? new byte[(5 * 1024 * 1024) + 1]
            : Encoding.UTF8.GetBytes("QR:QR");

        var response = await _client.SendAsync(CreateDecodeRequest(contentType, bytes));

        Assert.Equal(expected, response.StatusCode);
    }

    private static HttpRequestMessage CreateDecodeRequest(string contentType, byte[] bytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bdt/stages/expected-qr/decode");
        request.Headers.Add("X-Test-Role", "Operador");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        request.Content = CreateMultipartContent(contentType, bytes);
        return request;
    }

    private static MultipartFormDataContent CreateMultipartContent(string contentType, byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(bytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "image", contentType == "image/png" ? "qr.png" : "qr.jpg");
        return content;
    }
}
