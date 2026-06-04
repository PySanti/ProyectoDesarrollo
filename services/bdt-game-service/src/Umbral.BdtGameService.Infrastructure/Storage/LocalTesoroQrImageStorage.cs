using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Umbral.BdtGameService.Application.Abstractions.Storage;

namespace Umbral.BdtGameService.Infrastructure.Storage;

public sealed class LocalTesoroQrImageStorage : ITesoroQrImageStorage
{
    private readonly string _basePath;

    public LocalTesoroQrImageStorage(IConfiguration configuration)
    {
        _basePath = configuration["BdtTreasureStorage:BasePath"]
            ?? Path.Combine(Path.GetTempPath(), "umbral-bdt-game-service", "treasure-uploads");
    }

    public async Task<string> StoreAsync(
        Guid partidaId,
        Guid etapaId,
        Guid participanteUserId,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var safeExtension = contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        var reference = $"bdt/{partidaId:N}/etapas/{etapaId:N}/tesoros/{participanteUserId:N}-{hash[..16]}.{safeExtension}";
        var fullPath = Path.Combine(_basePath, reference.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath) ?? _basePath;

        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return reference;
    }
}
