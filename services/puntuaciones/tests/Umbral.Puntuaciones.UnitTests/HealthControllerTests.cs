using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;

namespace Umbral.Puntuaciones.UnitTests;

public class HealthControllerTests
{
    [Fact]
    public void Get_returns_ok_with_healthy_status_and_service()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var payload = Assert.IsType<HealthStatus>(ok.Value);
        Assert.Equal("healthy", payload.Status);
        Assert.Equal("puntuaciones", payload.Service);
    }
}
