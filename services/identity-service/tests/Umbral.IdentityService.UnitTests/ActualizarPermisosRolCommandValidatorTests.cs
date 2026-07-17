using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Validators;

namespace Umbral.IdentityService.UnitTests;

/// <summary>
/// El panel gobierna dos privilegios. ParticiparEnPartidas existe en el dominio pero esta fijo al
/// rol Participante (composite declarado en el realm): moverlo por API descuadraria el modelo y
/// podria tumbar el gameplay del movil.
/// </summary>
public class ActualizarPermisosRolCommandValidatorTests
{
    private readonly ActualizarPermisosRolCommandValidator _validator = new();

    [Theory]
    [InlineData("GestionarPartidas")]
    [InlineData("GestionarEquipos")]
    public async Task Acepta_los_permisos_gobernables(string permiso)
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { permiso });

        var resultado = await _validator.ValidateAsync(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Rechaza_ParticiparEnPartidas_por_no_ser_gobernable()
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { "ParticiparEnPartidas" });

        var resultado = await _validator.ValidateAsync(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task Rechaza_un_permiso_inexistente()
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { "NoExiste" });

        var resultado = await _validator.ValidateAsync(command);

        Assert.False(resultado.IsValid);
    }
}
