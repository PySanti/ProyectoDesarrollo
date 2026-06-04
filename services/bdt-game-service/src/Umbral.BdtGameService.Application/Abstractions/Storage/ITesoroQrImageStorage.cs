namespace Umbral.BdtGameService.Application.Abstractions.Storage;

public interface ITesoroQrImageStorage
{
    Task<string> StoreAsync(
        Guid partidaId,
        Guid etapaId,
        Guid participanteUserId,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken);
}
