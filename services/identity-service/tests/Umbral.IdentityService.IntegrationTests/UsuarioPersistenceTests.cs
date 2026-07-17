using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.ValueObjects;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class UsuarioPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"usuario-{Guid.NewGuid()}").Options);

    // Estos tests aseveran el modelo de EF y no un round-trip de datos: todo el suite de
    // persistencia usa UseInMemoryDatabase, y el proveedor InMemory no garantiza ejercer los
    // ValueConverter — guarda el objeto tal cual. Un round-trip pasaria igual sin el converter
    // registrado, y no probaria nada. Aseverar el modelo si muerde. Mismo patron que
    // Teams/InvitacionEquipoPersistenceTests.

    [Fact]
    public void UsuarioId_se_mapea_con_el_ValueConverter_a_Guid()
    {
        using var ctx = NewContext();

        var propiedad = ctx.Model.FindEntityType(typeof(Usuario))!.FindProperty(nameof(Usuario.UsuarioId));

        Assert.NotNull(propiedad);
        var converter = propiedad!.GetValueConverter();
        // Sin converter, Npgsql no sabe guardar un UsuarioLocalId y el servicio revienta al
        // primer SaveChanges contra Postgres — que ningun test toca (todos son InMemory).
        Assert.NotNull(converter);
        Assert.Equal(typeof(UsuarioLocalId), converter!.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void UsuarioId_sigue_mapeado_a_la_columna_usuarioid()
    {
        using var ctx = NewContext();

        var propiedad = ctx.Model.FindEntityType(typeof(Usuario))!.FindProperty(nameof(Usuario.UsuarioId));

        // El tipado no cambia la columna: sigue siendo uuid, y por eso este slice no lleva
        // parche de esquema para usuarios.
        Assert.Equal("usuarioid", propiedad!.GetColumnName());
    }
}
