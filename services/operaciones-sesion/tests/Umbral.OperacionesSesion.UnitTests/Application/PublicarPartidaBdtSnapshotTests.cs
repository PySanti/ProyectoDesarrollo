using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaBdtSnapshotTests
{
    [Fact]
    public void MapearJuego_bdt_construye_etapas_snapshot()
    {
        var juegoId = Guid.NewGuid();
        var e1 = Guid.NewGuid(); var e2 = Guid.NewGuid();
        var dto = new JuegoResumenDto(juegoId, 1, "BusquedaDelTesoro", Trivia: null,
            Bdt: new BdtConfigDto("Plaza central", new List<EtapaConfigDto>
            {
                new(e1, 1, "QR-A", 50, 120),
                new(e2, 2, "QR-B", 70, 90),
            }));

        // Valida la forma del DTO de config para fijar el contrato.
        Assert.Equal("BusquedaDelTesoro", dto.TipoJuego);
        Assert.NotNull(dto.Bdt);
        Assert.Equal(2, dto.Bdt!.Etapas.Count);
        Assert.Equal("QR-A", dto.Bdt.Etapas[0].CodigoQREsperado);
        Assert.Equal(50, dto.Bdt.Etapas[0].PuntajeAsignado);
        Assert.Equal(120, dto.Bdt.Etapas[0].TiempoLimiteSegundos);
        Assert.Equal("Plaza central", dto.Bdt.AreaBusqueda);
    }
}
