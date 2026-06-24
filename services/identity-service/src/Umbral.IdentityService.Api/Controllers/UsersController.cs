using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("api/identity/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserWithInitialRoleCommand command,
        [FromServices] IValidator<CreateUserWithInitialRoleCommand> validator,
        CancellationToken cancellationToken)
    {
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Created($"/api/identity/users/{response.UserId}", response);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetUsersQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        [FromBody] UpdateUserGeneralDataRequest request,
        [FromServices] IValidator<UpdateUserGeneralDataCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserGeneralDataCommand(userId, request.Name, request.Email);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{userId:guid}/deactivation")]
    public async Task<IActionResult> Deactivate(
        Guid userId,
        [FromServices] IValidator<DeactivateUserCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateUserCommand(userId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    private async Task<IActionResult?> ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(instance, cancellationToken);
        if (validation.IsValid)
            return null;

        foreach (var error in validation.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
    }
}
