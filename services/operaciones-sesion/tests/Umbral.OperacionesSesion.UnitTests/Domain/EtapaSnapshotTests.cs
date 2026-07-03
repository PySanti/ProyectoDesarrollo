using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class EtapaSnapshotTests
{
    private static EtapaSnapshot NuevaEtapa(string qr = "QR-1", int limite = 60)
        => new(Guid.NewGuid(), 1, qr, puntaje: 50, tiempoLimiteSegundos: limite);

    [Fact]
    public void Activar_sets_estado_activa_and_fecha()
    {
        var e = NuevaEtapa();
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(now);
        Assert.Equal(EstadoEtapa.Activa, e.Estado);
        Assert.Equal(now, e.FechaActivacion);
    }

    [Fact]
    public void RegistrarTesoro_valido_dentro_de_ventana_gana_y_cierra()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var participante = Guid.NewGuid();
        var r = e.RegistrarTesoro(participante, null, "QR-1", ResultadoValidacionQR.Valido, t0.AddSeconds(5));
        Assert.True(r.CerroEtapa);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        Assert.Equal(5000, r.TiempoResolucionMs);
        Assert.Equal(EstadoEtapa.Ganada, e.Estado);
        Assert.Equal(participante, e.GanadorParticipanteId);
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void RegistrarTesoro_invalido_registra_pero_no_cierra()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var r = e.RegistrarTesoro(Guid.NewGuid(), null, null, ResultadoValidacionQR.NoLegible, t0.AddSeconds(5));
        Assert.False(r.CerroEtapa);
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, e.Estado);
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void RegistrarTesoro_multiples_intentos_se_acumulan()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        e.RegistrarTesoro(Guid.NewGuid(), null, null, ResultadoValidacionQR.Invalido, t0.AddSeconds(1));
        e.RegistrarTesoro(Guid.NewGuid(), null, null, ResultadoValidacionQR.Invalido, t0.AddSeconds(2));
        Assert.Equal(2, e.Tesoros.Count);
    }

    [Fact]
    public void RegistrarTesoro_valido_fuera_de_ventana_registra_pero_no_gana()
    {
        var e = NuevaEtapa(limite: 10);
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var r = e.RegistrarTesoro(Guid.NewGuid(), null, "QR-1", ResultadoValidacionQR.Valido, t0.AddSeconds(20));
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, e.Estado); // el cierre por tiempo lo decide el agregado
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void CerrarPorTiempo_y_CerrarPorOperador_set_estados_distintos()
    {
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        var a = NuevaEtapa(); a.Activar(t0); a.CerrarPorTiempo(t0.AddSeconds(99));
        Assert.Equal(EstadoEtapa.CerradaPorTiempo, a.Estado);
        Assert.Equal(MotivoCierreEtapa.Tiempo, a.MotivoCierre);
        var b = NuevaEtapa(); b.Activar(t0); b.CerrarPorOperador(t0.AddSeconds(3));
        Assert.Equal(EstadoEtapa.Cerrada, b.Estado);
        Assert.Equal(MotivoCierreEtapa.AvanceOperador, b.MotivoCierre);
    }
}
