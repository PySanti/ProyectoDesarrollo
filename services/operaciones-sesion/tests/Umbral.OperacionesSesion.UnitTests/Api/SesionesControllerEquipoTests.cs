using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerEquipoTests
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
    public async Task PreinscribirEquipo_dispatches_command_con_lider_del_claim()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var sender = new FakeSender(new PreinscripcionEquipoResponse(Guid.NewGuid(), Guid.NewGuid(), 3));
        var controller = WithUser(sender, lider);

        var result = await controller.PreinscribirEquipo(partidaId, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var cmd = Assert.IsType<PreinscribirEquipoCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(lider, cmd.LiderId);
    }

    [Fact]
    public async Task AceptarConvocatoria_dispatches_con_aceptar_true()
    {
        var convocatoriaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new ConvocatoriaResponse(convocatoriaId, "Aceptada"));
        var controller = WithUser(sender, usuario);

        var result = await controller.AceptarConvocatoria(convocatoriaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<ResponderConvocatoriaCommand>(sender.LastRequest);
        Assert.Equal(convocatoriaId, cmd.ConvocatoriaId);
        Assert.Equal(usuario, cmd.UsuarioId);
        Assert.True(cmd.Aceptar);
    }

    [Fact]
    public async Task RechazarConvocatoria_dispatches_con_aceptar_false()
    {
        var convocatoriaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new ConvocatoriaResponse(convocatoriaId, "Rechazada"));
        var controller = WithUser(sender, usuario);

        var result = await controller.RechazarConvocatoria(convocatoriaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<ResponderConvocatoriaCommand>(sender.LastRequest);
        Assert.False(cmd.Aceptar);
    }

    [Fact]
    public async Task CancelarInscripcionEquipo_dispatches_y_devuelve_204()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var sender = new FakeSender(MediatR.Unit.Value);
        var controller = WithUser(sender, lider);

        var result = await controller.CancelarInscripcionEquipo(partidaId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var cmd = Assert.IsType<CancelarInscripcionEquipoCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(lider, cmd.LiderId);
    }
}
