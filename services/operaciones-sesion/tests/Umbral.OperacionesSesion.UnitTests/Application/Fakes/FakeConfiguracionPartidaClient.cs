using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeConfiguracionPartidaClient : IConfiguracionPartidaClient
{
    private readonly ConfiguracionPartidaDto? _respuesta;
    public string? LastBearerToken { get; private set; }

    public FakeConfiguracionPartidaClient(ConfiguracionPartidaDto? respuesta) => _respuesta = respuesta;

    public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
    {
        LastBearerToken = bearerToken;
        return Task.FromResult(_respuesta);
    }
}
