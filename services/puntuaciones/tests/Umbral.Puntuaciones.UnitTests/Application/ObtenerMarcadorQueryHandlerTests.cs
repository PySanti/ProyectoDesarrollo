using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerMarcadorQueryHandlerTests
{
    private readonly FakeProyeccionesRepository _repo = new();

    [Fact]
    public async Task Devuelve_marcador_con_posicion_actual()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
        var lider = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        lider.Acreditar(30, 1000);
        _repo.AddMarcador(lider);
        var consultado = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        consultado.Acreditar(10, 2000);
        _repo.AddMarcador(consultado);

        var r = await new ObtenerMarcadorQueryHandler(_repo).Handle(
            new ObtenerMarcadorQuery(partidaId, juegoId, consultado.CompetidorId), CancellationToken.None);

        Assert.Equal(consultado.CompetidorId, r.CompetidorId);
        Assert.Equal(10, r.Puntos);
        Assert.Equal(2, r.Posicion);
    }

    [Fact]
    public async Task Competidor_sin_marcador_ni_participacion_lanza_404()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));

        await Assert.ThrowsAsync<MarcadorNoEncontradoException>(() =>
            new ObtenerMarcadorQueryHandler(_repo).Handle(
                new ObtenerMarcadorQuery(partidaId, juegoId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Inscrito_que_no_anoto_ve_su_cero_en_vez_de_404()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var mudo = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
        var anotador = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        anotador.Acreditar(30, 1000);
        _repo.AddMarcador(anotador);
        _repo.AddParticipacion(ParticipacionProyectada.Nueva(partidaId, mudo, TipoCompetidor.Participante));

        var r = await new ObtenerMarcadorQueryHandler(_repo).Handle(
            new ObtenerMarcadorQuery(partidaId, juegoId, mudo), CancellationToken.None);

        Assert.Equal(0, r.Puntos);
        Assert.Equal(2, r.Posicion);
    }

    [Fact]
    public async Task Juego_desconocido_lanza_404_de_juego()
    {
        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerMarcadorQueryHandler(_repo).Handle(
                new ObtenerMarcadorQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}
