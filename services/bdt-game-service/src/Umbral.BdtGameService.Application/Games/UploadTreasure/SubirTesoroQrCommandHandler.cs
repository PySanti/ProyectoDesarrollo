using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using Umbral.BdtGameService.Application.Abstractions.Storage;
using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Application.Games.UploadTreasure;

public sealed class SubirTesoroQrCommandHandler : IRequestHandler<SubirTesoroQrCommand, SubirTesoroQrResponse>
{
    private readonly IPartidaBdtRepository _repository;
    private readonly ITesoroQrImageStorage _imageStorage;
    private readonly IQrImageDecoder _qrImageDecoder;

    public SubirTesoroQrCommandHandler(
        IPartidaBdtRepository repository,
        ITesoroQrImageStorage imageStorage,
        IQrImageDecoder qrImageDecoder)
    {
        _repository = repository;
        _imageStorage = imageStorage;
        _qrImageDecoder = qrImageDecoder;
    }

    public async Task<SubirTesoroQrResponse> Handle(SubirTesoroQrCommand request, CancellationToken cancellationToken)
    {
        var partida = await _repository.GetByIdWithExploradoresAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            throw new KeyNotFoundException("Partida BDT no encontrada.");
        }

        var (_, etapaActiva) = partida.ObtenerEtapaActivaParaParticipante(request.ParticipanteUserId);
        if (etapaActiva.EtapaId != request.EtapaId)
        {
            throw new InvalidOperationException("La etapa indicada no corresponde a la etapa activa.");
        }

        var imagenReferencia = await _imageStorage.StoreAsync(
            request.PartidaId,
            request.EtapaId,
            request.ParticipanteUserId,
            request.FileName,
            request.ContentType,
            request.ImageContent,
            cancellationToken);

        var qrDecodificado = await _qrImageDecoder.DecodeAsync(request.ImageContent, request.ContentType, cancellationToken);
        var tesoro = partida.RegistrarTesoroQr(
            request.EtapaId,
            request.ParticipanteUserId,
            imagenReferencia,
            qrDecodificado,
            DateTime.UtcNow);

        await _repository.UpdateAsync(partida, cancellationToken);

        var mensaje = tesoro.EstadoProcesamiento == EstadoProcesamientoTesoroQr.Decodificado
            ? "Tesoro recibido para validacion."
            : "Tesoro recibido, pero no se pudo leer un QR en la imagen.";

        return new SubirTesoroQrResponse(
            tesoro.TesoroId,
            tesoro.PartidaId,
            tesoro.EtapaId,
            tesoro.ExploradorId,
            tesoro.FechaEnvioUtc,
            tesoro.EstadoProcesamiento.ToString(),
            tesoro.QrDecodificado,
            mensaje);
    }
}
