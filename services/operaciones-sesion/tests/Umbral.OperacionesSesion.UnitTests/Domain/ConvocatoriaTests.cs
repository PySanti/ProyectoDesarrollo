using System;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ConvocatoriaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Nace_pendiente()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        Assert.Equal(EstadoConvocatoria.Pendiente, c.Estado);
        Assert.True(c.EstaPendiente);
        Assert.False(c.EstaAceptada);
        Assert.Null(c.FechaRespuesta);
    }

    [Fact]
    public void Aceptar_marca_aceptada_y_sella_fecha()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        c.Aceptar(T0.AddMinutes(1));
        Assert.Equal(EstadoConvocatoria.Aceptada, c.Estado);
        Assert.True(c.EstaAceptada);
        Assert.Equal(T0.AddMinutes(1), c.FechaRespuesta);
    }

    [Fact]
    public void Rechazar_marca_rechazada_y_sella_fecha()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        c.Rechazar(T0.AddMinutes(2));
        Assert.Equal(EstadoConvocatoria.Rechazada, c.Estado);
        Assert.False(c.EstaPendiente);
        Assert.Equal(T0.AddMinutes(2), c.FechaRespuesta);
    }
}
