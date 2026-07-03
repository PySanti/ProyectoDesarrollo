using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class RealtimeWiringTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    public RealtimeWiringTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Fact]
    public void ISesionEventsPublisher_se_resuelve_como_composite()
    {
        using var scope = _factory.Services.CreateScope();
        var pub = scope.ServiceProvider.GetRequiredService<ISesionEventsPublisher>();
        Assert.IsType<CompositeSesionEventsPublisher>(pub);
    }
}
