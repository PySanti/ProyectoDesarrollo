using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Partidas.Api.Contracts;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Queries;

namespace Umbral.Partidas.Api.Controllers;

[ApiController]
[Route("partidas")]
public sealed class PartidasController : ControllerBase
{
    private readonly ISender _mediator;

    public PartidasController(ISender mediator) => _mediator = mediator;

    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost]
    public async Task<IActionResult> CrearPartida(
        [FromBody] CrearPartidaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CrearPartidaCommand(
            request.NombrePartida,
            request.Modalidad,
            request.ModoInicioPartida,
            request.TiempoInicio,
            request.MinimosParticipacion,
            request.MaximosParticipacion);

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId = response.PartidaId }, response);
    }

    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost("{partidaId:guid}/juegos/trivia")]
    public async Task<IActionResult> AgregarJuegoTrivia(
        Guid partidaId,
        [FromBody] AgregarJuegoTriviaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AgregarJuegoTriviaCommand(partidaId, request.Orden, request.Preguntas);

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId }, response);
    }

    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost("{partidaId:guid}/juegos/bdt")]
    public async Task<IActionResult> AgregarJuegoBDT(
        Guid partidaId,
        [FromBody] AgregarJuegoBDTRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AgregarJuegoBDTCommand(partidaId, request.Orden, request.AreaBusqueda, request.Etapas);

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId }, response);
    }

    // Task 5: sin AND aquí — el gateway ya restringe /partidas/{**catch-all} a OperadorOAdministrador
    // y el único caller interno (Operaciones→Publicar) reenvía el bearer de quien ya tiene
    // GestionarPartidas (SP-3a §12), así que el servicio no expone otra vía de acceso.
    [Authorize(Policy = "GestionarPartidas")]
    [HttpGet("{partidaId:guid}")]
    public async Task<IActionResult> GetPartida(Guid partidaId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartidaByIdQuery(partidaId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "GestionarPartidas")]
    [HttpGet]
    public async Task<IActionResult> ListPartidas(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPartidasQuery(), cancellationToken);
        return Ok(result);
    }
}
