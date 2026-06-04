using System.Text;
using Umbral.BdtGameService.Application.Abstractions.Qr;

namespace Umbral.BdtGameService.Infrastructure.Qr;

public sealed class DeterministicQrImageDecoder : IQrImageDecoder
{
    public Task<string?> DecodeAsync(byte[] imageContent, string contentType, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(imageContent);
        if (!text.StartsWith("QR:", StringComparison.Ordinal))
        {
            return Task.FromResult<string?>(null);
        }

        var decoded = text[3..].Trim();
        return Task.FromResult(string.IsNullOrWhiteSpace(decoded) ? null : decoded);
    }
}
