using System;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class PartidaTests
{
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static Partida CrearManual() =>
        Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);

    [Fact]
    public void Crear_manual_sets_null_estado_and_no_games()
    {
        var partida = CrearManual();
        Assert.Null(partida.Estado);          // SEAM: not published yet
        Assert.True(partida.PartidaId.EsValido());
        Assert.Empty(partida.Juegos);
    }

    // El estado de runtime lo escribe SOLO la proyección de eventos de Operaciones de Sesión
    // (fix 4): PartidaPublicadaEnLobby/Iniciada/Cancelada/Finalizada → Estado.
    [Fact]
    public void ProyectarEstado_desde_null_aplica_el_estado_de_runtime()
    {
        var partida = CrearManual();

        partida.ProyectarEstado(EstadoPartida.Lobby);

        Assert.Equal(EstadoPartida.Lobby, partida.Estado);
    }

    [Fact]
    public void ProyectarEstado_progresa_por_el_ciclo_de_vida()
    {
        var partida = CrearManual();

        partida.ProyectarEstado(EstadoPartida.Lobby);
        partida.ProyectarEstado(EstadoPartida.Iniciada);
        partida.ProyectarEstado(EstadoPartida.Terminada);

        Assert.Equal(EstadoPartida.Terminada, partida.Estado);
    }

    // RabbitMQ topic no garantiza orden entre routing keys distintas: un Iniciada/Lobby rezagado
    // no debe resucitar una partida ya Cancelada o Terminada.
    [Fact]
    public void ProyectarEstado_no_pisa_un_estado_terminal_Cancelada()
    {
        var partida = CrearManual();
        partida.ProyectarEstado(EstadoPartida.Lobby);
        partida.ProyectarEstado(EstadoPartida.Cancelada);

        partida.ProyectarEstado(EstadoPartida.Iniciada); // evento rezagado

        Assert.Equal(EstadoPartida.Cancelada, partida.Estado);
    }

    [Fact]
    public void ProyectarEstado_no_pisa_un_estado_terminal_Terminada()
    {
        var partida = CrearManual();
        partida.ProyectarEstado(EstadoPartida.Iniciada);
        partida.ProyectarEstado(EstadoPartida.Terminada);

        partida.ProyectarEstado(EstadoPartida.Lobby); // evento rezagado

        Assert.Equal(EstadoPartida.Terminada, partida.Estado);
    }

    [Fact]
    public void Crear_guarda_la_fecha_de_creacion_que_recibe()
    {
        var partida = Partida.Crear(
            NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);

        // El dominio no lee el reloj: guarda el instante que le pasan. Eso es lo que hace
        // deterministas al test de orden del repositorio y al del handler.
        Assert.Equal(T0, partida.FechaCreacion);
    }

    [Fact]
    public void Crear_automatico_requires_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Automatico, null, 1, 10, T0));
    }

    [Fact]
    public void Crear_manual_rejects_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, DateTime.UtcNow, 1, 10, T0));
    }

    [Fact]
    public void Crear_rejects_maximos_below_minimos()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 5, 2, T0));
    }

    [Fact]
    public void AgregarJuego_appends_reference()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Single(partida.Juegos);
        Assert.Equal(juegoId, partida.Juegos[0].JuegoId);
        Assert.Equal(TipoJuego.Trivia, partida.Juegos[0].TipoJuego);
    }

    [Fact]
    public void AgregarJuego_rejects_orden_below_one()
    {
        var partida = CrearManual();
        Assert.Throws<ArgumentException>(() => partida.AgregarJuego(JuegoId.New(), 0, TipoJuego.Trivia));
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        Assert.Throws<OrdenJuegoDuplicadoException>(() => partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.BusquedaDelTesoro));
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_juego_id()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Throws<JuegoDuplicadoException>(() => partida.AgregarJuego(juegoId, 2, TipoJuego.Trivia));
    }

    [Fact]
    public void ValidarIntegridadJuegos_throws_when_no_games()
    {
        Assert.Throws<PartidaSinJuegosException>(() => CrearManual().ValidarIntegridadJuegos());
    }

    [Fact]
    public void ValidarIntegridadJuegos_throws_when_orden_not_contiguous()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 3, TipoJuego.Trivia); // gap
        Assert.Throws<OrdenJuegosNoContiguoException>(() => partida.ValidarIntegridadJuegos());
    }

    [Fact]
    public void ValidarIntegridadJuegos_passes_for_contiguous_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 2, TipoJuego.BusquedaDelTesoro);
        partida.ValidarIntegridadJuegos(); // no throw
    }

    [Fact]
    public void Crear_manualYAutomatico_requires_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, null, 1, 10, T0));
    }

    [Fact]
    public void Crear_manualYAutomatico_with_tiempo_inicio_succeeds()
    {
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, DateTime.UtcNow, 1, 10, T0);
        Assert.True(partida.PartidaId.EsValido());
    }

    [Fact]
    public void Crear_rejects_minimos_below_one()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 0, 5, T0));
    }
}
