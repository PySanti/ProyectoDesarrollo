using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class TeamInvitationsControllerTests
{
    private static TeamInvitationsController BuildController(FakeSender sender, Guid? sub)
    {
        var controller = new TeamInvitationsController(sender);
        var claims = sub is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim("sub", sub.Value.ToString()) });
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) }
        };
        return controller;
    }

    // ── Enviar ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Enviar_Dispatches_Command_And_Returns_Created()
    {
        var actor = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var invitacionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new EnviarInvitacionEquipoResponse(
            invitacionId, equipoId, invitado, actor, "Pendiente", DateTime.UtcNow);
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.Enviar(
            new EnviarInvitacionRequest(invitado),
            new InlineValidator<EnviarInvitacionEquipoCommand>(),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/api/teams/invitations/{invitacionId}", created.Location);
        var command = Assert.IsType<EnviarInvitacionEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal(invitado, command.InvitadoUserId);
    }

    [Fact]
    public async Task Enviar_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Enviar(
            new EnviarInvitacionRequest(Guid.NewGuid()),
            new InlineValidator<EnviarInvitacionEquipoCommand>(),
            CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Recibidas ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Recibidas_Dispatches_Query_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        IReadOnlyList<InvitacionRecibidasItemResponse> list = Array.Empty<InvitacionRecibidasItemResponse>();
        var sender = new FakeSender { NextResponse = list };
        var controller = BuildController(sender, actor);

        var result = await controller.Recibidas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
        var query = Assert.IsType<GetInvitacionesRecibidasQuery>(sender.LastRequest);
        Assert.Equal(actor, query.ActorUserId);
    }

    [Fact]
    public async Task Recibidas_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Recibidas(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Aceptar ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Aceptar_Dispatches_Command_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var invitacionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new AceptarInvitacionEquipoResponse(invitacionId, equipoId, actor, "Aceptada");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.Aceptar(
            invitacionId,
            new InlineValidator<AceptarInvitacionEquipoCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<AceptarInvitacionEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal(invitacionId, command.InvitacionId);
    }

    [Fact]
    public async Task Aceptar_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Aceptar(
            Guid.NewGuid(),
            new InlineValidator<AceptarInvitacionEquipoCommand>(),
            CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── Rechazar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rechazar_Dispatches_Command_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var invitacionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new RechazarInvitacionEquipoResponse(invitacionId, equipoId, actor, "Rechazada");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.Rechazar(
            invitacionId,
            new InlineValidator<RechazarInvitacionEquipoCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<RechazarInvitacionEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal(invitacionId, command.InvitacionId);
    }

    // ── Elegibles ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Elegibles_Dispatches_Query_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        IReadOnlyList<ParticipanteElegibleResponse> list = Array.Empty<ParticipanteElegibleResponse>();
        var sender = new FakeSender { NextResponse = list };
        var controller = BuildController(sender, actor);

        var result = await controller.Elegibles(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
        var query = Assert.IsType<GetParticipantesElegiblesQuery>(sender.LastRequest);
        Assert.Equal(actor, query.ActorUserId);
    }
}
