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
    public void Individual_nace_pendiente_sin_equipo_ni_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        Assert.Equal(Modalidad.Individual, insc.Modalidad);
        Assert.Null(insc.EquipoId);
        Assert.Empty(insc.Convocatorias);
        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.True(insc.EstaPendiente);
        Assert.True(insc.OcupaParticipacion);
        Assert.False(insc.EsActiva);
    }

    [Fact]
    public void PreinscribirEquipo_nace_pendiente_guarda_snapshot_sin_convocatorias()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, m1, new[] { m1, m2 }, partidaId, T0);

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.Empty(insc.Convocatorias);
        Assert.Equal(new[] { m1, m2 }, insc.MiembrosSnapshot);
    }

    [Fact]
    public void Aceptar_individual_pasa_a_activa_y_no_crea_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        var creadas = insc.Aceptar(T0);

        Assert.Equal(EstadoInscripcion.Activa, insc.Estado);
        Assert.True(insc.EsActiva);
        Assert.Empty(creadas);
        Assert.Empty(insc.Convocatorias);
    }

    [Fact]
    public void Aceptar_equipo_crea_una_convocatoria_pendiente_por_miembro()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, m1, new[] { m1, m2 }, partidaId, T0);

        var creadas = insc.Aceptar(T0);

        Assert.Equal(EstadoInscripcion.Activa, insc.Estado);
        Assert.Equal(2, creadas.Count);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.All(insc.Convocatorias, c => Assert.True(c.EstaPendiente));
        Assert.All(insc.Convocatorias, c => Assert.Equal(equipoId, c.EquipoId));
        Assert.All(insc.Convocatorias, c => Assert.Equal(partidaId, c.PartidaId));
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m1);
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m2);
    }

    [Fact]
    public void Rechazar_pasa_a_rechazada_y_deja_de_ocupar_participacion()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        insc.Rechazar();

        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
        Assert.False(insc.OcupaParticipacion);
        Assert.False(insc.EsActiva);
        Assert.False(insc.EstaPendiente);
    }

    [Fact]
    public void ConvocatoriasAceptadas_cuenta_solo_aceptadas_tras_aceptar_equipo()
    {
        var insc = InscripcionPartida.PreinscribirEquipo(
            Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid(), Guid.NewGuid() }, Guid.NewGuid(), T0);
        insc.Aceptar(T0);
        insc.Convocatorias[0].Aceptar(T0);

        Assert.Equal(1, insc.ConvocatoriasAceptadas);
    }
}
