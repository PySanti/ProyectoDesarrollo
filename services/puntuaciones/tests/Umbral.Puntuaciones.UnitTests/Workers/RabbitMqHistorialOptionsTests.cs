using Umbral.Puntuaciones.Api.Workers;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class RabbitMqHistorialOptionsTests
{
    [Fact]
    public void Defaults_del_contrato_de_transporte()
    {
        var options = new RabbitMqHistorialOptions();

        Assert.Equal("puntuaciones.operaciones-sesion.historial", options.Queue);
        Assert.Equal("operaciones-sesion.#", options.Binding);
        Assert.Equal("RabbitMqHistorial", RabbitMqHistorialOptions.SectionName);
    }
}
