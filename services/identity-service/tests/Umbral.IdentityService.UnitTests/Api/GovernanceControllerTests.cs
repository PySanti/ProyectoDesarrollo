using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class GovernanceControllerTests
{
    private static GovernanceController BuildController(FakeSender sender)
    {
        var controller = new GovernanceController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // ── GetRoles ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoles_Dispatches_Query_And_Returns_Ok()
    {
        var response = new PermisosRolesResponse(new[]
        {
            new RolPermisosDto("Administrador", Array.Empty<string>(), true),
            new RolPermisosDto("Operador", new[] { "GestionarPartidas" }, false),
            new RolPermisosDto("Participante", new[] { "GestionarEquipos", "ParticiparEnPartidas" }, false)
        });
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.GetRoles(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        Assert.IsType<GetPermisosRolesQuery>(sender.LastRequest);
    }

    // ── ActualizarPermisos ───────────────────────────────────────────────────

    [Fact]
    public async Task ActualizarPermisos_Valido_Dispatches_Command_And_Returns_Ok()
    {
        var response = new RolPermisosDto("Operador", new[] { "GestionarEquipos" }, false);
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.ActualizarPermisos(
            "Operador",
            new ActualizarPermisosRolRequest(new[] { "GestionarEquipos" }),
            new InlineValidator<ActualizarPermisosRolCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<ActualizarPermisosRolCommand>(sender.LastRequest);
        Assert.Equal("Operador", command.Rol);
        Assert.Equal(new[] { "GestionarEquipos" }, command.Permisos);
    }

    [Fact]
    public async Task ActualizarPermisos_Invalido_Returns_400_Sin_Llamar_Al_Sender()
    {
        var validator = new InlineValidator<ActualizarPermisosRolCommand>();
        validator.RuleFor(c => c.Rol).Must(_ => false).WithMessage("bad");
        var sender = new FakeSender();
        var controller = BuildController(sender);

        var result = await controller.ActualizarPermisos(
            "SuperUser",
            new ActualizarPermisosRolRequest(new[] { "GestionarEquipos" }),
            validator,
            CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
        Assert.Null(sender.LastRequest);
    }
}
