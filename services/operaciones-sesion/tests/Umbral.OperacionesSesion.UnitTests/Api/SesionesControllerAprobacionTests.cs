using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerAprobacionTests
{
    private static LobbyDto LobbyVacio(Guid partidaId) => new(
        partidaId, Guid.NewGuid(), "Lobby", "Individual", 1, 5, 0, 0,
        Array.Empty<Guid>(), Array.Empty<EquipoLobbyDto>(),
        Array.Empty<SolicitudIndividualDto>(), Array.Empty<SolicitudEquipoDto>());

    [Fact]
    public async Task Aceptar_despacha_comando_y_devuelve_lobby()
    {
        var partidaId = Guid.NewGuid();
        var inscripcionId = Guid.NewGuid();
        var sender = new FakeSender(LobbyVacio(partidaId));
        var controller = new SesionesController(sender);

        var result = await controller.AceptarInscripcion(partidaId, inscripcionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LobbyDto>(ok.Value);
        var cmd = Assert.IsType<AceptarInscripcionCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(inscripcionId, cmd.InscripcionId);
    }

    [Fact]
    public async Task Rechazar_despacha_comando_y_devuelve_lobby()
    {
        var partidaId = Guid.NewGuid();
        var inscripcionId = Guid.NewGuid();
        var sender = new FakeSender(LobbyVacio(partidaId));
        var controller = new SesionesController(sender);

        var result = await controller.RechazarInscripcion(partidaId, inscripcionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LobbyDto>(ok.Value);
        var cmd = Assert.IsType<RechazarInscripcionCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(inscripcionId, cmd.InscripcionId);
    }
}
