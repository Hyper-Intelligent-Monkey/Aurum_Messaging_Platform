using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validation.Auth;

public class GoogleLoginValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}

