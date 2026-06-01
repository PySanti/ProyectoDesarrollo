using Umbral.TeamService.Application.Teams.JoinTeamByCode;

namespace Umbral.TeamService.UnitTests;

public sealed class UnirseAEquipoValidatorTests
{
    private readonly UnirseAEquipoPorCodigoCommandValidator _validator = new();

    [Fact]
    public async Task Should_Fail_When_ActorUserId_Is_Empty()
    {
        var result = await _validator.ValidateAsync(new UnirseAEquipoPorCodigoCommand(Guid.Empty, "ABCD1234"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Should_Fail_When_CodigoAcceso_Is_Empty()
    {
        var result = await _validator.ValidateAsync(new UnirseAEquipoPorCodigoCommand(Guid.NewGuid(), string.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Should_Pass_With_Valid_Command()
    {
        var result = await _validator.ValidateAsync(new UnirseAEquipoPorCodigoCommand(Guid.NewGuid(), "ABCD1234"));
        Assert.True(result.IsValid);
    }
}
