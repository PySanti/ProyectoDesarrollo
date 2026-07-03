using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerMisConvocatoriasTests
{
    private static SesionesController WithUser(FakeSender sender, Guid sub)
    {
        var controller = new SesionesController(sender);
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", sub.ToString()) }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = ctx };
        return controller;
    }

    [Fact]
    public async Task ObtenerMisConvocatorias_despacha_query_con_usuario_del_token_y_devuelve_200()
    {
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new List<ConvocatoriaPendienteDto>());
        var controller = WithUser(sender, usuario);

        var result = await controller.ObtenerMisConvocatorias(CancellationToken.None);

        var query = Assert.IsType<ObtenerMisConvocatoriasPendientesQuery>(sender.LastRequest);
        Assert.Equal(usuario, query.UsuarioId);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ObtenerMisConvocatorias_devuelve_200_con_lista_vacia_cuando_no_hay_pendientes()
    {
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new List<ConvocatoriaPendienteDto>());
        var controller = WithUser(sender, usuario);

        var result = await controller.ObtenerMisConvocatorias(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var lista = Assert.IsAssignableFrom<IReadOnlyList<ConvocatoriaPendienteDto>>(ok.Value);
        Assert.Empty(lista);
    }
}
