using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class InscripcionPartidaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Individual_es_activa_sin_equipo_ni_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);
        Assert.Equal(Modalidad.Individual, insc.Modalidad);
        Assert.Null(insc.EquipoId);
        Assert.Empty(insc.Convocatorias);
        Assert.True(insc.EsActiva);
    }

    [Fact]
    public void PreinscribirEquipo_genera_una_convocatoria_pendiente_por_miembro()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, new[] { m1, m2 }, partidaId, T0);

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.True(insc.EsActiva);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.All(insc.Convocatorias, c => Assert.True(c.EstaPendiente));
        Assert.All(insc.Convocatorias, c => Assert.Equal(equipoId, c.EquipoId));
        Assert.All(insc.Convocatorias, c => Assert.Equal(partidaId, c.PartidaId));
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m1);
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m2);
        Assert.Equal(0, insc.ConvocatoriasAceptadas);
    }

    [Fact]
    public void ConvocatoriasAceptadas_cuenta_solo_aceptadas()
    {
        var insc = InscripcionPartida.PreinscribirEquipo(
            Guid.NewGuid(), new[] { Guid.NewGuid(), Guid.NewGuid() }, Guid.NewGuid(), T0);
        insc.Convocatorias[0].Aceptar(T0);

        Assert.Equal(1, insc.ConvocatoriasAceptadas);
    }
}
