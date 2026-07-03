using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class SesionPartida
{
    private readonly List<JuegoResumen> _juegos = new();
    private readonly List<InscripcionPartida> _inscripciones = new();

    public SesionPartidaId Id { get; private set; }
    public Guid PartidaId { get; private set; }
    public EstadoSesion Estado { get; private set; }
    public string Nombre { get; private set; } = null!;
    public Modalidad Modalidad { get; private set; }
    public ModoInicioPartida ModoInicioPartida { get; private set; }
    public DateTime? TiempoInicio { get; private set; }
    public int MinimosParticipacion { get; private set; }
    public int MaximosParticipacion { get; private set; }
    public DateTime? FechaInicio { get; private set; }
    public DateTime? FechaFin { get; private set; }

    public IReadOnlyList<JuegoResumen> Juegos => _juegos;
    public IReadOnlyList<InscripcionPartida> Inscripciones => _inscripciones;

    private SesionPartida() { } // EF

    private SesionPartida(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        Id = SesionPartidaId.New();
        PartidaId = partidaId;
        Nombre = snapshot.Nombre;
        Modalidad = snapshot.Modalidad;
        ModoInicioPartida = snapshot.ModoInicioPartida;
        TiempoInicio = snapshot.TiempoInicio;
        MinimosParticipacion = snapshot.MinimosParticipacion;
        MaximosParticipacion = snapshot.MaximosParticipacion;
        _juegos.AddRange(snapshot.Juegos);
        Estado = EstadoSesion.Lobby;
    }

    public static SesionPartida Publicar(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        ValidarPublicabilidad(partidaId, snapshot);
        return new SesionPartida(partidaId, snapshot);
    }

    public InscripcionPartida Inscribir(
        Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (Modalidad != Modalidad.Individual)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
            throw new ParticipanteYaInscritoException(participanteId);
        if (tieneParticipacionActivaEnOtra)
            throw new ParticipacionActivaExistenteException(participanteId);
        if (inscritosActivos >= MaximosParticipacion)
            throw new CupoLlenoException(PartidaId);

        var inscripcion = new InscripcionPartida(participanteId, fecha);
        _inscripciones.Add(inscripcion);
        return inscripcion;
    }

    public void CancelarInscripcion(Guid participanteId)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        var inscripcion = _inscripciones.FirstOrDefault(i => i.ParticipanteId == participanteId && i.EsActiva)
            ?? throw new InscripcionNoEncontradaException(participanteId);
        inscripcion.Cancelar();
    }

    public InscripcionPartida PreinscribirEquipo(
        Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros,
        bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (Modalidad != Modalidad.Equipo)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (!callerEsLider)
            throw new NoEsLiderEquipoException(equipoId);
        if (_inscripciones.Any(i => i.EquipoId == equipoId && i.EsActiva))
            throw new EquipoYaInscritoException(equipoId);
        if (equipoTieneParticipacionActivaEnOtra)
            throw new ParticipacionActivaExistenteException(equipoId);
        if (equiposActivos >= MaximosParticipacion)
            throw new CupoLlenoException(PartidaId);

        var inscripcion = InscripcionPartida.PreinscribirEquipo(equipoId, miembros, PartidaId, fecha);
        _inscripciones.Add(inscripcion);
        return inscripcion;
    }

    public Convocatoria ResponderConvocatoria(
        Guid convocatoriaId, Guid usuarioId, bool aceptar,
        bool participanteTieneParticipacionActivaEnOtra, DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);

        var convocatoria = _inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .FirstOrDefault(c => c.Id.Valor == convocatoriaId && c.UsuarioId == usuarioId && c.EstaPendiente)
            ?? throw new ConvocatoriaNoEncontradaException(convocatoriaId);

        if (aceptar)
        {
            if (participanteTieneParticipacionActivaEnOtra)
                throw new ParticipacionActivaExistenteException(usuarioId);
            var yaAceptoOtraEnEstaSesion = _inscripciones.Any(i => i.EsActiva
                && i.Convocatorias.Any(c => c.UsuarioId == usuarioId && c.EstaAceptada));
            if (yaAceptoOtraEnEstaSesion)
                throw new ParticipacionActivaExistenteException(usuarioId);
            convocatoria.Aceptar(now);
        }
        else
        {
            convocatoria.Rechazar(now);
        }

        return convocatoria;
    }

    public void CancelarInscripcionEquipo(Guid equipoId, bool callerEsLider)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (!callerEsLider)
            throw new NoEsLiderEquipoException(equipoId);
        var inscripcion = _inscripciones.FirstOrDefault(i => i.EquipoId == equipoId && i.EsActiva)
            ?? throw new InscripcionNoEncontradaException(equipoId);
        inscripcion.Cancelar();
    }

    public ResultadoInicio Iniciar(DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (ModoInicioPartida is not (ModoInicioPartida.Manual or ModoInicioPartida.ManualYAutomatico))
            throw new ModoInicioNoCompatibleException(PartidaId);
        return AplicarInicio(now);
    }

    public ResultadoInicio IntentarInicioAutomatico(DateTime now)
    {
        if (ModoInicioPartida is not (ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico))
            throw new ModoInicioNoCompatibleException(PartidaId);
        if (Estado != EstadoSesion.Lobby)
            return ResultadoInicio.NoCorresponde;
        if (TiempoInicio is null || now < TiempoInicio.Value)
            return ResultadoInicio.NoCorresponde;
        return AplicarInicio(now);
    }

    public ResultadoAvance FinalizarJuegoActual(DateTime now)
    {
        if (Estado != EstadoSesion.Iniciada)
            throw new SesionNoIniciadaException(PartidaId);

        var actual = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        if (actual.TipoJuego == TipoJuego.Trivia && actual.TienePreguntasAbiertas)
            throw new JuegoConPreguntasPendientesException(PartidaId);
        if (actual.TipoJuego == TipoJuego.BusquedaDelTesoro && actual.TieneEtapasAbiertas)
            throw new JuegoConEtapasPendientesException(PartidaId);
        actual.Finalizar();

        var siguiente = _juegos
            .Where(j => j.Estado == EstadoJuego.Pendiente)
            .OrderBy(j => j.Orden)
            .FirstOrDefault();

        if (siguiente is not null)
        {
            siguiente.Activar(now);
            return ResultadoAvance.Avanzado(actual, siguiente);
        }

        Estado = EstadoSesion.Terminada;
        FechaFin = now;
        return ResultadoAvance.Terminada(actual);
    }

    public ResultadoRespuesta ResponderPregunta(Guid participanteId, Guid opcionId, DateTime now)
    {
        Guid? equipoId = null;
        if (Modalidad == Modalidad.Equipo)
        {
            var inscripcion = _inscripciones.FirstOrDefault(i => i.EsActiva
                    && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada))
                ?? throw new ParticipanteNoInscritoException(participanteId);
            equipoId = inscripcion.EquipoId;
        }
        else if (!_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
        {
            throw new ParticipanteNoInscritoException(participanteId);
        }

        var juego = JuegoTriviaActivo();
        var activa = juego.PreguntaActiva ?? throw new NoHayPreguntaActivaException(PartidaId);

        var resultado = activa.RegistrarRespuesta(participanteId, equipoId, opcionId, now) with { JuegoId = juego.JuegoId };
        if (resultado.CerroPregunta)
            juego.ActivarSiguientePregunta(now); // RF-22: al cerrar por acierto, auto-activar la siguiente
        return resultado;
    }

    public ResultadoAvancePregunta AvanzarPregunta(DateTime now)
    {
        var juego = JuegoTriviaActivo();
        var activa = juego.PreguntaActiva ?? throw new NoHayPreguntaActivaException(PartidaId);

        var vencida = now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos);
        var motivo = vencida ? MotivoCierrePregunta.Tiempo : MotivoCierrePregunta.AvanceOperador;
        activa.Cerrar(motivo, now, ganador: null);

        var siguiente = juego.ActivarSiguientePregunta(now);
        return new ResultadoAvancePregunta(
            juego.JuegoId, activa.PreguntaId, activa.Orden, motivo,
            siguiente?.PreguntaId, siguiente?.Orden, siguiente?.TiempoLimiteSegundos, siguiente?.FechaActivacion,
            siguiente is null);
    }

    public ResultadoRegistroTesoro ValidarTesoro(Guid participanteId, byte[] imagen, DateTime now, Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder decoder)
    {
        Guid? equipoId = null;
        if (Modalidad == Modalidad.Equipo)
        {
            var inscripcion = _inscripciones.FirstOrDefault(i => i.EsActiva
                    && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada))
                ?? throw new ParticipanteNoInscritoException(participanteId);
            equipoId = inscripcion.EquipoId;
        }
        else if (!_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
        {
            throw new ParticipanteNoInscritoException(participanteId);
        }
        var juego = JuegoBDTActivo();
        var activa = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);

        var texto = decoder.Decodificar(imagen);
        var resultado = ClasificarQr(texto, activa, juego);

        var reg = activa.RegistrarTesoro(participanteId, equipoId, texto, resultado, now);

        if (reg.Gano)
        {
            juego.ActivarSiguienteEtapa(now);
        }
        else if (now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
        {
            activa.CerrarPorTiempo(now);
            juego.ActivarSiguienteEtapa(now);
        }

        return new ResultadoRegistroTesoro(
            resultado, reg.CerroEtapa || (!reg.Gano && activa.Estado == EstadoEtapa.CerradaPorTiempo),
            reg.Gano, reg.Puntaje, juego.JuegoId, activa.EtapaId, participanteId,
            reg.Gano ? participanteId : null, reg.TiempoResolucionMs, texto, now,
            equipoId, reg.Gano ? equipoId : null);
    }

    public ResultadoAvanceEtapa AvanzarEtapa(DateTime now)
    {
        var juego = JuegoBDTActivo();
        var activa = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);

        var vencida = now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos);
        if (vencida) activa.CerrarPorTiempo(now); else activa.CerrarPorOperador(now);
        var motivo = vencida ? MotivoCierreEtapa.Tiempo : MotivoCierreEtapa.AvanceOperador;

        var siguiente = juego.ActivarSiguienteEtapa(now);
        return new ResultadoAvanceEtapa(
            juego.JuegoId, activa.EtapaId, activa.Orden, motivo,
            siguiente?.EtapaId, siguiente?.Orden, siguiente?.TiempoLimiteSegundos, siguiente?.FechaActivacion,
            siguiente is null);
    }

    public Guid PrepararPista(Guid participanteDestinoId)
    {
        if (!_inscripciones.Any(i => i.ParticipanteId == participanteDestinoId && i.EsActiva))
            throw new ParticipanteNoInscritoException(participanteDestinoId);
        var juego = JuegoBDTActivo();
        _ = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);
        return juego.JuegoId;
    }

    public Guid PrepararPistaEquipo(Guid equipoDestinoId)
    {
        if (Modalidad != Modalidad.Equipo)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (!_inscripciones.Any(i => i.EquipoId == equipoDestinoId && i.EsActiva))
            throw InscripcionNoEncontradaException.ParaEquipo(equipoDestinoId);
        var juego = JuegoBDTActivo();
        _ = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);
        return juego.JuegoId;
    }

    public ResultadoCierreVencido CerrarActividadVencida(DateTime now)
    {
        if (Estado != EstadoSesion.Iniciada)
            return ResultadoCierreVencido.Ninguna;

        var juego = _juegos.SingleOrDefault(j => j.Estado == EstadoJuego.Activo);
        if (juego is null)
            return ResultadoCierreVencido.Ninguna;

        if (juego.TipoJuego == TipoJuego.Trivia)
        {
            var activa = juego.PreguntaActiva;
            if (activa is null || now < activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
                return ResultadoCierreVencido.Ninguna;

            var rp = AvanzarPregunta(now); // vencida → MotivoCierre.Tiempo
            var fin = rp.SinMasPreguntas ? FinalizarJuegoActual(now) : null;
            return ResultadoCierreVencido.Trivia(rp, fin);
        }

        if (juego.TipoJuego == TipoJuego.BusquedaDelTesoro)
        {
            var activa = juego.EtapaActiva;
            if (activa is null || now < activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
                return ResultadoCierreVencido.Ninguna;

            var re = AvanzarEtapa(now); // vencida → MotivoCierre.Tiempo
            var fin = re.SinMasEtapas ? FinalizarJuegoActual(now) : null;
            return ResultadoCierreVencido.Bdt(re, fin);
        }

        return ResultadoCierreVencido.Ninguna;
    }

    private JuegoResumen JuegoBDTActivo()
    {
        if (Estado != EstadoSesion.Iniciada)
            throw new SesionNoIniciadaException(PartidaId);
        var juego = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        if (juego.TipoJuego != TipoJuego.BusquedaDelTesoro)
            throw new JuegoActivoNoEsBDTException(PartidaId);
        return juego;
    }

    private static ResultadoValidacionQR ClasificarQr(string? texto, EtapaSnapshot activa, JuegoResumen juego)
    {
        if (texto is null) return ResultadoValidacionQR.NoLegible;
        if (texto == activa.CodigoQREsperado) return ResultadoValidacionQR.Valido;
        if (juego.Etapas.Any(e => e.CodigoQREsperado == texto)) return ResultadoValidacionQR.NoCorrespondeEtapaActiva;
        return ResultadoValidacionQR.Invalido;
    }

    private JuegoResumen JuegoTriviaActivo()
    {
        if (Estado != EstadoSesion.Iniciada)
            throw new SesionNoIniciadaException(PartidaId);
        var juego = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        if (juego.TipoJuego != TipoJuego.Trivia)
            throw new JuegoActivoNoEsTriviaException(PartidaId);
        return juego;
    }

    private ResultadoInicio AplicarInicio(DateTime now)
    {
        var participantes = Modalidad == Modalidad.Equipo
            ? _inscripciones.Count(i => i.EsActiva && i.ConvocatoriasAceptadas >= 1)
            : _inscripciones.Count(i => i.EsActiva);
        if (participantes < MinimosParticipacion)
        {
            Estado = EstadoSesion.Cancelada;
            FechaFin = now;
            return ResultadoInicio.Cancelada;
        }

        Estado = EstadoSesion.Iniciada;
        FechaInicio = now;
        var primero = _juegos.OrderBy(j => j.Orden).First();
        primero.Activar(now);
        return ResultadoInicio.Iniciada(primero);
    }

    private static void ValidarPublicabilidad(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        if (snapshot.Juegos.Count == 0)
            throw new PartidaNoPublicableException(partidaId);
        var ordenes = snapshot.Juegos.Select(j => j.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new PartidaNoPublicableException(partidaId);
        }
    }
}
