using FluentValidation.TestHelper;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Validators;

namespace Umbral.TriviaGame.Application.Tests.Validators;

public class CreateTriviaGameCommandValidatorTests
{
    private readonly CreateTriviaGameCommandValidator _validator = new();

    private static CreateTriviaGameCommand ValidIndividualCommand => new(
        Nombre: "Trivia Demo Sprint 1",
        Modalidad: "Individual",
        ModoInicio: "Manual",
        FormularioId: Guid.NewGuid(),
        TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
        MinimoParticipantes: 2,
        MaximoJugadores: 10,
        MaximoEquipos: null,
        MinimoJugadoresPorEquipo: null,
        MaximoJugadoresPorEquipo: null);

    private static CreateTriviaGameCommand ValidEquipoCommand => new(
        Nombre: "Trivia Equipo Sprint 1",
        Modalidad: "Equipo",
        ModoInicio: "Automatico",
        FormularioId: Guid.NewGuid(),
        TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
        MinimoParticipantes: 2,
        MaximoJugadores: null,
        MaximoEquipos: 5,
        MinimoJugadoresPorEquipo: 2,
        MaximoJugadoresPorEquipo: 5);

    [Fact]
    public void Validate_WithValidIndividualCommand_Passes()
    {
        var result = _validator.TestValidate(ValidIndividualCommand);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidEquipoCommand_Passes()
    {
        var result = _validator.TestValidate(ValidEquipoCommand);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyNombre_Fails(string? nombre)
    {
        var cmd = ValidIndividualCommand with { Nombre = nombre! };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Fact]
    public void Validate_WithNombreTooShort_Fails()
    {
        var cmd = ValidIndividualCommand with { Nombre = "AB" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Fact]
    public void Validate_WithNombreTooLong_Fails()
    {
        var cmd = ValidIndividualCommand with { Nombre = new string('a', 101) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithEmptyModalidad_Fails(string? modalidad)
    {
        var cmd = ValidIndividualCommand with { Modalidad = modalidad! };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Modalidad);
    }

    [Fact]
    public void Validate_WithInvalidModalidad_Fails()
    {
        var cmd = ValidIndividualCommand with { Modalidad = "Invalid" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Modalidad);
    }

    [Fact]
    public void Validate_WithNullMaximoJugadoresInIndividual_Fails()
    {
        var cmd = ValidIndividualCommand with { MaximoJugadores = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaximoJugadores);
    }

    [Fact]
    public void Validate_WithMaximoEquiposInIndividual_Fails()
    {
        var cmd = ValidIndividualCommand with { MaximoEquipos = 3 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaximoEquipos);
    }

    [Fact]
    public void Validate_WithLimitesPorEquipoInIndividual_Fails()
    {
        var cmd = ValidIndividualCommand with
        {
            MinimoJugadoresPorEquipo = 1,
            MaximoJugadoresPorEquipo = 3
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MinimoJugadoresPorEquipo);
        result.ShouldHaveValidationErrorFor(x => x.MaximoJugadoresPorEquipo);
    }

    [Fact]
    public void Validate_WithNullMaximoEquiposInEquipo_Fails()
    {
        var cmd = ValidEquipoCommand with { MaximoEquipos = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaximoEquipos);
    }

    [Fact]
    public void Validate_WithMaximoJugadoresInEquipo_Fails()
    {
        var cmd = ValidEquipoCommand with { MaximoJugadores = 10 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaximoJugadores);
    }

    [Fact]
    public void Validate_WithNullMinimoJugadoresPorEquipoInEquipo_Fails()
    {
        var cmd = ValidEquipoCommand with { MinimoJugadoresPorEquipo = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MinimoJugadoresPorEquipo);
    }

    [Fact]
    public void Validate_WithNullMaximoJugadoresPorEquipoInEquipo_Fails()
    {
        var cmd = ValidEquipoCommand with { MaximoJugadoresPorEquipo = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaximoJugadoresPorEquipo);
    }

    [Fact]
    public void Validate_WithEmptyFormularioId_Fails()
    {
        var cmd = ValidIndividualCommand with { FormularioId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FormularioId);
    }

    [Fact]
    public void Validate_WithMinimoParticipantesZero_Fails()
    {
        var cmd = ValidIndividualCommand with { MinimoParticipantes = 0 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MinimoParticipantes);
    }
}
