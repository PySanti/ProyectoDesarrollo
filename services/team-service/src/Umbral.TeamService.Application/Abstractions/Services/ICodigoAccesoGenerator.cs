namespace Umbral.TeamService.Application.Abstractions.Services;

public interface ICodigoAccesoGenerator
{
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken);
}
