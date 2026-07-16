using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ProyeccionesEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipo(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] {
                    new OpcionSnapshot(Guid.NewGuid(), "ok", true),
                    new OpcionSnapshot(Guid.NewGuid(), "wrong", false)
                })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public void MapearLobby_expone_equipos_con_convocados_y_aceptados()
    {
        var sesion = PartidaEquipo(Guid.NewGuid());
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, usuario, new[] { usuario, Guid.NewGuid() }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var lobby = PublicarPartidaCommandHandler.MapearLobby(sesion);

        var equipo = Assert.Single(lobby.Equipos);
        Assert.Equal(2, equipo.Convocados);
        Assert.Equal(1, equipo.Aceptados);
    }

    [Fact]
    public async Task MiSesion_expone_convocatoria_del_convocado()
    {
        var partidaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var sesion = PartidaEquipo(partidaId);
        var insc = sesion.PreinscribirEquipo(equipoId, true, usuario, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(usuario), default);

        Assert.NotNull(dto);
        Assert.NotNull(dto!.Convocatoria);
        Assert.Equal("Aceptada", dto.Convocatoria!.Estado);
        Assert.Equal(equipoId, dto.Convocatoria.EquipoId);
        Assert.Equal(insc.Convocatorias[0].Id.Valor, dto.Convocatoria.ConvocatoriaId);
    }

    [Fact]
    public async Task YaRespondio_es_true_para_todo_el_equipo_que_ya_respondio_y_false_para_los_demas()
    {
        var partidaId = Guid.NewGuid();
        var liderA = Guid.NewGuid();
        var miembroA = Guid.NewGuid();
        var liderB = Guid.NewGuid();
        var equipoIdA = Guid.NewGuid();
        var equipoIdB = Guid.NewGuid();

        var sesion = PartidaEquipo(partidaId);

        // Pre-inscribir equipo A con líder y miembro
        var inscA = sesion.PreinscribirEquipo(equipoIdA, true, liderA, new[] { liderA, miembroA }, false, 0, T0);
        sesion.AceptarInscripcion(inscA.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        var convocA1 = inscA.Convocatorias[0];
        var convocA2 = inscA.Convocatorias[1];
        sesion.ResponderConvocatoria(convocA1.Id.Valor, liderA, true, false, T0);
        sesion.ResponderConvocatoria(convocA2.Id.Valor, miembroA, true, false, T0);

        // Pre-inscribir equipo B con líder
        var inscB = sesion.PreinscribirEquipo(equipoIdB, true, liderB, new[] { liderB }, false, 1, T0);
        sesion.AceptarInscripcion(inscB.Id.Valor, 1, T0); // HU-19: aceptar crea las convocatorias
        var convocB = inscB.Convocatorias[0];
        sesion.ResponderConvocatoria(convocB.Id.Valor, liderB, true, false, T0);

        // Iniciar sesión
        sesion.Iniciar(T0);

        // El líder de A responde incorrectamente a la pregunta (sella el equipo)
        var juego = sesion.Juegos.First(j => j.Estado == EstadoJuego.Activo && j.TipoJuego == TipoJuego.Trivia);
        var preg = juego.PreguntaActiva;
        Assert.NotNull(preg);
        var opcionIncorrecta = preg!.Opciones.First(o => !o.EsCorrecta);
        sesion.ResponderPregunta(liderA, opcionIncorrecta.OpcionId, T0.AddSeconds(1));

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var deMiembroA = await handler.Handle(new ObtenerMiSesionQuery(miembroA), default);
        var deLiderB = await handler.Handle(new ObtenerMiSesionQuery(liderB), default);

        Assert.True(deMiembroA!.YaRespondioPreguntaActual);
        Assert.False(deLiderB!.YaRespondioPreguntaActual);
    }

    [Fact]
    public async Task MiSesion_prefiere_la_convocatoria_aceptada_sobre_la_pendiente()
    {
        var partidaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();
        var sesion = PartidaEquipo(partidaId);

        // Equipo A convoca al usuario (queda Pendiente; inscripción A es la primera de la lista)
        var inscA = sesion.PreinscribirEquipo(equipoA, true, usuario, new[] { usuario, Guid.NewGuid() }, false, 0, T0);
        sesion.AceptarInscripcion(inscA.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias (usuario queda Pendiente)

        // Equipo B convoca al usuario y este acepta
        var inscB = sesion.PreinscribirEquipo(equipoB, true, usuario, new[] { usuario }, false, 1, T0);
        sesion.AceptarInscripcion(inscB.Id.Valor, 1, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(inscB.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(usuario), default);

        Assert.NotNull(dto!.Convocatoria);
        Assert.Equal("Aceptada", dto.Convocatoria!.Estado);
        Assert.Equal(equipoB, dto.Convocatoria.EquipoId);
    }
}
