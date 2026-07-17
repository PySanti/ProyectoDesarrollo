using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarParticipacionHandlersTests
{
    private static readonly DateTime T0 = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InscripcionAceptada_individual_proyecta_al_participante()
    {
        var repo = new FakeProyeccionesRepository();
        var handler = new ProyectarInscripcionAceptadaCommandHandler(repo, new FakePuntuacionesUnitOfWork());
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        await handler.Handle(new ProyectarInscripcionAceptadaCommand(
            Guid.NewGuid(), T0, partidaId, "Individual", participanteId, null), CancellationToken.None);

        var p = Assert.Single(repo.Participaciones);
        Assert.Equal(participanteId, p.CompetidorId);
        Assert.Equal(TipoCompetidor.Participante, p.TipoCompetidor);
    }

    [Fact]
    public async Task InscripcionAceptada_equipo_proyecta_al_equipo()
    {
        var repo = new FakeProyeccionesRepository();
        var handler = new ProyectarInscripcionAceptadaCommandHandler(repo, new FakePuntuacionesUnitOfWork());
        var equipoId = Guid.NewGuid();

        await handler.Handle(new ProyectarInscripcionAceptadaCommand(
            Guid.NewGuid(), T0, Guid.NewGuid(), "Equipo", null, equipoId), CancellationToken.None);

        var p = Assert.Single(repo.Participaciones);
        // El competidor en Equipo es el equipo, no sus miembros.
        Assert.Equal(equipoId, p.CompetidorId);
        Assert.Equal(TipoCompetidor.Equipo, p.TipoCompetidor);
    }

    [Fact]
    public async Task InscripcionAceptada_repetida_no_duplica()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var cmd = new ProyectarInscripcionAceptadaCommand(
            Guid.NewGuid(), T0, Guid.NewGuid(), "Individual", Guid.NewGuid(), null);

        await new ProyectarInscripcionAceptadaCommandHandler(repo, uow).Handle(cmd, CancellationToken.None);
        await new ProyectarInscripcionAceptadaCommandHandler(repo, uow).Handle(cmd, CancellationToken.None);

        Assert.Single(repo.Participaciones);
    }

    [Fact]
    public async Task ConvocatoriaCreada_nace_pendiente_y_ConvocatoriaRespondida_la_acepta()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var convocatoriaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await new ProyectarConvocatoriaCreadaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaCreadaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), convocatoriaId, Guid.NewGuid(), usuarioId),
            CancellationToken.None);
        Assert.False(Assert.Single(repo.Convocatorias).Aceptada);

        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, convocatoriaId, usuarioId, "Aceptada"),
            CancellationToken.None);

        Assert.True(Assert.Single(repo.Convocatorias).Aceptada);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_rechazada_deja_aceptada_en_false()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var convocatoriaId = Guid.NewGuid();

        await new ProyectarConvocatoriaCreadaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaCreadaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), convocatoriaId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, convocatoriaId, Guid.NewGuid(), "Rechazada"),
            CancellationToken.None);

        Assert.False(Assert.Single(repo.Convocatorias).Aceptada);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_sin_creada_no_lanza()
    {
        var repo = new FakeProyeccionesRepository();

        // Best-effort (ADR-0012): si se pierde ConvocatoriaCreada no hay fila que actualizar
        // (falta el EquipoId para crearla). Se ackea y el miembro cae al comportamiento de hoy.
        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, new FakePuntuacionesUnitOfWork()).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), Guid.NewGuid(), "Aceptada"),
            CancellationToken.None);

        Assert.Empty(repo.Convocatorias);
    }
}
