using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarScoringHandlersTests
{
    private readonly FakeProyeccionesRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();
    private static readonly DateTime Ahora = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);

    private static ProyectarPuntajeTriviaCommand Trivia(Guid juegoId, Guid participanteId, int puntaje, long tiempoMs, Guid? equipoId = null)
        => new(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), juegoId, Guid.NewGuid(), participanteId, puntaje, tiempoMs, equipoId);

    [Fact]
    public async Task PuntajeTrivia_individual_acredita_al_participante()
    {
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(Trivia(juegoId, participanteId, 10, 1500), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, participanteId, 5, 500), CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(participanteId, marcador.CompetidorId);
        Assert.Equal(TipoCompetidor.Participante, marcador.TipoCompetidor);
        Assert.Equal(15, marcador.PuntosAcumulados);
        Assert.Equal(2000, marcador.TiempoAcumuladoMs);
        Assert.Equal(2, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task PuntajeTrivia_equipo_acredita_al_equipo_no_al_autor()
    {
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        // Dos autores distintos del mismo equipo.
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 10, 1000, equipoId), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 20, 2000, equipoId), CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(equipoId, marcador.CompetidorId);
        Assert.Equal(TipoCompetidor.Equipo, marcador.TipoCompetidor);
        Assert.Equal(30, marcador.PuntosAcumulados);
    }

    [Fact]
    public async Task PuntajeTrivia_duplicado_no_acredita_dos_veces()
    {
        var cmd = Trivia(Guid.NewGuid(), Guid.NewGuid(), 10, 1000);
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(cmd, CancellationToken.None);
        await handler.Handle(cmd, CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(10, marcador.PuntosAcumulados);
        Assert.Equal(1, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task EtapaBdtGanada_acredita_puntaje_de_etapa()
    {
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var cmd = new ProyectarEtapaBdtGanadaCommand(
            Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), juegoId, Guid.NewGuid(), participanteId, 25, 4000, null);

        await new ProyectarEtapaBdtGanadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(25, marcador.PuntosAcumulados);
        Assert.Equal(4000, marcador.TiempoAcumuladoMs);
        Assert.Equal(1, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task Competidores_distintos_del_mismo_juego_tienen_marcadores_separados()
    {
        var juegoId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 10, 1000), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 20, 2000), CancellationToken.None);

        Assert.Equal(2, _repo.Marcadores.Count);
    }
}
