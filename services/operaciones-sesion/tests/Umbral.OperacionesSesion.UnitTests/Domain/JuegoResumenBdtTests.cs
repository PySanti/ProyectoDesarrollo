// tests/.../Domain/JuegoResumenBdtTests.cs
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class JuegoResumenBdtTests
{
    private static JuegoResumen JuegoBdt(params int[] ordenes)
    {
        var etapas = ordenes.Select(o => new EtapaSnapshot(Guid.NewGuid(), o, $"QR-{o}", 50, 60)).ToList();
        return new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "", etapas);
    }

    [Fact]
    public void Activar_bdt_activa_primera_etapa_por_orden()
    {
        var juego = JuegoBdt(2, 1);
        juego.Activar(new DateTime(2026, 6, 28));
        Assert.Equal(EstadoJuego.Activo, juego.Estado);
        Assert.NotNull(juego.EtapaActiva);
        Assert.Equal(1, juego.EtapaActiva!.Orden);
        Assert.True(juego.TieneEtapasAbiertas);
    }

    [Fact]
    public void ActivarSiguienteEtapa_avanza_a_la_proxima_pendiente()
    {
        var juego = JuegoBdt(1, 2);
        var now = new DateTime(2026, 6, 28);
        juego.Activar(now);
        juego.EtapaActiva!.CerrarPorOperador(now); // cierra la 1
        var siguiente = juego.ActivarSiguienteEtapa(now);
        Assert.NotNull(siguiente);
        Assert.Equal(2, siguiente!.Orden);
        Assert.Equal(2, juego.EtapaActiva!.Orden);
    }

    [Fact]
    public void Bdt_sin_etapas_no_tiene_etapa_activa()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "", Array.Empty<EtapaSnapshot>());
        juego.Activar(new DateTime(2026, 6, 28));
        Assert.Null(juego.EtapaActiva);
        Assert.False(juego.TieneEtapasAbiertas);
    }
}
