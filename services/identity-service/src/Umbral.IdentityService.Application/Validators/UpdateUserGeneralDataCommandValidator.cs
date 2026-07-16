using FluentValidation;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Application.Validators;

public sealed class UpdateUserGeneralDataCommandValidator : AbstractValidator<UpdateUserGeneralDataCommand>
{
    public UpdateUserGeneralDataCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop).TextoHumano(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
