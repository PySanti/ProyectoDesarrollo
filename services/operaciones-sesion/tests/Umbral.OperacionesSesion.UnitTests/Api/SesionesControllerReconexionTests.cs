using System;
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

public class SesionesControllerReconexionTests
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
    public async Task Mi_sesion_con_participacion_devuelve_200_y_dto()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var dto = new MiSesionDto(partidaId, Guid.NewGuid(), "Iniciada", "Individual",
            new InscripcionResumenDto(Guid.NewGuid(), "Activa"), null, null, null, null, null);
        var sender = new FakeSender(dto);
        var controller = WithUser(sender, sub);

        var result = await controller.ObtenerMiSesion(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<MiSesionDto>(ok.Value);
        var query = Assert.IsType<ObtenerMiSesionQuery>(sender.LastRequest);
        Assert.Equal(sub, query.ParticipanteId);
    }

    [Fact]
    public async Task Mi_sesion_sin_participacion_devuelve_204()
    {
        var sender = new FakeSender(null);
        var controller = WithUser(sender, Guid.NewGuid());

        var result = await controller.ObtenerMiSesion(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
