using FluentValidation.TestHelper;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Validators;

namespace Umbral.TriviaGame.Application.Tests.Validators;

public class StartTriviaGameCommandValidatorTests
{
    private readonly StartTriviaGameCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var cmd = new StartTriviaGameCommand(PartidaId: Guid.NewGuid());
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyPartidaId_Fails()
    {
        var cmd = new StartTriviaGameCommand(PartidaId: Guid.Empty);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PartidaId);
    }
}
