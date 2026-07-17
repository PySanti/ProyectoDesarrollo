using System;
using System.Linq;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Application.Validators;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ResolverNombresPartidaQueryValidatorTests
{
    private static ResolverNombresPartidaQuery ConIds(int cantidad)
        => new(Enumerable.Range(0, cantidad).Select(_ => Guid.NewGuid()).ToList());

    [Fact]
    public void Acepta_el_tope_exacto()
    {
        var r = new ResolverNombresPartidaQueryValidator()
            .Validate(ConIds(ResolverNombresPartidaQueryValidator.MaxIds));

        Assert.True(r.IsValid);
    }

    [Fact]
    public void Rechaza_pasado_el_tope()
    {
        var r = new ResolverNombresPartidaQueryValidator()
            .Validate(ConIds(ResolverNombresPartidaQueryValidator.MaxIds + 1));

        Assert.False(r.IsValid);
    }

    [Fact]
    public void Acepta_lista_vacia()
    {
        var r = new ResolverNombresPartidaQueryValidator().Validate(ConIds(0));

        Assert.True(r.IsValid);
    }
}
