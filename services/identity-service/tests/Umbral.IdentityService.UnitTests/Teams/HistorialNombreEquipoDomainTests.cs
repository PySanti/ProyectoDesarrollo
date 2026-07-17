using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class HistorialNombreEquipoDomainTests
{
    [Fact]
    public void Registrar_crea_fila_con_datos_y_fecha()
    {
        var usuario = Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var fecha = new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc);

        var h = HistorialNombreEquipo.Registrar(usuario, equipo, "  Titanes  ", fecha);

        Assert.NotEqual(Guid.Empty, h.Id);
        Assert.Equal(usuario, h.SubjectId);
        Assert.Equal(equipo, h.EquipoId);
        Assert.Equal("Titanes", h.NombreEquipo);
        Assert.Equal(fecha, h.FechaRegistroUtc);
    }

    [Fact]
    public void Registrar_con_nombre_vacio_lanza()
    {
        Assert.Throws<ArgumentException>(
            () => HistorialNombreEquipo.Registrar(Guid.NewGuid(), Guid.NewGuid(), "  ", DateTime.UtcNow));
    }
}
