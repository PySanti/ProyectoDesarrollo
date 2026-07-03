using System.Reflection;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerEtapaActualQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_stage_without_leaking_expected_qr()
    {
        var (repo, _, _, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new ObtenerEtapaActualQueryHandler(repo);
        var dto = await handler.Handle(new ObtenerEtapaActualQuery(partidaId), default);
        Assert.Equal(1, dto.Orden);
        // No-leak: el DTO no expone CodigoQREsperado en ninguna propiedad
        Assert.Null(typeof(EtapaActualDto).GetProperty("CodigoQREsperado", BindingFlags.Public | BindingFlags.Instance));
    }
}
