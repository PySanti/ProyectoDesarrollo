using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.Partidas.Api.Contracts;
using Umbral.Partidas.Api.Controllers;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Api;

// The controller is a pure MediatR dispatcher: validation lives in ValidationBehavior
// (doctrine audit M-2), so invalid-input -> 400 is covered by ValidationBehaviorTests
// and the contract suite, not here.
public class PartidasControllerTests
{
    [Fact]
    public async Task CrearPartida_valid_returns_201_created()
    {
        var response = new CrearPartidaResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new CrearPartidaRequest("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var result = await controller.CrearPartida(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task AgregarJuegoTrivia_valid_returns_201()
    {
        var response = new AgregarJuegoResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new AgregarJuegoTriviaRequest(1, new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 10, 30)
        });

        var result = await controller.AgregarJuegoTrivia(Guid.NewGuid(), request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task AgregarJuegoBDT_valid_returns_201()
    {
        var response = new AgregarJuegoResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new AgregarJuegoBDTRequest(1, "Plaza", new List<EtapaRequest> { new(1, "QR", 50, 120) });

        var result = await controller.AgregarJuegoBDT(Guid.NewGuid(), request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task GetPartida_returns_200()
    {
        var detail = new PartidaDetailDto(Guid.NewGuid(), "Copa", "Individual", "Manual", null, 1, 10, null, new List<JuegoDto>());
        var controller = new PartidasController(new FakeSender(detail));

        var result = await controller.GetPartida(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public async Task ListPartidas_returns_200()
    {
        IReadOnlyList<PartidaSummaryDto> list = new List<PartidaSummaryDto>();
        var controller = new PartidasController(new FakeSender(list));

        var result = await controller.ListPartidas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(list, ok.Value);
    }
}
