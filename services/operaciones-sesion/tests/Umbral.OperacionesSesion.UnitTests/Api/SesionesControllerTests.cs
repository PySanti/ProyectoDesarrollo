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
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.UnitTests.Api;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerTests
{
    private static SesionesController ControllerWith(FakeSender sender, Guid? participanteId = null, string? authHeader = null)
    {
        var http = new DefaultHttpContext();
        if (authHeader is not null) http.Request.Headers.Authorization = authHeader;
        if (participanteId is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", participanteId.Value.ToString()) }, "test"));
        return new SesionesController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    private static LobbyDto Lobby(Guid partidaId) =>
        new(partidaId, Guid.NewGuid(), "Lobby", "Individual", 1, 10, 0, Array.Empty<Guid>(), Array.Empty<EquipoLobbyDto>(),
            Array.Empty<SolicitudIndividualDto>(), Array.Empty<SolicitudEquipoDto>());

    [Fact]
    public async Task Publicar_returns_201_and_forwards_bearer()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(Lobby(partidaId));
        var controller = ControllerWith(sender, authHeader: "Bearer xyz");

        var result = await controller.Publicar(partidaId, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        var command = Assert.IsType<PublicarPartidaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, command.PartidaId);
        Assert.Equal("Bearer xyz", command.BearerToken);
    }

    [Fact]
    public async Task Inscribir_uses_sub_claim_and_returns_201()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var sender = new FakeSender(new InscripcionResponse(Guid.NewGuid(), partidaId, participante));
        var controller = ControllerWith(sender, participanteId: participante);

        var result = await controller.Inscribir(partidaId, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var command = Assert.IsType<InscribirParticipanteCommand>(sender.LastRequest);
        Assert.Equal(participante, command.ParticipanteId);
    }

    [Fact]
    public async Task Inscribir_without_sub_claim_throws()
    {
        var controller = ControllerWith(new FakeSender(null));
        await Assert.ThrowsAsync<ParticipanteNoIdentificadoException>(
            () => controller.Inscribir(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CancelarInscripcion_returns_204()
    {
        var participante = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(MediatR.Unit.Value);
        var controller = ControllerWith(sender, participanteId: participante);

        var result = await controller.CancelarInscripcion(partidaId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<CancelarInscripcionCommand>(sender.LastRequest);
        Assert.Equal(participante, command.ParticipanteId);
        Assert.Equal(partidaId, command.PartidaId);
    }

    [Fact]
    public async Task Publicar_without_auth_header_sends_null_bearer()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(Lobby(partidaId));
        var controller = ControllerWith(sender); // no authHeader

        var result = await controller.Publicar(partidaId, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var command = Assert.IsType<PublicarPartidaCommand>(sender.LastRequest);
        Assert.Null(command.BearerToken);
    }

    [Fact]
    public async Task Inscribir_falls_back_to_NameIdentifier_when_sub_absent()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, participante.ToString()) }, "test"));
        var sender = new FakeSender(new InscripcionResponse(Guid.NewGuid(), partidaId, participante));
        var controller = new SesionesController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };

        var result = await controller.Inscribir(partidaId, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        var command = Assert.IsType<InscribirParticipanteCommand>(sender.LastRequest);
        Assert.Equal(participante, command.ParticipanteId);
    }

    [Fact]
    public async Task ObtenerLobby_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(Lobby(partidaId));
        var controller = ControllerWith(sender);

        var result = await controller.ObtenerLobby(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LobbyDto>(ok.Value);
    }

    [Fact]
    public async Task Iniciar_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new InicioPartidaResponse(partidaId, "Iniciada", Guid.NewGuid(), 1));
        var controller = ControllerWith(sender);

        var result = await controller.Iniciar(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var command = Assert.IsType<IniciarPartidaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, command.PartidaId);
    }

    [Fact]
    public async Task IniciarAutomatico_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new InicioPartidaResponse(partidaId, "Lobby", null, null));
        var controller = ControllerWith(sender);

        var result = await controller.IniciarAutomatico(partidaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<IntentarInicioAutomaticoCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task FinalizarJuegoActual_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new AvanceJuegoResponse(partidaId, "Iniciada", 1, 2, false));
        var controller = ControllerWith(sender);

        var result = await controller.FinalizarJuegoActual(partidaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<FinalizarJuegoActualCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ObtenerEstado_returns_200_and_dispatches_query()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new EstadoSesionDto(partidaId, Guid.NewGuid(), "Lobby", "Individual", Array.Empty<JuegoEstadoDto>(), null));
        var controller = ControllerWith(sender);

        var result = await controller.ObtenerEstado(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<EstadoSesionDto>(ok.Value);
        Assert.IsType<ObtenerEstadoSesionQuery>(sender.LastRequest);
    }
}
