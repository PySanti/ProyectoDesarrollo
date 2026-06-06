using FluentValidation;

namespace Umbral.BdtGameService.Application.Games.DecodeExpectedQr;

public sealed class DecodificarQrEsperadoBdtCommandValidator : AbstractValidator<DecodificarQrEsperadoBdtCommand>
{
    public const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    public DecodificarQrEsperadoBdtCommandValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(contentType => AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Solo se aceptan imagenes JPEG o PNG.");
        RuleFor(command => command.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxImageSizeBytes)
            .WithMessage("La imagen no puede superar 5 MB.");
        RuleFor(command => command.ImageContent)
            .NotNull()
            .Must(content => content.Length > 0)
            .WithMessage("La imagen es requerida.");
        RuleFor(command => command)
            .Must(command => command.ImageContent.LongLength == command.Length)
            .WithMessage("La metadata de tamano no coincide con el contenido de la imagen.");
    }
}
