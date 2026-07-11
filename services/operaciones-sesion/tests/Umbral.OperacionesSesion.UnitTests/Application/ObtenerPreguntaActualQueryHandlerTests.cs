using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerPreguntaActualQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Iniciada(Guid partidaId)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "Paris", true), new OpcionSnapshot(Guid.NewGuid(), "Londres", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Returns_active_question_without_correct_flag()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId));
        var handler = new ObtenerPreguntaActualQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerPreguntaActualQuery(partidaId), CancellationToken.None);

        Assert.Equal(1, dto.Orden);
        Assert.Equal(2, dto.Opciones.Count);
        // DTO de opción pública NO tiene propiedad EsCorrecta:
        Assert.Null(typeof(Umbral.OperacionesSesion.Application.DTOs.OpcionPublicaDto).GetProperty("EsCorrecta"));
    }

    [Fact]
    public async Task No_active_question_throws()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "a", true), new OpcionSnapshot(Guid.NewGuid(), "b", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        repo.Add(SesionPartida.Publicar(partidaId, snap)); // Lobby, ninguna pregunta activa
        var handler = new ObtenerPreguntaActualQueryHandler(repo);

        await Assert.ThrowsAsync<NoHayPreguntaActivaException>(
            () => handler.Handle(new ObtenerPreguntaActualQuery(partidaId), CancellationToken.None));
    }
}
