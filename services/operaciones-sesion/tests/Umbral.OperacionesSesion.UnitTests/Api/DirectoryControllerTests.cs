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

// El tope del lote NO se prueba aqui: lo aplica el ValidationBehavior de MediatR, que no
// existe con FakeSender. Su 400 se cubre en los contract tests, con el pipeline real.
public class DirectoryControllerTests
{
    [Fact]
    public async Task Despacha_la_query_y_devuelve_200_con_el_payload()
    {
        var partidaId = Guid.NewGuid();
        var payload = new ResolverNombresPartidaResponse(
            new List<NombrePartidaDto> { new(partidaId, "Copa UMBRAL") });
        var sender = new FakeSender(payload);
        var controller = new DirectoryController(sender);

        var result = await controller.ResolverNombresPartida(
            new ResolverNombresPartidaRequest(new[] { partidaId }), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(payload, ok.Value);
        var query = Assert.IsType<ResolverNombresPartidaQuery>(sender.LastRequest);
        Assert.Equal(new[] { partidaId }, query.PartidaIds);
    }

    [Fact]
    public async Task Lista_nula_se_normaliza_a_vacia()
    {
        var sender = new FakeSender(
            new ResolverNombresPartidaResponse(Array.Empty<NombrePartidaDto>()));
        var controller = new DirectoryController(sender);

        var result = await controller.ResolverNombresPartida(
            new ResolverNombresPartidaRequest(null), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var query = Assert.IsType<ResolverNombresPartidaQuery>(sender.LastRequest);
        Assert.Empty(query.PartidaIds);
    }
}
