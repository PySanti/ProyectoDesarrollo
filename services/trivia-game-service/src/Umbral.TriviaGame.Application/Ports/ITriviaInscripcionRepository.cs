using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Ports;

public interface ITriviaInscripcionRepository
{
    Task AddAsync(TriviaInscripcion inscripcion, CancellationToken cancellationToken = default);

    Task<int> CountByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPartidaYUsuarioAsync(PartidaId partidaId, string usuarioId, CancellationToken cancellationToken = default);
}
