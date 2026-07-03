using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.Api.Controllers;

[ApiController]
[Route("operaciones-sesion")]
public sealed class SesionesController : ControllerBase
{
    private readonly ISender _mediator;

    public SesionesController(ISender mediator) => _mediator = mediator;

    [HttpPost("partidas/{partidaId:guid}/publicacion")]
    public async Task<IActionResult> Publicar(Guid partidaId, CancellationToken cancellationToken)
    {
        var bearer = Request.Headers.Authorization.ToString();
        var command = new PublicarPartidaCommand(partidaId, string.IsNullOrWhiteSpace(bearer) ? null : bearer);
        var lobby = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, lobby);
    }

    [HttpPost("partidas/{partidaId:guid}/inscripciones")]
    public async Task<IActionResult> Inscribir(Guid partidaId, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new InscribirParticipanteCommand(partidaId, participanteId), cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, response);
    }

    [HttpDelete("partidas/{partidaId:guid}/inscripciones/mia")]
    public async Task<IActionResult> CancelarInscripcion(Guid partidaId, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        await _mediator.Send(new CancelarInscripcionCommand(partidaId, participanteId), cancellationToken);
        return NoContent();
    }

    [HttpPost("partidas/{partidaId:guid}/inscripciones-equipo")]
    public async Task<IActionResult> PreinscribirEquipo(Guid partidaId, CancellationToken cancellationToken)
    {
        var liderId = ObtenerParticipanteId();
        var bearer = Request.Headers.Authorization.ToString();
        var response = await _mediator.Send(
            new PreinscribirEquipoCommand(partidaId, liderId, string.IsNullOrWhiteSpace(bearer) ? null : bearer),
            cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, response);
    }

    [HttpDelete("partidas/{partidaId:guid}/inscripciones-equipo/mia")]
    public async Task<IActionResult> CancelarInscripcionEquipo(Guid partidaId, CancellationToken cancellationToken)
    {
        var liderId = ObtenerParticipanteId();
        var bearer = Request.Headers.Authorization.ToString();
        await _mediator.Send(
            new CancelarInscripcionEquipoCommand(partidaId, liderId, string.IsNullOrWhiteSpace(bearer) ? null : bearer),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/aceptacion")]
    public async Task<IActionResult> AceptarConvocatoria(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerParticipanteId();
        var response = await _mediator.Send(
            new ResponderConvocatoriaCommand(convocatoriaId, usuarioId, true), cancellationToken);
        return Ok(response);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/rechazo")]
    public async Task<IActionResult> RechazarConvocatoria(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerParticipanteId();
        var response = await _mediator.Send(
            new ResponderConvocatoriaCommand(convocatoriaId, usuarioId, false), cancellationToken);
        return Ok(response);
    }

    [HttpGet("partidas/{partidaId:guid}/lobby")]
    public async Task<IActionResult> ObtenerLobby(Guid partidaId, CancellationToken cancellationToken)
    {
        var lobby = await _mediator.Send(new ObtenerLobbyQuery(partidaId), cancellationToken);
        return Ok(lobby);
    }

    [HttpPost("partidas/{partidaId:guid}/inicio")]
    public async Task<IActionResult> Iniciar(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new IniciarPartidaCommand(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/inicio-automatico")]
    public async Task<IActionResult> IniciarAutomatico(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new IntentarInicioAutomaticoCommand(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/juego-actual/finalizacion")]
    public async Task<IActionResult> FinalizarJuegoActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new FinalizarJuegoActualCommand(partidaId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/estado")]
    public async Task<IActionResult> ObtenerEstado(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerEstadoSesionQuery(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/pregunta-actual/respuesta")]
    public async Task<IActionResult> Responder(Guid partidaId, [FromBody] ResponderPreguntaRequest request, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new ResponderPreguntaCommand(partidaId, participanteId, request.OpcionId), cancellationToken);
        return Ok(response);
    }

    [HttpPost("partidas/{partidaId:guid}/pregunta-actual/avance")]
    public async Task<IActionResult> Avanzar(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AvanzarPreguntaCommand(partidaId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/pregunta-actual")]
    public async Task<IActionResult> ObtenerPreguntaActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerPreguntaActualQuery(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/etapa-actual/tesoro")]
    public async Task<IActionResult> ValidarTesoro(Guid partidaId, [FromBody] ValidarTesoroRequest request, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new ValidarTesoroCommand(partidaId, participanteId, request.ImagenBase64), cancellationToken);
        return Ok(response);
    }

    [HttpPost("partidas/{partidaId:guid}/etapa-actual/avance")]
    public async Task<IActionResult> AvanzarEtapa(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AvanzarEtapaCommand(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/pistas")]
    public async Task<IActionResult> EnviarPista(Guid partidaId, [FromBody] EnviarPistaRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new EnviarPistaCommand(partidaId, request.ParticipanteDestinoId, request.Texto, request.EquipoDestinoId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/etapa-actual")]
    public async Task<IActionResult> ObtenerEtapaActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerEtapaActualQuery(partidaId), cancellationToken));

    [HttpGet("mi-sesion")]
    public async Task<IActionResult> ObtenerMiSesion(CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var dto = await _mediator.Send(new ObtenerMiSesionQuery(participanteId), cancellationToken);
        return dto is null ? NoContent() : Ok(dto);
    }

    [HttpGet("mis-convocatorias")]
    public async Task<IActionResult> ObtenerMisConvocatorias(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerParticipanteId();
        var dto = await _mediator.Send(new ObtenerMisConvocatoriasPendientesQuery(usuarioId), cancellationToken);
        return Ok(dto);
    }

    private Guid ObtenerParticipanteId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new ParticipanteNoIdentificadoException();
    }
}
