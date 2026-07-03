// tests/Umbral.OperacionesSesion.UnitTests/Domain/CerrarActividadVencidaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class CerrarActividadVencidaTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaIniciada(params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.Iniciar(T0);
        return s;
    }

    private static SesionPartida BdtIniciada(params (string qr, int limite)[] etapas)
    {
        var snapEtapas = etapas.Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.qr, 50, e.limite)).ToArray();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Área", snapEtapas);
        var snap = new ConfiguracionSnapshot("Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.Iniciar(T0);
        return s;
    }

    [Fact]
    public void No_op_cuando_pregunta_no_vencida()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        var r = s.CerrarActividadVencida(T0.AddSeconds(10)); // dentro de ventana
        Assert.False(r.HuboCambio);
        Assert.Equal(TipoCierreVencido.Ninguna, r.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, s.Estado);
    }

    [Fact]
    public void Trivia_vencida_cierra_por_tiempo_y_activa_siguiente()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        var r = s.CerrarActividadVencida(T0.AddSeconds(31));
        Assert.Equal(TipoCierreVencido.Trivia, r.Tipo);
        Assert.Equal(MotivoCierrePregunta.Tiempo, r.Pregunta!.MotivoCierre);
        Assert.Equal(2, r.Pregunta.PreguntaActivadaOrden);
        Assert.Null(r.JuegoFinalizado);
    }

    [Fact]
    public void Trivia_ultima_vencida_finaliza_y_termina_partida()
    {
        var s = TriviaIniciada(P(1, 30)); // única pregunta del único juego
        var r = s.CerrarActividadVencida(T0.AddSeconds(31));
        Assert.Equal(TipoCierreVencido.Trivia, r.Tipo);
        Assert.True(r.Pregunta!.SinMasPreguntas);
        Assert.NotNull(r.JuegoFinalizado);
        Assert.True(r.JuegoFinalizado!.Terminada());
        Assert.Equal(EstadoSesion.Terminada, s.Estado);
    }

    [Fact]
    public void Bdt_vencida_cierra_por_tiempo_y_activa_siguiente()
    {
        var s = BdtIniciada(("QR-1", 60), ("QR-2", 60));
        var r = s.CerrarActividadVencida(T0.AddSeconds(61));
        Assert.Equal(TipoCierreVencido.Bdt, r.Tipo);
        Assert.Equal(MotivoCierreEtapa.Tiempo, r.Etapa!.MotivoCierre);
        Assert.Equal(2, r.Etapa.EtapaActivadaOrden);
        Assert.Null(r.JuegoFinalizado);
    }

    [Fact]
    public void Bdt_ultima_vencida_finaliza_y_termina_partida()
    {
        var s = BdtIniciada(("QR-1", 60));
        var r = s.CerrarActividadVencida(T0.AddSeconds(61));
        Assert.Equal(TipoCierreVencido.Bdt, r.Tipo);
        Assert.True(r.Etapa!.SinMasEtapas);
        Assert.NotNull(r.JuegoFinalizado);
        Assert.True(r.JuegoFinalizado!.Terminada());
        Assert.Equal(EstadoSesion.Terminada, s.Estado);
    }

    [Fact]
    public void Idempotente_segunda_llamada_es_no_op()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        s.CerrarActividadVencida(T0.AddSeconds(31)); // cierra Q1, activa Q2 (FechaActivacion = ese now)
        var r2 = s.CerrarActividadVencida(T0.AddSeconds(31)); // Q2 recién activada, no vencida
        Assert.False(r2.HuboCambio);
    }
}
