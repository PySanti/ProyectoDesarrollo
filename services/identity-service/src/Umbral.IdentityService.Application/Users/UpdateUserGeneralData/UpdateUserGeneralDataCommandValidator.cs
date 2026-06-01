using FluentValidation;

namespace Umbral.IdentityService.Application.Users.UpdateUserGeneralData;

public sealed class UpdateUserGeneralDataCommandValidator : AbstractValidator<UpdateUserGeneralDataCommand>
{
    public UpdateUserGeneralDataCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
