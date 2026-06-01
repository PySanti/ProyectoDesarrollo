using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.TriviaGame.Api.Constants;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.Operador)]
[Route("api/trivia-games")]
public sealed class TriviaGamesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaGamesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTriviaGameCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new StartTriviaGameCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/participants")]
    public async Task<IActionResult> GetParticipants(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTriviaGameParticipantsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTriviaGameByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
