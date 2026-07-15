using System;
using System.Collections.Generic;
using System.Linq;
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

    // 7b-bis: en Equipo, GetByParticipanteActivoAsync solo localiza la sesión del caller
    // cuando SU PROPIA convocatoria ya está Aceptada (AceptarInscripcion crea las
    // convocatorias y simultáneamente pasa la inscripción a Activa — ver
    // InscripcionPartida.Aceptar). Por eso el único estado real observable en mi-sesión
    // para Equipo es "Activa": no existe una combinación alcanzable de "convocatoria
    // propia resoluble" + "inscripción todavía Pendiente" (verificado empíricamente antes
    // de escribir este test: con la inscripción recién preinscrita, o incluso ya aceptada
    // pero sin que el caller haya respondido su propia convocatoria, GetByParticipanteActivoAsync
    // devuelve null — dto null, no hay sesión que exponer).
    private static SesionPartida EquipoConConvocatoriaAceptada(Guid partidaId, Guid lider, Guid equipoId, out InscripcionPartida insc)
    {
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        insc = sesion.PreinscribirEquipo(equipoId, true, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // crea convocatorias, inscripcion -> Activa
        var conv = insc.Convocatorias.Single(c => c.UsuarioId == lider);
        sesion.ResponderConvocatoria(conv.Id.Valor, lider, true, false, T0);
        return sesion;
    }

    [Fact]
    public async Task Equipo_convocatoria_propia_aceptada_expone_estado_real_de_la_inscripcion()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var sesion = EquipoConConvocatoriaAceptada(partidaId, lider, equipoId, out var insc);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(lider), default);

        Assert.NotNull(dto);
        Assert.Equal("Activa", dto!.Inscripcion.Estado); // antes del fix: "Equipo" (fallback roto)
        Assert.Equal(insc.Id.Valor, dto.Inscripcion.InscripcionId); // antes del fix: Guid.Empty
    }
}
