using System;
using System.Collections.Generic;
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

public class SesionesControllerTriviaTests
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
    public async Task Responder_dispatches_command_with_claim_sub_and_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var opcion = Guid.NewGuid();
        var sender = new FakeSender(new RespuestaTriviaResponse(partidaId, Guid.NewGuid(), true, true, 10));
        var controller = WithUser(sender, sub);

        var result = await controller.Responder(partidaId, new ResponderPreguntaRequest(opcion), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var command = Assert.IsType<ResponderPreguntaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, command.PartidaId);
        Assert.Equal(sub, command.ParticipanteId);
        Assert.Equal(opcion, command.OpcionId);
    }

    [Fact]
    public async Task Avanzar_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new AvancePreguntaResponse(partidaId, 1, 2, false));
        var controller = WithUser(sender, Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.Avanzar(partidaId, CancellationToken.None));
    }

    [Fact]
    public async Task PreguntaActual_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new PreguntaActualDto(partidaId, Guid.NewGuid(), Guid.NewGuid(), 1, "Q", 30, DateTime.UtcNow,
            new List<OpcionPublicaDto>()));
        var controller = WithUser(sender, Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.ObtenerPreguntaActual(partidaId, CancellationToken.None));
    }
}
