using Umbral.IdentityService.Domain.ValueObjects;

namespace Umbral.IdentityService.UnitTests.Domain;

public sealed class UsuarioLocalIdTests
{
    [Fact]
    public void From_conserva_el_valor()
    {
        var guid = Guid.NewGuid();

        var id = UsuarioLocalId.From(guid);

        Assert.Equal(guid, id.Valor);
    }

    [Fact]
    public void New_genera_valores_distintos()
    {
        Assert.NotEqual(UsuarioLocalId.New(), UsuarioLocalId.New());
    }

    [Fact]
    public void Dos_ids_con_el_mismo_valor_son_iguales()
    {
        var guid = Guid.NewGuid();

        // record struct: igualdad por valor. Sin esto un id no serviria como clave ni se
        // podria comparar, y los repositorios lo necesitan.
        Assert.Equal(UsuarioLocalId.From(guid), UsuarioLocalId.From(guid));
    }
}
