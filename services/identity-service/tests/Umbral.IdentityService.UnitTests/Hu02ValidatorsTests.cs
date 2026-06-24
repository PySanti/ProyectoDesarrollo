using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;

using Umbral.IdentityService.Application.Validators;
namespace Umbral.IdentityService.UnitTests;

public sealed class Hu02ValidatorsTests
{
    [Fact]
    public void UpdateUserGeneralDataValidator_Should_Pass_For_Valid_Command()
    {
        var validator = new UpdateUserGeneralDataCommandValidator();
        var command = new UpdateUserGeneralDataCommand(Guid.NewGuid(), "Admin", "admin@umbral.dev");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateUserGeneralDataValidator_Should_Fail_When_UserId_Is_Empty()
    {
        var validator = new UpdateUserGeneralDataCommandValidator();
        var command = new UpdateUserGeneralDataCommand(Guid.Empty, "Admin", "admin@umbral.dev");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateUserGeneralDataValidator_Should_Fail_When_Email_Is_Invalid()
    {
        var validator = new UpdateUserGeneralDataCommandValidator();
        var command = new UpdateUserGeneralDataCommand(Guid.NewGuid(), "Admin", "invalid-email");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeactivateUserValidator_Should_Pass_For_Valid_Command()
    {
        var validator = new DeactivateUserCommandValidator();
        var command = new DeactivateUserCommand(Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DeactivateUserValidator_Should_Fail_When_UserId_Is_Empty()
    {
        var validator = new DeactivateUserCommandValidator();
        var command = new DeactivateUserCommand(Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
