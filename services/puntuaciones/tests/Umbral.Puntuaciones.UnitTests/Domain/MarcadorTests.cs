using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.Domain.Exceptions;

namespace Umbral.Puntuaciones.UnitTests.Domain;

public class MarcadorTests
{
    private static Marcador NuevoMarcador() =>
        Marcador.Nuevo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoCompetidor.Participante);

    [Fact]
    public void Nuevo_inicia_en_cero()
    {
        var m = NuevoMarcador();

        Assert.Equal(0, m.PuntosAcumulados);
        Assert.Equal(0, m.TiempoAcumuladoMs);
        Assert.Equal(0, m.UnidadesGanadas);
    }

    [Fact]
    public void Acreditar_acumula_puntos_tiempo_y_unidades()
    {
        var m = NuevoMarcador();

        m.Acreditar(10, 1500);
        m.Acreditar(5, 500);

        Assert.Equal(15, m.PuntosAcumulados);
        Assert.Equal(2000, m.TiempoAcumuladoMs);
        Assert.Equal(2, m.UnidadesGanadas);
    }

    [Fact]
    public void Acreditar_puntos_negativos_lanza()
    {
        var m = NuevoMarcador();

        Assert.Throws<PuntuacionInvalidaException>(() => m.Acreditar(-1, 100));
    }

    [Fact]
    public void Acreditar_tiempo_negativo_lanza()
    {
        var m = NuevoMarcador();

        Assert.Throws<PuntuacionInvalidaException>(() => m.Acreditar(1, -100));
    }
}
