using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerBdtTests
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
    public async Task Validar_tesoro_dispatches_command_with_sub_and_imagen()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var sender = new FakeSender(new ValidacionTesoroResponse(partidaId, Guid.NewGuid(), "Valido", true, true, 50));
        var controller = WithUser(sender, sub);

        var result = await controller.ValidarTesoro(partidaId, new ValidarTesoroRequest("Zm9v"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ValidacionTesoroResponse>(ok.Value);
        var cmd = Assert.IsType<ValidarTesoroCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(sub, cmd.ParticipanteId);
        Assert.Equal("Zm9v", cmd.ImagenBase64);
    }

    [Fact]
    public async Task Avanzar_etapa_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new AvanceEtapaResponse(partidaId, 1, 2, false));
        var controller = WithUser(sender, Guid.NewGuid());

        var result = await controller.AvanzarEtapa(partidaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AvanzarEtapaCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Enviar_pista_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var destino = Guid.NewGuid();
        var sender = new FakeSender(new PistaEnviadaResponse(partidaId, Guid.NewGuid(), destino,
            new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc)));
        var controller = WithUser(sender, Guid.NewGuid());

        var result = await controller.EnviarPista(partidaId, new EnviarPistaRequest(destino, "Mira el faro"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<EnviarPistaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(destino, cmd.ParticipanteDestinoId);
        Assert.Equal("Mira el faro", cmd.Texto);
    }
}
