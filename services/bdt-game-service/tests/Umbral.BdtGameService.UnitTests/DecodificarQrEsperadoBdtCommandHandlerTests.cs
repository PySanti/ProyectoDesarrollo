using Umbral.BdtGameService.Application.Abstractions.Qr;
using Umbral.BdtGameService.Application.Games.DecodeExpectedQr;

namespace Umbral.BdtGameService.UnitTests;

public sealed class DecodificarQrEsperadoBdtCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Decoded_Text_When_Qr_Is_Readable()
    {
        var decoder = new FakeQrDecoder(" QR-ETAPA-1 ");
        var handler = new DecodificarQrEsperadoBdtCommandHandler(decoder);

        var response = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal("Decodificado", response.EstadoProcesamiento);
        Assert.Equal("QR-ETAPA-1", response.QrDecodificado);
        Assert.Contains("correctamente", response.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.True(decoder.Called);
    }

    [Fact]
    public async Task Handle_Should_Return_Unreadable_State_When_Qr_Is_Not_Readable()
    {
        var handler = new DecodificarQrEsperadoBdtCommandHandler(new FakeQrDecoder(null));

        var response = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal("NoLegible", response.EstadoProcesamiento);
        Assert.Null(response.QrDecodificado);
        Assert.Contains("no se pudo leer", response.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validator_Should_Reject_Invalid_Image_Metadata()
    {
        var validator = new DecodificarQrEsperadoBdtCommandValidator();

        var result = validator.Validate(new DecodificarQrEsperadoBdtCommand(
            string.Empty,
            "text/plain",
            DecodificarQrEsperadoBdtCommandValidator.MaxImageSizeBytes + 1,
            Array.Empty<byte>()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(DecodificarQrEsperadoBdtCommand.FileName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(DecodificarQrEsperadoBdtCommand.ContentType));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(DecodificarQrEsperadoBdtCommand.Length));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(DecodificarQrEsperadoBdtCommand.ImageContent));
    }

    private static DecodificarQrEsperadoBdtCommand CreateCommand()
    {
        var content = new byte[] { 1, 2, 3 };
        return new DecodificarQrEsperadoBdtCommand("qr.png", "image/png", content.Length, content);
    }

    private sealed class FakeQrDecoder : IQrImageDecoder
    {
        private readonly string? _decoded;

        public FakeQrDecoder(string? decoded)
        {
            _decoded = decoded;
        }

        public bool Called { get; private set; }

        public Task<string?> DecodeAsync(byte[] imageContent, string contentType, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(_decoded);
        }
    }
}
