using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class HistorialControllerTests
{
    [Fact]
    public void Exige_la_policy_OperadorOAdminGestionarPartidas()
    {
        // Task 5: rol AND privilegio, no solo el rol — ver Program.cs para la composición de la policy.
        var attribute = typeof(HistorialController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("OperadorOAdminGestionarPartidas", attribute.Policy);
    }

    [Fact]
    public async Task ObtenerHistorial_despacha_la_query_con_los_parametros()
    {
        var partidaId = Guid.NewGuid();
        var esperado = new HistorialPartidaResponse(partidaId, 0, Array.Empty<EntradaHistorialDto>());
        var sender = new FakeSender(esperado);
        var controller = new HistorialController(sender);

        var resultado = await controller.ObtenerHistorial(partidaId, 50, 10, "EtapaBDTGanada", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
        var query = Assert.IsType<ObtenerHistorialPartidaQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(50, query.Limit);
        Assert.Equal(10, query.Offset);
        Assert.Equal("EtapaBDTGanada", query.TipoEvento);
    }
}
