// TriviaLeafTypesTests.cs
using System;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class TriviaLeafTypesTests
{
    [Fact]
    public void Opcion_and_respuesta_expose_their_data()
    {
        var op = new OpcionSnapshot(Guid.NewGuid(), "Paris", true);
        Assert.True(op.EsCorrecta);
        Assert.Equal("Paris", op.Texto);

        var pid = Guid.NewGuid();
        var r = new RespuestaTrivia(pid, op.OpcionId, true, new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(pid, r.ParticipanteId);
        Assert.True(r.EsCorrecta);
    }

    [Fact]
    public void Result_records_carry_outcome()
    {
        var rr = new ResultadoRespuesta(true, true, 10, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc), 1200);
        Assert.True(rr.CerroPregunta);
        Assert.Equal(10, rr.Puntaje);

        var av = new ResultadoAvancePregunta(Guid.NewGuid(), Guid.NewGuid(), 1, MotivoCierrePregunta.AvanceOperador,
            null, null, null, null, true);
        Assert.True(av.SinMasPreguntas);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, av.MotivoCierre);
    }
}
