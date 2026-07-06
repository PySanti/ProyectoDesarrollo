using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class EquiposControllerTests
{
    [Fact]
    public async Task ObtenerRendimiento_despacha_query_y_devuelve_ok()
    {
        var equipoId = Guid.NewGuid();
        var respuesta = new RendimientoEquipoResponse(equipoId, Array.Empty<RendimientoPartidaDto>());
        var sender = new FakeSender(respuesta);
        var controller = new EquiposController(sender);

        var result = await controller.ObtenerRendimiento(equipoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerRendimientoEquipoQuery>(sender.LastRequest);
        Assert.Equal(equipoId, query.EquipoId);
    }
}
