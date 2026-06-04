using FluentValidation.TestHelper;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Validators;

namespace Umbral.TriviaGame.Application.Tests.Validators;

public class UpdateTriviaFormCommandValidatorTests
{
    private readonly UpdateTriviaFormCommandValidator _validator = new();

    private static UpdateTriviaFormCommand ValidCommand => new(
        Guid.NewGuid(),
        "General Knowledge Round 1",
        new List<QuestionInputDto>
        {
            new("What is the capital of France?", 10, 30, 1,
                new List<AnswerOptionInputDto>
                {
                    new("Paris", true),
                    new("London", false),
                    new("Berlin", false),
                    new("Madrid", false),
                }),
        });

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.TestValidate(ValidCommand);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyFormId_Fails()
    {
        var cmd = ValidCommand with { FormId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FormId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTitle_Fails(string? title)
    {
        var cmd = ValidCommand with { Title = title! };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithNullQuestions_Fails()
    {
        var cmd = ValidCommand with { Questions = null! };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Questions);
    }

    [Fact]
    public void Validate_WithEmptyQuestions_Fails()
    {
        var cmd = ValidCommand with { Questions = new List<QuestionInputDto>() };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Questions);
    }
}
