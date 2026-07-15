using System;
using System.Linq;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.Validators;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Directory;

public class ResolverNombresQueryValidatorTests
{
    private static Guid[] Ids(int n) => Enumerable.Range(0, n).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void Lote_en_el_tope_exacto_es_valido()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Ids(200), Array.Empty<Guid>()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void El_tope_cuenta_ambas_listas_sumadas()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Ids(150), Ids(51)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Lote_vacio_es_valido()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Array.Empty<Guid>(), Array.Empty<Guid>()));

        Assert.True(result.IsValid);
    }
}
