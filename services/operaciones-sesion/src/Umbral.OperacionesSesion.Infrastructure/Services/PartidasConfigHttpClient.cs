using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// Synchronous config handoff (SP-3a Option A): GET /partidas/{id} on Partidas, mapped to a snapshot DTO.
// 404 → null (partida does not exist); network/timeout/non-success → PartidasConfigInaccesible (Partidas down ≠ missing).
public sealed class PartidasConfigHttpClient : IConfiguracionPartidaClient
{
    private readonly HttpClient _http;

    public PartidasConfigHttpClient(HttpClient http) => _http = http;

    public async Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/partidas/{partidaId}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation("Authorization", bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PartidasConfigInaccesibleException(partidaId, ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new PartidasConfigInaccesibleException(partidaId);

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<PartidasConfigResponse>(cancellationToken: cancellationToken)
                ?? throw new PartidasConfigInaccesibleException(partidaId);

            return new ConfiguracionPartidaDto(
                payload.NombrePartida,
                payload.Modalidad,
                payload.ModoInicioPartida,
                payload.TiempoInicio,
                payload.MinimosParticipacion,
                payload.MaximosParticipacion,
                payload.Juegos.Select(j => new JuegoResumenDto(
                    j.JuegoId, j.Orden, j.TipoJuego,
                    j.Trivia is null
                        ? null
                        : new TriviaConfigDto(j.Trivia.Preguntas
                            .Select(p => new PreguntaConfigDto(
                                p.PreguntaId, p.Texto, p.PuntajeAsignado, p.TiempoLimiteSegundos,
                                p.Opciones.Select(o => new OpcionConfigDto(o.OpcionId, o.Texto, o.EsCorrecta)).ToList()))
                            .ToList()),
                    j.Bdt is null
                        ? null
                        : new BdtConfigDto(j.Bdt.AreaBusqueda, j.Bdt.Etapas
                            .Select(e => new EtapaConfigDto(
                                e.EtapaBDTId, e.Orden, e.CodigoQREsperado, e.PuntajeAsignado, e.TiempoLimiteSegundos))
                            .ToList()))).ToList());
        }
        catch (JsonException ex)
        {
            throw new PartidasConfigInaccesibleException(partidaId, ex);
        }
    }

    // Local deserialization shape for Partidas' PartidaDetailDto (camelCase JSON; case-insensitive binding).
    private sealed record PartidasConfigResponse(
        string NombrePartida,
        string Modalidad,
        string ModoInicioPartida,
        DateTime? TiempoInicio,
        int MinimosParticipacion,
        int MaximosParticipacion,
        List<PartidasJuegoResponse> Juegos);

    private sealed record PartidasJuegoResponse(Guid JuegoId, int Orden, string TipoJuego, PartidasTriviaResponse? Trivia, PartidasBdtResponse? Bdt = null);
    private sealed record PartidasTriviaResponse(List<PartidasPreguntaResponse> Preguntas);
    private sealed record PartidasPreguntaResponse(Guid PreguntaId, string Texto, int PuntajeAsignado, int TiempoLimiteSegundos, List<PartidasOpcionResponse> Opciones);
    private sealed record PartidasOpcionResponse(Guid OpcionId, string Texto, bool EsCorrecta);
    private sealed record PartidasBdtResponse(string AreaBusqueda, List<PartidasEtapaResponse> Etapas);
    private sealed record PartidasEtapaResponse(Guid EtapaBDTId, int Orden, string CodigoQREsperado, int PuntajeAsignado, int TiempoLimiteSegundos);
}
