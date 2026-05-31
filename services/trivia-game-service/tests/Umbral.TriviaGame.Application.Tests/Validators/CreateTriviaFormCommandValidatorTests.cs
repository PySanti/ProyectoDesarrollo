using FluentValidation.TestHelper;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Validators;

namespace Umbral.TriviaGame.Application.Tests.Validators;

public class CreateTriviaFormCommandValidatorTests
{
    private readonly CreateTriviaFormCommandValidator _validator = new();

    private static CreateTriviaFormCommand ValidCommand => new(
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
    public void Validate_WithTitleExceedingMaxLength_Fails()
    {
        var cmd = ValidCommand with { Title = new string('a', 201) };
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

    [Fact]
    public void Validate_QuestionWithoutText_Fails()
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", true),
                        new("London", false),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].Text");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public void Validate_QuestionWithInvalidScore_Fails(int score)
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", score, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", true),
                        new("London", false),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].AssignedScore");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(301)]
    public void Validate_QuestionWithInvalidTimeLimit_Fails(int seconds)
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", 10, seconds, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", true),
                        new("London", false),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].TimeLimitSeconds");
    }

    [Fact]
    public void Validate_QuestionWithThreeOptions_Fails()
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", true),
                        new("London", false),
                        new("Berlin", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].Options.Count");
    }

    [Fact]
    public void Validate_QuestionWithNoCorrectOption_Fails()
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", false),
                        new("London", false),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].Options");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("1 opción correcta"));
    }

    [Fact]
    public void Validate_QuestionWithTwoCorrectOptions_Fails()
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("Paris", true),
                        new("London", true),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].Options");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("1 opción correcta"));
    }

    [Fact]
    public void Validate_OptionWithEmptyText_Fails()
    {
        var cmd = ValidCommand with
        {
            Questions = new List<QuestionInputDto>
            {
                new("What is the capital of France?", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("", true),
                        new("London", false),
                        new("Berlin", false),
                        new("Madrid", false),
                    }),
            }
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Questions[0].Options[0].Text");
    }
}
