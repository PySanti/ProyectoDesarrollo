using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class TeamsAdminControllerTests
{
    [Fact]
    public async Task Listar_Dispatches_Query_And_Returns_200_With_Payload()
    {
        var payload = new List<EquipoAdminItemResponse>
        {
            new(Guid.NewGuid(), "Equipo A", "Activo",
                new List<MiembroEquipoAdminResponse> { new(Guid.NewGuid(), "Ana", true) })
        };
        var sender = new FakeSender { NextResponse = payload };
        var controller = new TeamsAdminController(sender);

        var result = await controller.Listar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(payload, ok.Value);
        Assert.IsType<ListarEquiposQuery>(sender.LastRequest);
    }
}
