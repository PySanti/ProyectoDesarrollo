using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class AdminTeamsControllerTests
{
    private static AdminTeamsController BuildController(FakeSender sender)
    {
        var controller = new AdminTeamsController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static EquipoAdminResponse SampleResponse(Guid equipoId, Guid liderUserId) =>
        new(equipoId, "Equipo A", "Activo", liderUserId,
            new[] { new EquipoAdminIntegrante(liderUserId, true) });

    // ── GetEquipos (list) ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEquipos_Dispatches_Query_And_Returns_Ok()
    {
        IReadOnlyList<EquipoAdminResponse> list = new[] { SampleResponse(Guid.NewGuid(), Guid.NewGuid()) };
        var sender = new FakeSender { NextResponse = list };
        var controller = BuildController(sender);

        var result = await controller.GetEquipos(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
        Assert.IsType<GetEquiposAdminQuery>(sender.LastRequest);
    }

    // ── GetEquipoById (detail) ───────────────────────────────────────────────

    [Fact]
    public async Task GetEquipoById_Dispatches_Query_And_Returns_Ok()
    {
        var equipoId = Guid.NewGuid();
        var liderUserId = Guid.NewGuid();
        var response = SampleResponse(equipoId, liderUserId);
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.GetEquipoById(equipoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var query = Assert.IsType<GetEquipoAdminByIdQuery>(sender.LastRequest);
        Assert.Equal(equipoId, query.EquipoId);
    }

    [Fact]
    public async Task GetEquipoById_Returns_404_When_Query_Yields_Null()
    {
        var equipoId = Guid.NewGuid();
        var sender = new FakeSender { NextResponse = null };
        var controller = BuildController(sender);

        var result = await controller.GetEquipoById(equipoId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        var query = Assert.IsType<GetEquipoAdminByIdQuery>(sender.LastRequest);
        Assert.Equal(equipoId, query.EquipoId);
    }

    // ── Crear ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Crear_Dispatches_Command_And_Returns_Created()
    {
        var liderUserId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var response = SampleResponse(equipoId, liderUserId);
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.Crear(
            new CrearEquipoAdminRequest("Equipo A", liderUserId),
            new InlineValidator<CrearEquipoAdminCommand>(),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/identity/admin/teams/{equipoId}", created.Location);
        Assert.Equal(response, created.Value);
        var command = Assert.IsType<CrearEquipoAdminCommand>(sender.LastRequest);
        Assert.Equal("Equipo A", command.NombreEquipo);
        Assert.Equal(liderUserId, command.LiderUserId);
    }

    [Fact]
    public async Task Crear_Returns_400_When_Validation_Fails_And_Does_Not_Dispatch()
    {
        var validator = new InlineValidator<CrearEquipoAdminCommand>();
        validator.RuleFor(c => c.NombreEquipo).Must(_ => false).WithMessage("bad");
        var sender = new FakeSender();
        var controller = BuildController(sender);

        var result = await controller.Crear(
            new CrearEquipoAdminRequest("", Guid.NewGuid()),
            validator,
            CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
        Assert.Null(sender.LastRequest);
    }

    // ── Renombrar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Renombrar_Dispatches_Command_And_Returns_Ok()
    {
        var equipoId = Guid.NewGuid();
        var liderUserId = Guid.NewGuid();
        var response = SampleResponse(equipoId, liderUserId) with { NombreEquipo = "Equipo B" };
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.Renombrar(
            equipoId,
            new RenombrarEquipoRequest("Equipo B"),
            new InlineValidator<RenombrarEquipoAdminCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<RenombrarEquipoAdminCommand>(sender.LastRequest);
        Assert.Equal(equipoId, command.EquipoId);
        Assert.Equal("Equipo B", command.NombreEquipo);
    }

    // ── ReasignarLiderazgo ───────────────────────────────────────────────────

    [Fact]
    public async Task ReasignarLiderazgo_Dispatches_Command_And_Returns_Ok()
    {
        var equipoId = Guid.NewGuid();
        var nuevoLiderUserId = Guid.NewGuid();
        var response = SampleResponse(equipoId, nuevoLiderUserId);
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.ReasignarLiderazgo(
            equipoId,
            new ReasignarLiderazgoAdminRequest(nuevoLiderUserId),
            new InlineValidator<ReasignarLiderazgoAdminCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<ReasignarLiderazgoAdminCommand>(sender.LastRequest);
        Assert.Equal(equipoId, command.EquipoId);
        Assert.Equal(nuevoLiderUserId, command.NuevoLiderUserId);
    }

    // ── CambiarEstado ────────────────────────────────────────────────────────

    [Fact]
    public async Task CambiarEstado_Dispatches_Command_And_Returns_Ok()
    {
        var equipoId = Guid.NewGuid();
        var liderUserId = Guid.NewGuid();
        var response = SampleResponse(equipoId, liderUserId) with { Estado = "Desactivado" };
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.CambiarEstado(
            equipoId,
            new CambiarEstadoEquipoRequest("Desactivado"),
            new InlineValidator<CambiarEstadoEquipoAdminCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<CambiarEstadoEquipoAdminCommand>(sender.LastRequest);
        Assert.Equal(equipoId, command.EquipoId);
        Assert.Equal("Desactivado", command.Estado);
    }

    [Fact]
    public async Task CambiarEstado_Returns_400_When_Validation_Fails_And_Does_Not_Dispatch()
    {
        var validator = new InlineValidator<CambiarEstadoEquipoAdminCommand>();
        validator.RuleFor(c => c.Estado).Must(_ => false).WithMessage("bad");
        var sender = new FakeSender();
        var controller = BuildController(sender);

        var result = await controller.CambiarEstado(
            Guid.NewGuid(),
            new CambiarEstadoEquipoRequest("Suspendido"),
            validator,
            CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
        Assert.Null(sender.LastRequest);
    }

    // ── Eliminar ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Eliminar_Dispatches_Command_And_Returns_NoContent()
    {
        var equipoId = Guid.NewGuid();
        var sender = new FakeSender();
        var controller = BuildController(sender);

        var result = await controller.Eliminar(equipoId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<EliminarEquipoAdminCommand>(sender.LastRequest);
        Assert.Equal(equipoId, command.EquipoId);
    }
}
