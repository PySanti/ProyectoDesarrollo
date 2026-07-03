using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface IConfiguracionPartidaClient
{
    Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken);
}
