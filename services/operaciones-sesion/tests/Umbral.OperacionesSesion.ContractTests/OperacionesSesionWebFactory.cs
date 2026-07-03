using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.ContractTests;

public sealed class StubConfigClient : IConfiguracionPartidaClient
{
    // Per-partida overrides; null value = explicit 404. Unmapped ids fall back to a valid Individual config.
    public Dictionary<Guid, ConfiguracionPartidaDto?> Respuestas { get; } = new();

    public ConfiguracionPartidaDto Default { get; set; } =
        new("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });

    public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
        => Task.FromResult(Respuestas.TryGetValue(partidaId, out var r) ? r : Default);
}

/// <summary>
/// Stub de IQrDecoder para contract tests: interpreta los bytes de la "imagen" como el texto
/// del QR (UTF-8). Retorna null si el array es nulo o vacío (simula QR ilegible).
/// </summary>
public sealed class ContractTestQrDecoder : Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder
{
    public string? Decodificar(byte[] imagen) =>
        imagen is null || imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
}

public sealed class OperacionesSesionWebFactory : WebApplicationFactory<Program>
{
    public StubConfigClient Stub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfiguracionPartidaClient>();
            services.AddSingleton<IConfiguracionPartidaClient>(Stub);

            services.RemoveAll<Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder>();
            services.AddSingleton<Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder>(new ContractTestQrDecoder());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateClientAs(Guid participanteId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", participanteId.ToString());
        return client;
    }
}
