using System.Text.Json;
using Umbral.Puntuaciones.Api.Workers;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class HistorialEventMapperTests
{
    private static readonly Guid PartidaId = Guid.NewGuid();
    private static readonly Guid JuegoId = Guid.NewGuid();
    private static readonly Guid PersonaId = Guid.NewGuid();
    private static readonly Guid EquipoId = Guid.NewGuid();

    private static EnvelopeResumen Envelope(string tipo, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return new EnvelopeResumen(
            Guid.NewGuid(), tipo, 1, new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc),
            doc.RootElement.Clone());
    }

    // Un caso por tipo del contrato: payload de ejemplo + ids esperados + claves esperadas del detalle.
    public static TheoryData<string, string, Guid?, Guid?, Guid?, string[]> Casos()
    {
        var sesion = Guid.NewGuid();
        var data = new TheoryData<string, string, Guid?, Guid?, Guid?, string[]>
        {
            { "PartidaPublicadaEnLobby",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","modalidad":"Equipo","minimosParticipacion":1,"maximosParticipacion":10}""",
              null, null, null, new[] { "modalidad", "minimosParticipacion", "maximosParticipacion" } },
            { "PartidaIniciada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","fechaInicio":"2026-07-06T12:00:00Z","primerJuegoId":"{{JuegoId}}","primerJuegoOrden":1}""",
              null, null, null, new[] { "fechaInicio", "primerJuegoId", "primerJuegoOrden" } },
            { "JuegoActivado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","orden":1,"tipoJuego":"Trivia"}""",
              JuegoId, null, null, new[] { "orden", "tipoJuego" } },
            { "PartidaCancelada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","motivo":"MinimosNoAlcanzados","fechaCancelacion":"2026-07-06T12:00:00Z"}""",
              null, null, null, new[] { "motivo", "fechaCancelacion" } },
            { "PartidaFinalizada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","fechaFin":"2026-07-06T12:00:00Z"}""",
              null, null, null, new[] { "fechaFin" } },
            { "RespuestaTriviaValidada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","opcionId":"{{Guid.NewGuid()}}","esCorrecta":true,"instante":"2026-07-06T12:00:00Z","equipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "preguntaId", "opcionId", "esCorrecta", "instante" } },
            { "PuntajeTriviaIncrementado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":null}""",
              JuegoId, PersonaId, null, new[] { "preguntaId", "puntaje", "tiempoRespuestaMs" } },
            { "PreguntaTriviaActivada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","orden":1,"tiempoLimiteSegundos":30,"fechaActivacion":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "preguntaId", "orden", "tiempoLimiteSegundos", "fechaActivacion" } },
            { "PreguntaTriviaCerrada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","motivo":"RespuestaCorrecta","fechaCierre":"2026-07-06T12:00:00Z","ganadorParticipanteId":"{{PersonaId}}","ganadorEquipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "preguntaId", "motivo", "fechaCierre" } },
            { "TesoroQRValidado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","resultado":"Valido","instante":"2026-07-06T12:00:00Z","equipoId":null}""",
              JuegoId, PersonaId, null, new[] { "etapaId", "resultado", "instante" } },
            { "EtapaBDTGanada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","puntaje":10,"tiempoResolucionMs":1234,"equipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "etapaId", "puntaje", "tiempoResolucionMs" } },
            { "EtapaBDTCerrada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","motivo":"Tiempo","fechaCierre":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "etapaId", "motivo", "fechaCierre" } },
            { "EtapaBDTActivada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","orden":1,"tiempoLimiteSegundos":60,"fechaActivacion":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "etapaId", "orden", "tiempoLimiteSegundos", "fechaActivacion" } },
            { "PistaEnviada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","participanteDestinoId":"{{PersonaId}}","texto":"cerca de la fuente","instante":"2026-07-06T12:00:00Z","equipoDestinoId":null}""",
              JuegoId, PersonaId, null, new[] { "texto", "instante" } },
            { "ConvocatoriaCreada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","convocatoriaId":"{{Guid.NewGuid()}}","equipoId":"{{EquipoId}}","usuarioId":"{{PersonaId}}"}""",
              null, PersonaId, EquipoId, new[] { "convocatoriaId" } },
            { "ConvocatoriaRespondida",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","convocatoriaId":"{{Guid.NewGuid()}}","usuarioId":"{{PersonaId}}","estadoConvocatoria":"Aceptada"}""",
              null, PersonaId, null, new[] { "convocatoriaId", "estadoConvocatoria" } },
            { "UbicacionActualizada",
              $$"""{"partidaId":"{{PartidaId}}","participanteId":"{{PersonaId}}","latitud":10.5,"longitud":-66.9,"instante":"2026-07-06T12:00:00Z"}""",
              null, PersonaId, null, new[] { "latitud", "longitud", "instante" } },
            { "InscripcionSolicitada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Individual","participanteId":"{{PersonaId}}","equipoId":null,"instante":"2026-07-06T12:00:00Z"}""",
              null, PersonaId, null, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionSolicitada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Equipo","participanteId":null,"equipoId":"{{EquipoId}}","instante":"2026-07-06T12:00:00Z"}""",
              null, null, EquipoId, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionAceptada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Individual","participanteId":"{{PersonaId}}","equipoId":null,"instante":"2026-07-06T12:00:00Z"}""",
              null, PersonaId, null, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionAceptada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Equipo","participanteId":null,"equipoId":"{{EquipoId}}","instante":"2026-07-06T12:00:00Z"}""",
              null, null, EquipoId, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionRechazada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Individual","participanteId":"{{PersonaId}}","equipoId":null,"instante":"2026-07-06T12:00:00Z"}""",
              null, PersonaId, null, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionRechazada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","modalidad":"Equipo","participanteId":null,"equipoId":"{{EquipoId}}","instante":"2026-07-06T12:00:00Z"}""",
              null, null, EquipoId, new[] { "inscripcionId", "modalidad", "instante" } },
            { "InscripcionEquipoCreada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","inscripcionId":"{{Guid.NewGuid()}}","equipoId":"{{EquipoId}}","instante":"2026-07-06T12:00:00Z"}""",
              null, null, EquipoId, new[] { "inscripcionId", "instante" } },
            { "InscripcionEquipoCancelada",
              $$"""{"partidaId":"{{PartidaId}}","inscripcionId":"{{Guid.NewGuid()}}","equipoId":"{{EquipoId}}","instante":"2026-07-06T12:00:00Z"}""",
              null, null, EquipoId, new[] { "inscripcionId", "instante" } },
        };
        return data;
    }

    [Theory]
    [MemberData(nameof(Casos))]
    public void Mapea_cada_tipo_con_ids_y_detalle(
        string tipo, string payload, Guid? juegoId, Guid? participanteId, Guid? equipoId, string[] clavesDetalle)
    {
        var envelope = Envelope(tipo, payload);

        var comando = HistorialEventMapper.Map(envelope);

        Assert.NotNull(comando);
        Assert.Equal(envelope.EventId, comando!.EventId);
        Assert.Equal(tipo, comando.TipoEvento);
        Assert.Equal(envelope.OccurredAt, comando.OccurredAt);
        Assert.Equal(PartidaId, comando.PartidaId);
        Assert.Equal(juegoId, comando.JuegoId);
        Assert.Equal(participanteId, comando.ParticipanteId);
        Assert.Equal(equipoId, comando.EquipoId);
        using var detalle = JsonDocument.Parse(comando.DetalleJson);
        var claves = detalle.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(clavesDetalle.OrderBy(n => n).ToArray(), claves);
    }

    [Fact]
    public void Tipo_desconocido_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("EventoInventado", $$"""{"partidaId":"{{PartidaId}}"}""")));

    [Fact]
    public void Payload_sin_partidaId_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("PartidaIniciada", """{"fechaInicio":"2026-07-06T12:00:00Z"}""")));

    [Fact]
    public void PartidaId_no_guid_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("PartidaIniciada", """{"partidaId":"no-es-guid"}""")));
}
