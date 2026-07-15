using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerPartidasPublicadasTests
{
    [Fact]
    public async Task Partidas_publicadas_devuelve_200_con_lista()
    {
        IReadOnlyList<PartidaPublicadaDto> dtos = new[]
        {
            new PartidaPublicadaDto(Guid.NewGuid(), "Copa", "Individual", "Manual", null, 1, 10, 3)
        };
        var sender = new FakeSender(dtos);
        var controller = new SesionesController(sender);

        var result = await controller.ListarPartidasPublicadas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dtos, ok.Value);
        Assert.IsType<ListarPartidasPublicadasQuery>(sender.LastRequest);
    }
}
