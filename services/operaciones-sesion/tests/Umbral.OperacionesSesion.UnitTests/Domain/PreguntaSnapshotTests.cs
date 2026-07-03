// PreguntaSnapshotTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class PreguntaSnapshotTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot Pregunta(int limite = 30)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "Paris", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "Londres", false);
        return new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, limite, new[] { ok, no });
    }

    private static Guid CorrectaId(PreguntaSnapshot p) => p.Opciones.Single(o => o.EsCorrecta).OpcionId;
    private static Guid IncorrectaId(PreguntaSnapshot p) => p.Opciones.First(o => !o.EsCorrecta).OpcionId;

    [Fact]
    public void Activar_sets_active_and_fecha()
    {
        var p = Pregunta();
        p.Activar(T0);
        Assert.Equal(EstadoPregunta.Activa, p.Estado);
        Assert.Equal(T0, p.FechaActivacion);
    }

    [Fact]
    public void Correct_answer_closes_and_sets_winner_and_score()
    {
        var p = Pregunta();
        p.Activar(T0);
        var part = Guid.NewGuid();
        var r = p.RegistrarRespuesta(part, null, CorrectaId(p), T0.AddSeconds(5));
        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(10, r.Puntaje);
        Assert.Equal(5000, r.TiempoRespuestaMs);
        Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
        Assert.Equal(MotivoCierrePregunta.RespuestaCorrecta, p.MotivoCierre);
        Assert.Equal(part, p.GanadorParticipanteId);
    }

    [Fact]
    public void Wrong_answer_records_but_keeps_open()
    {
        var p = Pregunta();
        p.Activar(T0);
        var r = p.RegistrarRespuesta(Guid.NewGuid(), null, IncorrectaId(p), T0.AddSeconds(2));
        Assert.False(r.EsCorrecta);
        Assert.False(r.CerroPregunta);
        Assert.Null(r.Puntaje);
        Assert.Equal(EstadoPregunta.Activa, p.Estado);
        Assert.Single(p.Respuestas);
    }

    [Fact]
    public void Duplicate_answer_by_same_participant_throws()
    {
        var p = Pregunta();
        p.Activar(T0);
        var part = Guid.NewGuid();
        p.RegistrarRespuesta(part, null, IncorrectaId(p), T0.AddSeconds(1));
        Assert.Throws<RespuestaDuplicadaException>(() => p.RegistrarRespuesta(part, null, IncorrectaId(p), T0.AddSeconds(2)));
    }

    [Fact]
    public void Answer_after_time_limit_throws()
    {
        var p = Pregunta(limite: 30);
        p.Activar(T0);
        Assert.Throws<PreguntaFueraDeTiempoException>(
            () => p.RegistrarRespuesta(Guid.NewGuid(), null, CorrectaId(p), T0.AddSeconds(31)));
    }

    [Fact]
    public void Answer_at_exact_deadline_throws()
    {
        var p = Pregunta(limite: 30);
        p.Activar(T0);
        Assert.Throws<PreguntaFueraDeTiempoException>(
            () => p.RegistrarRespuesta(Guid.NewGuid(), null, CorrectaId(p), T0.AddSeconds(30)));
    }

    [Fact]
    public void Operator_close_sets_motivo_without_winner()
    {
        var p = Pregunta();
        p.Activar(T0);
        p.Cerrar(MotivoCierrePregunta.AvanceOperador, T0.AddSeconds(10), ganador: null);
        Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
        Assert.Null(p.GanadorParticipanteId);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, p.MotivoCierre);
    }
}
