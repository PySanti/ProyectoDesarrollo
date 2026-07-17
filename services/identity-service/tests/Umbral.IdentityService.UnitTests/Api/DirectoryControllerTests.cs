using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.Validators;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class DirectoryControllerTests
{
    private static DirectoryController NuevoController(FakeSender sender)
    {
        var controller = new DirectoryController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task Despacha_la_query_y_devuelve_200_con_el_payload()
    {
        var sub = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var payload = new NombresResponse(
            new List<NombreParticipanteResponse> { new(sub, "María González") },
            new List<NombreEquipoResponse> { new(equipoId, "Los Cazadores") });
        var sender = new FakeSender { NextResponse = payload };
        var controller = NuevoController(sender);

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(new[] { sub }, new[] { equipoId }),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(payload, ok.Value);
        var query = Assert.IsType<ResolverNombresQuery>(sender.LastRequest);
        Assert.Equal(new[] { sub }, query.ParticipanteIds);
        Assert.Equal(new[] { equipoId }, query.EquipoIds);
    }

    [Fact]
    public async Task Listas_nulas_se_normalizan_a_vacias()
    {
        var sender = new FakeSender
        {
            NextResponse = new NombresResponse(
                Array.Empty<NombreParticipanteResponse>(), Array.Empty<NombreEquipoResponse>())
        };
        var controller = NuevoController(sender);

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(null, null),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var query = Assert.IsType<ResolverNombresQuery>(sender.LastRequest);
        Assert.Empty(query.ParticipanteIds);
        Assert.Empty(query.EquipoIds);
    }

    [Fact]
    public async Task Lote_sobre_el_tope_devuelve_400_sin_despachar()
    {
        var sender = new FakeSender();
        var controller = NuevoController(sender);
        var demasiados = new Guid[201];
        for (var i = 0; i < demasiados.Length; i++) demasiados[i] = Guid.NewGuid();

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(demasiados, null),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.Null(sender.LastRequest);
    }
}
