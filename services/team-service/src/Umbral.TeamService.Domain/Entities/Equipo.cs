using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.Domain.Entities;

public sealed class Equipo
{
    private readonly List<ParticipanteEquipo> _participantes = new();

    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public string CodigoAcceso { get; private set; }
    public EstadoEquipo Estado { get; private set; }
    public IReadOnlyCollection<ParticipanteEquipo> Participantes => _participantes;

    private Equipo()
    {
        NombreEquipo = string.Empty;
        CodigoAcceso = string.Empty;
    }

    private Equipo(string nombreEquipo, string codigoAcceso, Guid creadorUserId)
    {
        if (string.IsNullOrWhiteSpace(nombreEquipo))
        {
            throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));
        }

        if (string.IsNullOrWhiteSpace(codigoAcceso))
        {
            throw new ArgumentException("CodigoAcceso requerido", nameof(codigoAcceso));
        }

        EquipoId = Guid.NewGuid();
        NombreEquipo = nombreEquipo.Trim();
        CodigoAcceso = codigoAcceso.Trim().ToUpperInvariant();
        Estado = EstadoEquipo.Activo;

        _participantes.Add(ParticipanteEquipo.CrearCreador(creadorUserId));
        EnsureCardinalityInvariant();
    }

    public static Equipo CrearPorParticipante(string nombreEquipo, string codigoAcceso, Guid creadorUserId)
    {
        return new Equipo(nombreEquipo, codigoAcceso, creadorUserId);
    }

    private void EnsureCardinalityInvariant()
    {
        if (_participantes.Count < 1 || _participantes.Count > 5)
        {
            throw new InvalidOperationException("La cardinalidad del equipo debe estar entre 1 y 5 integrantes.");
        }

        if (_participantes.Count(p => p.EsLider) != 1)
        {
            throw new InvalidOperationException("El equipo debe tener exactamente un lider.");
        }
    }
}
