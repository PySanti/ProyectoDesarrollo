// tests/.../Application/BdtValidatorsTests.cs
using FluentValidation.TestHelper;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class BdtValidatorsTests
{
    [Fact]
    public void ValidarTesoro_requires_partidaId_and_imagen()
    {
        var v = new ValidarTesoroCommandValidator();
        v.TestValidate(new ValidarTesoroCommand(Guid.Empty, Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(c => c.PartidaId);
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(c => c.ImagenBase64);
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), "Zm9v"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidarTesoro_imagen_malformada_da_error_de_validacion()
    {
        var v = new ValidarTesoroCommandValidator();
        // base64 malformado → debe tener error en ImagenBase64
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), "no es base64!!"))
            .ShouldHaveValidationErrorFor(c => c.ImagenBase64);
        // base64 válido → sin errores
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), "Zm9v"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AvanzarEtapa_requires_partidaId()
    {
        var v = new AvanzarEtapaCommandValidator();
        v.TestValidate(new AvanzarEtapaCommand(Guid.Empty)).ShouldHaveValidationErrorFor(c => c.PartidaId);
        v.TestValidate(new AvanzarEtapaCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }
}
