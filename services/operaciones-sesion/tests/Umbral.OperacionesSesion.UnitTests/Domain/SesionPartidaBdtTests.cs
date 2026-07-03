// SesionPartidaBdtTests.cs
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaBdtTests
{
    // Decoder fake: interpreta los bytes como el texto UTF-8 del QR.
    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static byte[] Img(string texto) => System.Text.Encoding.UTF8.GetBytes(texto);

    // Helper: publica + inscribe + inicia una sesión BDT Individual con las etapas dadas (qr, limite).
    // Reutiliza el ConfiguracionSnapshot del dominio igual que en SesionPartidaTriviaTests (3c).
    private static SesionPartida SesionBdtIniciada(Guid participante, params (string Qr, int Limite)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var etapasSnap = etapas.Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.Qr, 50, e.Limite)).ToList();
        var juego = new JuegoResumen(juegoId, 1, TipoJuego.BusquedaDelTesoro, "", etapasSnap);
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snapshot);
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        sesion.Inscribir(participante, false, 0, now);
        sesion.Iniciar(now);
        return sesion;
    }

    private static SesionPartida SesionTriviaIniciada(Guid participante)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snapshot);
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        sesion.Inscribir(participante, false, 0, now);
        sesion.Iniciar(now);
        return sesion;
    }

    [Fact]
    public void ValidarTesoro_correcto_gana_y_auto_avanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-1"), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.Valido, r.Resultado);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        // auto-avance: la etapa activa ahora es la 2
        var juego = sesion.Juegos.Single();
        Assert.Equal(2, juego.EtapaActiva!.Orden);
    }

    [Fact]
    public void ValidarTesoro_incorrecto_registra_sin_ganar()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-OTRO"), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.Invalido, r.Resultado);
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, sesion.Juegos.Single().EtapaActiva!.Estado);
    }

    [Fact]
    public void ValidarTesoro_qr_de_otra_etapa_es_NoCorrespondeEtapaActiva()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-2"), now, new TextoQrDecoder()); // QR de la etapa 2 mientras la activa es la 1
        Assert.Equal(ResultadoValidacionQR.NoCorrespondeEtapaActiva, r.Resultado);
        Assert.False(r.Gano);
    }

    [Fact]
    public void ValidarTesoro_imagen_ilegible_es_NoLegible()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Array.Empty<byte>(), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.NoLegible, r.Resultado);
    }

    [Fact]
    public void ValidarTesoro_sin_inscripcion_lanza_403_antes_que_409()
    {
        var jugador = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        Assert.Throws<ParticipanteNoInscritoException>(() =>
            sesion.ValidarTesoro(intruso, Img("QR-1"), now, new TextoQrDecoder()));
    }

    [Fact]
    public void AvanzarEtapa_operador_cierra_sin_ganador_y_activa_siguiente()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.AvanzarEtapa(now);
        Assert.Equal(MotivoCierreEtapa.AvanceOperador, r.MotivoCierre);
        Assert.False(r.SinMasEtapas);
        Assert.Equal(2, r.EtapaActivadaOrden);
    }

    [Fact]
    public void FinalizarJuegoActual_con_etapa_abierta_lanza_JuegoConEtapasPendientes()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        Assert.Throws<JuegoConEtapasPendientesException>(() => sesion.FinalizarJuegoActual(now));
    }

    [Fact]
    public void ValidarTesoro_correcto_fuera_de_tiempo_registra_sin_ganar_y_cierra()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 30), ("QR-2", 60));
        // now = 10:01:00 → 60s > limite de 30s
        var now = new DateTime(2026, 6, 28, 10, 1, 0);
        var r = sesion.ValidarTesoro(jugador, Img("QR-1"), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.Valido, r.Resultado);
        Assert.False(r.Gano);
        Assert.True(r.CerroEtapa);          // compound: camino por tiempo
        Assert.Null(r.GanadorParticipanteId);
        Assert.Equal(2, sesion.Juegos.Single().EtapaActiva!.Orden); // auto-avance
    }

    [Fact]
    public void AvanzarEtapa_etapa_vencida_por_tiempo_avanza_con_motivo_tiempo()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 30), ("QR-2", 60));
        // now = 10:01:00 → 60s > limite de 30s
        var now = new DateTime(2026, 6, 28, 10, 1, 0);
        var r = sesion.AvanzarEtapa(now);
        Assert.Equal(MotivoCierreEtapa.Tiempo, r.MotivoCierre);
        Assert.False(r.SinMasEtapas);
        Assert.Equal(2, r.EtapaActivadaOrden);
    }

    [Fact]
    public void AvanzarEtapa_ultima_etapa_sin_mas_etapas()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.AvanzarEtapa(now);
        Assert.True(r.SinMasEtapas);
        Assert.Null(r.EtapaActivadaOrden);
    }

    // FU2: 403-antes-409 — el chequeo de inscripción debe preceder al de estado del juego.
    // Si no se moviera el guard, este test fallaría con NoHayEtapaActivaException (409)
    // porque la única etapa ya fue cerrada por el operador antes de que llegue el intruso.
    [Fact]
    public void ValidarTesoro_sin_inscripcion_lanza_403_aunque_no_haya_etapa_activa()
    {
        var jugador = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);

        // Operador avanza la única etapa → ya no hay etapa activa (daría NoHayEtapaActivaException/409 si llegara)
        sesion.AvanzarEtapa(now);

        // El intruso llama ValidarTesoro: debe obtener 403 (inscripción) antes que 409 (sin etapa activa)
        Assert.Throws<ParticipanteNoInscritoException>(() =>
            sesion.ValidarTesoro(intruso, Img("QR-1"), now.AddSeconds(1), new TextoQrDecoder()));
    }

    [Fact]
    public void PrepararPista_con_bdt_activo_y_destino_inscrito_retorna_juegoId()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var juegoId = sesion.Juegos.Single().JuegoId;

        var r = sesion.PrepararPista(jugador);

        Assert.Equal(juegoId, r);
    }

    [Fact]
    public void PrepararPista_destino_no_inscrito_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));

        Assert.Throws<ParticipanteNoInscritoException>(() => sesion.PrepararPista(Guid.NewGuid()));
    }

    [Fact]
    public void PrepararPista_juego_activo_no_es_bdt_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionTriviaIniciada(jugador);

        Assert.Throws<JuegoActivoNoEsBDTException>(() => sesion.PrepararPista(jugador));
    }

    [Fact]
    public void PrepararPista_bdt_sin_etapa_activa_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        // ganar la única etapa deja el BDT activo pero sin etapa activa (no finaliza el juego)
        sesion.ValidarTesoro(jugador, Img("QR-1"), new DateTime(2026, 6, 28, 10, 0, 5), new TextoQrDecoder());
        Assert.Null(sesion.Juegos.Single().EtapaActiva);

        Assert.Throws<NoHayEtapaActivaException>(() => sesion.PrepararPista(jugador));
    }
}
