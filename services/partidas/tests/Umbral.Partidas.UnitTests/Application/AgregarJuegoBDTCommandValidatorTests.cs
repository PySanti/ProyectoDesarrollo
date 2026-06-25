using System;
using System.Collections.Generic;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoBDTCommandValidatorTests
{
    private readonly AgregarJuegoBDTCommandValidator _validator = new();

    private static AgregarJuegoBDTCommand With(string area, IReadOnlyList<EtapaRequest> etapas)
        => new(Guid.NewGuid(), 1, area, etapas);

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = With("Plaza", new List<EtapaRequest> { new(1, "QR", 50, 120) });
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Blank_area_fails()
    {
        var cmd = With("  ", new List<EtapaRequest> { new(1, "QR", 50, 120) });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Empty_stages_fails()
    {
        Assert.False(_validator.Validate(With("Plaza", new List<EtapaRequest>())).IsValid);
    }

    [Fact]
    public void Stage_with_blank_qr_or_non_positive_values_fails()
    {
        var cmd = With("Plaza", new List<EtapaRequest> { new(1, "", 0, 0) });
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
