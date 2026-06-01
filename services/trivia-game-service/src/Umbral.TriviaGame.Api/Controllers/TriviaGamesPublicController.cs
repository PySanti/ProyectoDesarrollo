using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trivia-games")]
public sealed class TriviaGamesPublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaGamesPublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? modalidad,
        CancellationToken cancellationToken)
    {
        var query = new GetPublishedTriviaGamesQuery(modalidad);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var command = new JoinTriviaGameCommand(id, usuarioId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/lobby")]
    public async Task<IActionResult> GetLobby(
        Guid id,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var query = new GetTriviaGameLobbyQuery(id, usuarioId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/questions/{preguntaId:guid}/answer")]
    public async Task<IActionResult> Answer(
        Guid id,
        Guid preguntaId,
        [FromBody] AnswerTriviaQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId))
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var command = new AnswerTriviaQuestionCommand(
            id,
            preguntaId,
            usuarioId,
            request.OpcionIndex);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record AnswerTriviaQuestionRequest(int OpcionIndex);
