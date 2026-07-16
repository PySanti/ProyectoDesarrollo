using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;

using Umbral.IdentityService.Application.Validators;
namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class CrearEquipoValidatorTests
{
    private readonly CrearEquipoCommandValidator _validator = new();

    [Fact]
    public async Task Should_Fail_When_ActorUserId_Is_Empty()
    {
        var result = await _validator.ValidateAsync(new CrearEquipoCommand(Guid.Empty, "Equipo"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Should_Fail_When_NombreEquipo_Is_Empty()
    {
        var result = await _validator.ValidateAsync(new CrearEquipoCommand(Guid.NewGuid(), string.Empty));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("****")]
    [InlineData("1234")]
    public async Task Should_Fail_When_NombreEquipo_Has_No_Letters(string nombre)
    {
        var result = await _validator.ValidateAsync(new CrearEquipoCommand(Guid.NewGuid(), nombre));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Should_Pass_With_Valid_Command()
    {
        var result = await _validator.ValidateAsync(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"));
        Assert.True(result.IsValid);
    }
}
