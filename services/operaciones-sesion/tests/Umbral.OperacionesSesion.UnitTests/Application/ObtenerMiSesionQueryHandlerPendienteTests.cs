using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerMiSesionQueryHandlerPendienteTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida IndividualEnLobby(Guid partidaId)
    {
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Inscripcion_pendiente_expone_estado_Pendiente_en_mi_sesion()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var sesion = IndividualEnLobby(partidaId);
        var insc = sesion.Inscribir(participante, false, 0, T0); // queda Pendiente
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(participante), default);

        Assert.NotNull(dto);
        Assert.Equal("Pendiente", dto!.Inscripcion.Estado);
        Assert.Equal(insc.Id.Valor, dto.Inscripcion.InscripcionId);
        Assert.Null(dto.Convocatoria);
    }
}
