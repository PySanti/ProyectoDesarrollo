using Umbral.BdtGameService.Application.Games.ListPublished;

namespace Umbral.BdtGameService.UnitTests;

public sealed class ListarPartidasBdtPublicadasValidatorTests
{
    [Fact]
    public void Validator_Should_Accept_Omitted_And_Known_Modality()
    {
        var validator = new ListarPartidasBdtPublicadasQueryValidator();

        Assert.True(validator.Validate(new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), null)).IsValid);
        Assert.True(validator.Validate(new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), "Individual")).IsValid);
        Assert.True(validator.Validate(new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), "Equipo")).IsValid);
    }

    [Fact]
    public void Validator_Should_Reject_Invalid_Modality_And_Empty_Actor()
    {
        var validator = new ListarPartidasBdtPublicadasQueryValidator();

        var result = validator.Validate(new ListarPartidasBdtPublicadasQuery(Guid.Empty, "Mixta"));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }
}
