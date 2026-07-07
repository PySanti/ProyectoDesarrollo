using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarEventoHistorialCommandHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeHistorialRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();

    private ProyectarEventoHistorialCommandHandler Handler() => new(_repo, _uow);

    private static ProyectarEventoHistorialCommand Comando(
        string tipo = "EtapaBDTGanada",
        Guid? eventId = null, Guid? partidaId = null, Guid? juegoId = null,
        Guid? participanteId = null, Guid? equipoId = null,
        DateTime? occurredAt = null, string detalle = """{"puntaje":10}""")
        => new(eventId ?? Guid.NewGuid(), tipo, occurredAt ?? Ahora, partidaId ?? Guid.NewGuid(),
            juegoId, participanteId, equipoId, detalle);

    [Fact]
    public async Task Inserta_la_fila_con_todos_los_campos()
    {
        var comando = Comando(
            juegoId: Guid.NewGuid(), participanteId: Guid.NewGuid(), equipoId: Guid.NewGuid());

        await Handler().Handle(comando, CancellationToken.None);

        var evento = Assert.Single(_repo.Eventos);
        Assert.Equal(comando.EventId, evento.EventId);
        Assert.Equal(comando.PartidaId, evento.PartidaId);
        Assert.Equal(comando.JuegoId, evento.JuegoId);
        Assert.Equal("EtapaBDTGanada", evento.TipoEvento);
        Assert.Equal(Ahora, evento.OccurredAt);
        Assert.Equal(comando.ParticipanteId, evento.ParticipanteId);
        Assert.Equal(comando.EquipoId, evento.EquipoId);
        Assert.Equal("""{"puntaje":10}""", evento.DetalleJson);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task EventId_duplicado_no_inserta_segunda_fila()
    {
        var eventId = Guid.NewGuid();
        await Handler().Handle(Comando(eventId: eventId), CancellationToken.None);

        await Handler().Handle(Comando(eventId: eventId), CancellationToken.None);

        Assert.Single(_repo.Eventos);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Ubicacion_a_menos_de_60s_del_mismo_participante_y_partida_se_descarta()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(30)), CancellationToken.None);

        Assert.Single(_repo.Eventos);
    }

    [Fact]
    public async Task Ubicacion_a_60s_o_mas_se_guarda()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(60)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }

    [Fact]
    public async Task Ubicacion_de_otro_participante_o_partida_se_guarda()
    {
        var partidaId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: Guid.NewGuid(), occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: Guid.NewGuid(), occurredAt: Ahora.AddSeconds(10)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }

    [Fact]
    public async Task Otros_tipos_no_se_muestrean()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("RespuestaTriviaValidada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("RespuestaTriviaValidada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(5)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }
}
