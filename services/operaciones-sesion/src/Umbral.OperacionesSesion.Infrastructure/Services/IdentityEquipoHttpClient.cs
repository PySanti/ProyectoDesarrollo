using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// GET /api/teams/mine en Identity → snapshot de membresía del líder autenticado.
// 404 → null (sin equipo activo); red/timeout/non-success → IdentityInaccesible (Identity caído ≠ sin equipo).
public sealed class IdentityEquipoHttpClient : IEquipoDirectoryClient
{
    private readonly HttpClient _http;

    public IdentityEquipoHttpClient(HttpClient http) => _http = http;

    public async Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/teams/mine");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation("Authorization", bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new IdentityInaccesibleException(ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new IdentityInaccesibleException();

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<EquipoMineResponse>(cancellationToken: cancellationToken)
                ?? throw new IdentityInaccesibleException();

            return new EquipoSnapshotDto(
                payload.EquipoId,
                payload.NombreEquipo,
                payload.Participantes.Select(p => new MiembroEquipoDto(p.UsuarioId, p.EsLider)).ToList());
        }
        catch (JsonException ex)
        {
            throw new IdentityInaccesibleException(ex);
        }
    }

    // Deserialización local del contrato identity-api GET /api/teams/mine (camelCase; binding case-insensitive).
    private sealed record EquipoMineResponse(Guid EquipoId, string NombreEquipo, string Estado, List<MiembroResponse> Participantes);
    private sealed record MiembroResponse(Guid UsuarioId, bool EsLider);
}
