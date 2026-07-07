using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class ParticipantesControllerTests
{
    [Fact]
    public void Exige_autenticacion_sin_restriccion_de_rol()
    {
        var attribute = typeof(ParticipantesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(attribute.Roles);
    }

    [Fact]
    public async Task ObtenerHistorialPartidas_despacha_la_query()
    {
        var participanteId = Guid.NewGuid();
        var esperado = new HistorialPartidasResponse(participanteId, Array.Empty<PartidaJugadaDto>());
        var sender = new FakeSender(esperado);
        var controller = new ParticipantesController(sender);

        var resultado = await controller.ObtenerHistorialPartidas(participanteId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
        var query = Assert.IsType<ObtenerHistorialPartidasQuery>(sender.LastRequest);
        Assert.Equal(participanteId, query.ParticipanteId);
    }
}
