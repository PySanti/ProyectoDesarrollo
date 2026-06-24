using Microsoft.AspNetCore.Mvc;

namespace Umbral.Partidas.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new HealthStatus("healthy", "partidas"));
}

public sealed record HealthStatus(string Status, string Service);
