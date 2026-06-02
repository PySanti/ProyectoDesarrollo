using Umbral.TeamService.Domain.Enums;
using Umbral.TeamService.Domain.Exceptions;

namespace Umbral.TeamService.Domain.Entities;

public sealed class Equipo
{
    private const int MaximoIntegrantes = 5;

    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public string CodigoAcceso { get; private set; }
    public EstadoEquipo Estado { get; private set; }
    public List<ParticipanteEquipo> Participantes { get; private set; } = new();

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

        Participantes.Add(ParticipanteEquipo.CrearCreador(creadorUserId));
        EnsureCardinalityInvariant();
    }

    public static Equipo CrearPorParticipante(string nombreEquipo, string codigoAcceso, Guid creadorUserId)
    {
        return new Equipo(nombreEquipo, codigoAcceso, creadorUserId);
    }

    public void AgregarParticipante(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("UsuarioId requerido", nameof(usuarioId));
        }

        if (Participantes.Any(p => p.UsuarioId == usuarioId))
        {
            throw new InvalidOperationException("El participante ya pertenece a este equipo.");
        }

        if (Participantes.Count >= MaximoIntegrantes)
        {
            throw new InvalidOperationException("El equipo ya alcanzo el maximo de 5 integrantes.");
        }

        Participantes.Add(ParticipanteEquipo.CrearIntegrante(usuarioId));
        EnsureCardinalityInvariant();
    }

    public ResultadoSalidaEquipo Salir(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("UsuarioId requerido", nameof(usuarioId));
        }

        if (Estado != EstadoEquipo.Activo)
        {
            throw new EquipoNoActivoException(EquipoId);
        }

        var participante = Participantes.SingleOrDefault(p => p.UsuarioId == usuarioId);
        if (participante is null)
        {
            throw new ParticipanteNoPerteneceAlEquipoException(usuarioId);
        }

        if (participante.EsLider && Participantes.Count > 1)
        {
            throw new LiderDebeTransferirLiderazgoException(usuarioId);
        }

        Participantes.Remove(participante);

        if (participante.EsLider)
        {
            Estado = EstadoEquipo.Eliminado;
            return ResultadoSalidaEquipo.EquipoEliminado;
        }

        EnsureCardinalityInvariant();
        return ResultadoSalidaEquipo.SalioDelEquipo;
    }

    private void EnsureCardinalityInvariant()
    {
        if (Participantes.Count < 1 || Participantes.Count > MaximoIntegrantes)
        {
            throw new InvalidOperationException("La cardinalidad del equipo debe estar entre 1 y 5 integrantes.");
        }

        if (Participantes.Count(p => p.EsLider) != 1)
        {
            throw new InvalidOperationException("El equipo debe tener exactamente un lider.");
        }
    }
}
