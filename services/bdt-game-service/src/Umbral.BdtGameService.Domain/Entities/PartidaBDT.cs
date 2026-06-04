using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.Domain.Entities;

public sealed class PartidaBDT
{
    public Guid PartidaId { get; private set; }
    public string Nombre { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public EstadoPartida Estado { get; private set; }
    public AreaBusqueda AreaBusqueda { get; private set; }
    public int MinimoParticipantes { get; private set; }
    public int? MaximoParticipantes { get; private set; }
    public int? MaximoEquipos { get; private set; }
    public int? MinimoJugadoresPorEquipo { get; private set; }
    public ModoInicioPartida ModoInicio { get; private set; }
    public List<EtapaBDT> Etapas { get; private set; } = new();
    public List<ExploradorBDT> Exploradores { get; private set; } = new();
    public List<TesoroQR> Tesoros { get; private set; } = new();

    private PartidaBDT()
    {
        Nombre = string.Empty;
        AreaBusqueda = new AreaBusqueda("Sin area configurada");
        ModoInicio = ModoInicioPartida.Manual;
    }

    private PartidaBDT(
        string nombre,
        Modalidad modalidad,
        AreaBusqueda areaBusqueda,
        int minimoParticipantes,
        int? maximoParticipantes,
        int? maximoEquipos,
        int? minimoJugadoresPorEquipo,
        ModoInicioPartida modoInicio,
        IEnumerable<EtapaBDT> etapas,
        EstadoPartida estado)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("Nombre requerido", nameof(nombre));
        }

        var etapasList = etapas.ToList();
        if (etapasList.Count == 0)
        {
            throw new ArgumentException("Una partida BDT debe tener al menos una etapa.", nameof(etapas));
        }

        if (etapasList.Select(etapa => etapa.Orden).Distinct().Count() != etapasList.Count)
        {
            throw new InvalidOperationException("No se permiten etapas BDT con orden duplicado.");
        }

        if (minimoParticipantes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimoParticipantes), "El minimo de participantes debe ser mayor que cero.");
        }

        ValidarLimitesPorModalidad(modalidad, minimoParticipantes, maximoParticipantes, maximoEquipos, minimoJugadoresPorEquipo);

        PartidaId = Guid.NewGuid();
        Nombre = nombre.Trim();
        Modalidad = modalidad;
        Estado = estado;
        AreaBusqueda = areaBusqueda;
        MinimoParticipantes = minimoParticipantes;
        MaximoParticipantes = maximoParticipantes;
        MaximoEquipos = maximoEquipos;
        MinimoJugadoresPorEquipo = minimoJugadoresPorEquipo;
        ModoInicio = modoInicio;
        Etapas = etapasList;
    }

    public static PartidaBDT CrearPublicada(string nombre, Modalidad modalidad, AreaBusqueda areaBusqueda, IEnumerable<EtapaBDT> etapas)
    {
        var defaults = ObtenerLimitesPorDefecto(modalidad);
        return new PartidaBDT(
            nombre,
            modalidad,
            areaBusqueda,
            1,
            defaults.MaximoParticipantes,
            defaults.MaximoEquipos,
            defaults.MinimoJugadoresPorEquipo,
            ModoInicioPartida.Manual,
            etapas,
            EstadoPartida.Lobby);
    }

    public static PartidaBDT CrearPublicada(
        string nombre,
        Modalidad modalidad,
        AreaBusqueda areaBusqueda,
        int minimoParticipantes,
        int? maximoParticipantes,
        int? maximoEquipos,
        int? minimoJugadoresPorEquipo,
        ModoInicioPartida modoInicio,
        IEnumerable<EtapaBDT> etapas)
    {
        return new PartidaBDT(
            nombre,
            modalidad,
            areaBusqueda,
            minimoParticipantes,
            maximoParticipantes,
            maximoEquipos,
            minimoJugadoresPorEquipo,
            modoInicio,
            etapas,
            EstadoPartida.Lobby);
    }

    public static PartidaBDT CrearNoPublicada(string nombre, Modalidad modalidad, AreaBusqueda areaBusqueda, IEnumerable<EtapaBDT> etapas, EstadoPartida estado)
    {
        if (estado == EstadoPartida.Lobby)
        {
            throw new ArgumentException("Use CrearPublicada para partidas en Lobby.", nameof(estado));
        }

        var defaults = ObtenerLimitesPorDefecto(modalidad);
        return new PartidaBDT(
            nombre,
            modalidad,
            areaBusqueda,
            1,
            defaults.MaximoParticipantes,
            defaults.MaximoEquipos,
            defaults.MinimoJugadoresPorEquipo,
            ModoInicioPartida.Manual,
            etapas,
            estado);
    }

    public ExploradorBDT RegistrarParticipanteIndividual(Guid participanteUserId, DateTime fechaInscripcionUtc)
    {
        if (participanteUserId == Guid.Empty)
        {
            throw new ArgumentException("ParticipanteUserId requerido", nameof(participanteUserId));
        }

        if (Estado != EstadoPartida.Lobby)
        {
            throw new InvalidOperationException("La BDT no esta en lobby.");
        }

        if (Modalidad != Modalidad.Individual)
        {
            throw new InvalidOperationException("La BDT no es individual.");
        }

        if (Exploradores.Any(explorador =>
                explorador.TipoCompetidor == TipoCompetidor.Usuario &&
                explorador.CompetidorId == participanteUserId))
        {
            throw new InvalidOperationException("El participante ya esta inscrito en esta BDT.");
        }

        if (!MaximoParticipantes.HasValue)
        {
            throw new InvalidOperationException("La BDT individual no tiene capacidad configurada.");
        }

        var inscritosIndividuales = Exploradores.Count(explorador => explorador.TipoCompetidor == TipoCompetidor.Usuario);
        if (inscritosIndividuales >= MaximoParticipantes.Value)
        {
            throw new InvalidOperationException("La BDT individual no tiene cupos disponibles.");
        }

        var explorador = ExploradorBDT.CrearIndividual(PartidaId, participanteUserId, fechaInscripcionUtc);
        Exploradores.Add(explorador);
        return explorador;
    }

    public int ObtenerPosicionEnLobby(Guid exploradorId)
    {
        var orderedExploradores = Exploradores
            .Where(explorador => explorador.TipoCompetidor == TipoCompetidor.Usuario)
            .OrderBy(explorador => explorador.FechaInscripcionUtc)
            .ThenBy(explorador => explorador.ExploradorId)
            .ToList();

        var index = orderedExploradores.FindIndex(explorador => explorador.ExploradorId == exploradorId);
        if (index < 0)
        {
            throw new InvalidOperationException("El explorador no pertenece a la partida.");
        }

        return index + 1;
    }

    public EtapaBDT IniciarManualmente(Guid operadorUserId, DateTime iniciadaEnUtc)
    {
        if (operadorUserId == Guid.Empty)
        {
            throw new ArgumentException("OperadorUserId requerido", nameof(operadorUserId));
        }

        if (iniciadaEnUtc == default)
        {
            throw new ArgumentException("Fecha de inicio requerida", nameof(iniciadaEnUtc));
        }

        if (Estado != EstadoPartida.Lobby)
        {
            throw new InvalidOperationException("La BDT no esta en lobby.");
        }

        if (ModoInicio == ModoInicioPartida.Automatico)
        {
            throw new InvalidOperationException("La BDT configurada como automatica no puede iniciarse manualmente.");
        }

        if (!CumpleMinimoParticipacion())
        {
            throw new InvalidOperationException("La BDT no cumple el minimo de participacion configurado.");
        }

        if (Etapas.Count == 0)
        {
            throw new InvalidOperationException("La BDT no tiene etapas configuradas.");
        }

        if (Etapas.Any(etapa => etapa.Estado == EstadoEtapa.Activa))
        {
            throw new InvalidOperationException("La BDT ya tiene una etapa activa.");
        }

        var primeraEtapa = Etapas.OrderBy(etapa => etapa.Orden).First();
        primeraEtapa.Activar(iniciadaEnUtc);
        Estado = EstadoPartida.Iniciada;

        return primeraEtapa;
    }

    public (ExploradorBDT Explorador, EtapaBDT EtapaActiva) ObtenerEtapaActivaParaParticipante(Guid participanteUserId)
    {
        if (participanteUserId == Guid.Empty)
        {
            throw new ArgumentException("ParticipanteUserId requerido", nameof(participanteUserId));
        }

        if (Estado != EstadoPartida.Iniciada)
        {
            throw new InvalidOperationException("La BDT no esta iniciada.");
        }

        var etapasActivas = Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa).ToList();
        if (etapasActivas.Count == 0)
        {
            throw new InvalidOperationException("La BDT no tiene etapa activa.");
        }

        if (etapasActivas.Count > 1)
        {
            throw new InvalidOperationException("La BDT tiene mas de una etapa activa.");
        }

        var explorador = Exploradores.SingleOrDefault(candidate =>
            candidate.TipoCompetidor == TipoCompetidor.Usuario &&
            candidate.CompetidorId == participanteUserId);

        if (explorador is null)
        {
            throw new UnauthorizedAccessException("El participante no esta registrado en esta BDT.");
        }

        return (explorador, etapasActivas[0]);
    }

    public TesoroQR RegistrarTesoroQr(
        Guid etapaId,
        Guid participanteUserId,
        string imagenReferencia,
        string? qrDecodificado,
        DateTime fechaEnvioUtc)
    {
        if (etapaId == Guid.Empty)
        {
            throw new ArgumentException("EtapaId requerido", nameof(etapaId));
        }

        if (string.IsNullOrWhiteSpace(imagenReferencia))
        {
            throw new ArgumentException("ImagenReferencia requerida", nameof(imagenReferencia));
        }

        if (fechaEnvioUtc == default)
        {
            throw new ArgumentException("FechaEnvioUtc requerida", nameof(fechaEnvioUtc));
        }

        var (explorador, etapaActiva) = ObtenerEtapaActivaParaParticipante(participanteUserId);
        if (etapaActiva.EtapaId != etapaId)
        {
            throw new InvalidOperationException("La etapa indicada no corresponde a la etapa activa.");
        }

        if (etapaActiva.Estado != EstadoEtapa.Activa)
        {
            throw new InvalidOperationException("La etapa no acepta intentos de tesoro.");
        }

        var tesoro = TesoroQR.Crear(
            PartidaId,
            etapaActiva.EtapaId,
            explorador.ExploradorId,
            imagenReferencia,
            qrDecodificado,
            fechaEnvioUtc);

        Tesoros.Add(tesoro);
        return tesoro;
    }

    private static (int? MaximoParticipantes, int? MaximoEquipos, int? MinimoJugadoresPorEquipo) ObtenerLimitesPorDefecto(Modalidad modalidad)
    {
        return modalidad == Modalidad.Individual
            ? (10, null, null)
            : (null, 10, 1);
    }

    private bool CumpleMinimoParticipacion()
    {
        var inscritos = Modalidad == Modalidad.Individual
            ? Exploradores.Count(explorador => explorador.TipoCompetidor == TipoCompetidor.Usuario)
            : Exploradores.Count(explorador => explorador.TipoCompetidor == TipoCompetidor.Equipo);

        return inscritos >= MinimoParticipantes;
    }

    private static void ValidarLimitesPorModalidad(
        Modalidad modalidad,
        int minimoParticipantes,
        int? maximoParticipantes,
        int? maximoEquipos,
        int? minimoJugadoresPorEquipo)
    {
        if (modalidad == Modalidad.Individual)
        {
            if (!maximoParticipantes.HasValue || maximoParticipantes.Value <= 0)
            {
                throw new InvalidOperationException("La modalidad individual requiere maximo de jugadores mayor que cero.");
            }

            if (maximoParticipantes.Value < minimoParticipantes)
            {
                throw new InvalidOperationException("El maximo de jugadores no puede ser menor que el minimo de participantes.");
            }

            if (maximoEquipos.HasValue || minimoJugadoresPorEquipo.HasValue)
            {
                throw new InvalidOperationException("La modalidad individual no debe definir limites de equipos.");
            }

            return;
        }

        if (!maximoEquipos.HasValue || maximoEquipos.Value <= 0)
        {
            throw new InvalidOperationException("La modalidad por equipo requiere maximo de equipos mayor que cero.");
        }

        if (!minimoJugadoresPorEquipo.HasValue || minimoJugadoresPorEquipo.Value <= 0)
        {
            throw new InvalidOperationException("La modalidad por equipo requiere minimo de jugadores por equipo mayor que cero.");
        }

        if (maximoEquipos.Value < minimoParticipantes)
        {
            throw new InvalidOperationException("El maximo de equipos no puede ser menor que el minimo de participantes.");
        }

        if (maximoParticipantes.HasValue)
        {
            throw new InvalidOperationException("La modalidad por equipo no debe definir maximo de jugadores individuales.");
        }
    }
}
