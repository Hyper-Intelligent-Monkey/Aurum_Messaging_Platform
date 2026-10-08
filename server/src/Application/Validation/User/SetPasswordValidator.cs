using Application.DTOs.User;
using FluentValidation;

namespace Application.Validation.User;

public class SetPasswordValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.");
    }
}
