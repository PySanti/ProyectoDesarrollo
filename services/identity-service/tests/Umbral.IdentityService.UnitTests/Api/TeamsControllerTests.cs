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

public sealed class TeamsControllerTests
{
    private static TeamsController BuildController(FakeSender sender, Guid? sub)
    {
        var controller = new TeamsController(sender);
        var claims = sub is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim("sub", sub.Value.ToString()) });
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) }
        };
        return controller;
    }

    // ── Crear ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Crear_Dispatches_Command_And_Returns_Created()
    {
        var actor = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new CrearEquipoResponse(
            equipoId, "Equipo A", "Activo", actor,
            new[] { new CrearEquipoIntegranteResponse(actor, true) });
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.Crear(
            new CrearEquipoRequest("Equipo A"),
            new InlineValidator<CrearEquipoCommand>(),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/identity/teams/{equipoId}", created.Location);
        var command = Assert.IsType<CrearEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal("Equipo A", command.NombreEquipo);
    }

    [Fact]
    public async Task Crear_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Crear(
            new CrearEquipoRequest("X"),
            new InlineValidator<CrearEquipoCommand>(),
            CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Crear_Returns_400_When_Validation_Fails()
    {
        var validator = new InlineValidator<CrearEquipoCommand>();
        validator.RuleFor(c => c.NombreEquipo).Must(_ => false).WithMessage("bad");
        var controller = BuildController(new FakeSender(), Guid.NewGuid());

        var result = await controller.Crear(
            new CrearEquipoRequest(""),
            validator,
            CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    // ── Salir ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Salir_Dispatches_Command_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new SalirDeEquipoResponse(actor, equipoId, "Salida", "Eliminado");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.Salir(
            new InlineValidator<SalirDeEquipoCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<SalirDeEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
    }

    [Fact]
    public async Task Salir_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Salir(
            new InlineValidator<SalirDeEquipoCommand>(),
            CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── TransferirLiderazgo ──────────────────────────────────────────────────

    [Fact]
    public async Task TransferirLiderazgo_Dispatches_Command_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var nuevoLider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = new TransferirLiderazgoResponse(equipoId, actor, nuevoLider, "Activo");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.TransferirLiderazgo(
            new TransferirLiderazgoRequest(nuevoLider),
            new InlineValidator<TransferirLiderazgoCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<TransferirLiderazgoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal(nuevoLider, command.NuevoLiderUserId);
    }

    [Fact]
    public async Task TransferirLiderazgo_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.TransferirLiderazgo(
            new TransferirLiderazgoRequest(Guid.NewGuid()),
            new InlineValidator<TransferirLiderazgoCommand>(),
            CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ── EliminarMiEquipo ─────────────────────────────────────────────────────

    [Fact]
    public async Task EliminarMiEquipo_Dispatches_And_Returns_NoContent()
    {
        var actor = Guid.NewGuid();
        var sender = new FakeSender { NextResponse = new EliminarEquipoResponse(Guid.NewGuid(), "Eliminado") };
        var controller = BuildController(sender, actor);

        var result = await controller.EliminarMiEquipo(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<EliminarMiEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
    }

    [Fact]
    public async Task EliminarMiEquipo_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        Assert.IsType<UnauthorizedResult>(await controller.EliminarMiEquipo(CancellationToken.None));
    }

    // ── MiHistorial ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Historial_Dispatches_Query_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var response = new HistorialNombresEquipoResponse(
            new[] { new HistorialNombreEquipoItem("Titanes", Guid.NewGuid(), DateTime.UtcNow) });
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.MiHistorial(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        var query = Assert.IsType<GetHistorialNombresEquipoQuery>(sender.LastRequest);
        Assert.Equal(actor, query.ActorUserId);
    }

    [Fact]
    public async Task Historial_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.MiHistorial(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }
}
