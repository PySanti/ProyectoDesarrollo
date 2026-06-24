using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class UsersControllerTests
{
    private static UsersController BuildController(FakeSender sender)
    {
        var controller = new UsersController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Dispatches_Command_And_Returns_Created()
    {
        var userId = Guid.NewGuid();
        var response = new CreateUserWithInitialRoleResponse(
            userId, "kc-id", "Alice", "alice@x.com", "Operador", "Activo");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var command = new CreateUserWithInitialRoleCommand("Alice", "alice@x.com", "Operador");
        var result = await controller.Create(command, new InlineValidator<CreateUserWithInitialRoleCommand>(), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/api/identity/users/{userId}", created.Location);
        Assert.IsType<CreateUserWithInitialRoleCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Returns_400_When_Validation_Fails()
    {
        var validator = new InlineValidator<CreateUserWithInitialRoleCommand>();
        validator.RuleFor(c => c.Name).Must(_ => false).WithMessage("bad");
        var controller = BuildController(new FakeSender());

        var command = new CreateUserWithInitialRoleCommand("", "alice@x.com", "Operador");
        var result = await controller.Create(command, validator, CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }

    // ── GetUsers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_Dispatches_Query_And_Returns_Ok()
    {
        IReadOnlyList<UserSummaryResponse> list = Array.Empty<UserSummaryResponse>();
        var sender = new FakeSender { NextResponse = list };
        var controller = BuildController(sender);

        var result = await controller.GetUsers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
        Assert.IsType<GetUsersQuery>(sender.LastRequest);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Dispatches_Query_And_Returns_Ok()
    {
        var userId = Guid.NewGuid();
        var response = new UserDetailResponse(userId, "kc-id", "Alice", "alice@x.com", "Operador", "Activo");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.GetById(userId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var query = Assert.IsType<GetUserByIdQuery>(sender.LastRequest);
        Assert.Equal(userId, query.UserId);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Dispatches_Command_And_Returns_Ok()
    {
        var userId = Guid.NewGuid();
        var response = new UpdateUserGeneralDataResponse(userId, "Bob", "bob@x.com", "Operador", "Activo");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.Update(
            userId,
            new UpdateUserGeneralDataRequest("Bob", "bob@x.com"),
            new InlineValidator<UpdateUserGeneralDataCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<UpdateUserGeneralDataCommand>(sender.LastRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("Bob", command.Name);
        Assert.Equal("bob@x.com", command.Email);
    }

    // ── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_Dispatches_Command_And_Returns_Ok()
    {
        var userId = Guid.NewGuid();
        var response = new DeactivateUserResponse(userId, "Inactivo");
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender);

        var result = await controller.Deactivate(
            userId,
            new InlineValidator<DeactivateUserCommand>(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        var command = Assert.IsType<DeactivateUserCommand>(sender.LastRequest);
        Assert.Equal(userId, command.UserId);
    }
}
