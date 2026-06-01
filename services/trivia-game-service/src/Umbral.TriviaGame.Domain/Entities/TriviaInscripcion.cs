using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Entities;

public sealed class TriviaInscripcion : Entity<TriviaInscripcionId>
{
    public PartidaId PartidaId { get; }
    public string UsuarioId { get; }
    public string? EquipoId { get; }
    public DateTimeOffset FechaInscripcion { get; }

    private TriviaInscripcion(TriviaInscripcionId id, PartidaId partidaId, string usuarioId, string? equipoId, DateTimeOffset fechaInscripcion)
        : base(id)
    {
        PartidaId = partidaId;
        UsuarioId = usuarioId;
        EquipoId = equipoId;
        FechaInscripcion = fechaInscripcion;
    }

    public static TriviaInscripcion Create(PartidaId partidaId, string usuarioId, string? equipoId = null)
    {
        if (partidaId is null)
            throw new DomainValidationException("El identificador de la partida es obligatorio.");
        if (string.IsNullOrWhiteSpace(usuarioId))
            throw new DomainValidationException("El identificador del usuario es obligatorio.");

        var id = TriviaInscripcionId.New();
        var now = DateTimeOffset.UtcNow;

        return new TriviaInscripcion(id, partidaId, usuarioId, equipoId, now);
    }

    public override string ToString() =>
        $"TriviaInscripcion {{ Id: {Id}, PartidaId: {PartidaId.Value}, UsuarioId: \"{UsuarioId}\", EquipoId: \"{EquipoId}\" }}";
}
