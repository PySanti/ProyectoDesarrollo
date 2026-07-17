using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class Equipo
{
    private const int MaximoIntegrantes = 5;

    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public EstadoEquipo Estado { get; private set; }
    public List<ParticipanteEquipo> Participantes { get; private set; } = new();

    private Equipo()
    {
        NombreEquipo = string.Empty;
    }

    private Equipo(string nombreEquipo, Guid creadorSubjectId)
    {
        if (string.IsNullOrWhiteSpace(nombreEquipo))
            throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));
        EquipoId = Guid.NewGuid();
        NombreEquipo = nombreEquipo.Trim();
        Estado = EstadoEquipo.Activo;
        Participantes.Add(ParticipanteEquipo.CrearCreador(creadorSubjectId));
        EnsureCardinalityInvariant();
    }

    public static Equipo CrearPorParticipante(string nombreEquipo, Guid creadorSubjectId)
    {
        return new Equipo(nombreEquipo, creadorSubjectId);
    }

    public void AgregarParticipante(Guid subjectId)
    {
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        }

        if (Participantes.Any(p => p.SubjectId == subjectId))
        {
            throw new InvalidOperationException("El participante ya pertenece a este equipo.");
        }

        if (Participantes.Count >= MaximoIntegrantes)
        {
            throw new InvalidOperationException("El equipo ya alcanzo el maximo de 5 integrantes.");
        }

        Participantes.Add(ParticipanteEquipo.CrearIntegrante(subjectId));
        EnsureCardinalityInvariant();
    }

    public ResultadoSalidaEquipo Salir(Guid subjectId)
    {
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        }

        if (Estado != EstadoEquipo.Activo)
        {
            throw new EquipoNoActivoException(EquipoId);
        }

        var participante = Participantes.SingleOrDefault(p => p.SubjectId == subjectId);
        if (participante is null)
        {
            throw new ParticipanteNoPerteneceAlEquipoException(subjectId);
        }

        if (participante.EsLider && Participantes.Count > 1)
        {
            throw new LiderDebeTransferirLiderazgoException(subjectId);
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

    // Los nombres de los elementos de la tupla no se renombran: los consumen los handlers por
    // nombre, y tocarlos arrastraria el churn fuera del limite del slice (ver el spec).
    // Los nombres de los elementos de la tupla no se renombran: los handlers los consumen por
    // nombre y tocarlos arrastraria el churn fuera del limite del slice (ver el spec).
    public (Guid LiderAnteriorUserId, Guid NuevoLiderUserId) TransferirLiderazgo(Guid actorSubjectId, Guid nuevoLiderSubjectId)
    {
        if (actorSubjectId == Guid.Empty)
        {
            throw new ArgumentException("ActorSubjectId requerido", nameof(actorSubjectId));
        }

        if (nuevoLiderSubjectId == Guid.Empty)
        {
            throw new ArgumentException("NuevoLiderSubjectId requerido", nameof(nuevoLiderSubjectId));
        }

        if (Estado != EstadoEquipo.Activo)
        {
            throw new EquipoNoActivoException(EquipoId);
        }

        var liderActual = Participantes.SingleOrDefault(p => p.EsLider);
        if (liderActual is null)
        {
            throw new InvalidOperationException("El equipo debe tener exactamente un lider.");
        }

        var actor = Participantes.SingleOrDefault(p => p.SubjectId == actorSubjectId);
        if (actor is null)
        {
            throw new ParticipanteNoPerteneceAlEquipoException(actorSubjectId);
        }

        if (!actor.EsLider || liderActual.SubjectId != actorSubjectId)
        {
            throw new ActorNoEsLiderEquipoException(actorSubjectId);
        }

        if (nuevoLiderSubjectId == actorSubjectId)
        {
            throw new NuevoLiderDebeSerDiferenteException(nuevoLiderSubjectId);
        }

        var nuevoLider = Participantes.SingleOrDefault(p => p.SubjectId == nuevoLiderSubjectId);
        if (nuevoLider is null)
        {
            throw new NuevoLiderNoPerteneceAlEquipoException(nuevoLiderSubjectId);
        }

        liderActual.QuitarLiderazgo();
        nuevoLider.MarcarComoLider();
        EnsureCardinalityInvariant();

        return (actorSubjectId, nuevoLiderSubjectId);
    }

    public static Equipo CrearPorAdmin(string nombreEquipo, Guid liderSubjectId)
    {
        return new Equipo(nombreEquipo, liderSubjectId);
    }

    public IReadOnlyList<Guid> EliminarPorLider(Guid actorSubjectId)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);

        var lider = Participantes.SingleOrDefault(p => p.EsLider);
        if (lider is null || lider.SubjectId != actorSubjectId)
            throw new ActorNoEsLiderEquipoException(actorSubjectId);

        var afectados = Participantes.Select(p => p.SubjectId).ToList();
        Estado = EstadoEquipo.Eliminado;
        return afectados;
    }

    public IReadOnlyList<Guid> EliminarPorAdmin()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);

        var afectados = Participantes.Select(p => p.SubjectId).ToList();
        Estado = EstadoEquipo.Eliminado;
        return afectados;
    }

    public void Desactivar()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        Estado = EstadoEquipo.Desactivado;
    }

    public void Reactivar()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        Estado = EstadoEquipo.Activo;
    }

    public void Renombrar(string nuevoNombre)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ArgumentException("NombreEquipo requerido", nameof(nuevoNombre));
        NombreEquipo = nuevoNombre.Trim();
    }

    public (Guid LiderAnteriorUserId, Guid NuevoLiderUserId) ReasignarLiderazgoPorAdmin(Guid nuevoLiderSubjectId)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        if (nuevoLiderSubjectId == Guid.Empty)
            throw new ArgumentException("NuevoLiderUserId requerido", nameof(nuevoLiderSubjectId));

        var liderActual = Participantes.SingleOrDefault(p => p.EsLider)
            ?? throw new InvalidOperationException("El equipo debe tener exactamente un lider.");

        if (liderActual.SubjectId == nuevoLiderSubjectId)
            throw new NuevoLiderDebeSerDiferenteException(nuevoLiderSubjectId);

        var nuevoLider = Participantes.SingleOrDefault(p => p.SubjectId == nuevoLiderSubjectId)
            ?? throw new NuevoLiderNoPerteneceAlEquipoException(nuevoLiderSubjectId);

        liderActual.QuitarLiderazgo();
        nuevoLider.MarcarComoLider();
        EnsureCardinalityInvariant();

        return (liderActual.SubjectId, nuevoLiderSubjectId);
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
