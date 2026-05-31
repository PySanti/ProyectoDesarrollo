using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.TriviaGame.Api.Constants;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.Operador)]
[Route("api/trivia-forms")]
public sealed class TriviaFormsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaFormsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTriviaFormCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTriviaFormCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.FormId)
            return BadRequest(new { detail = "El id de la ruta no coincide con el del cuerpo de la solicitud." });

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTriviaFormByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
