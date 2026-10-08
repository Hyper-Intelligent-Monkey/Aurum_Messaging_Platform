using Application.DTOs.User;
using FluentValidation;

namespace Application.Validation.User;

public class UpdateAvatarValidator : AbstractValidator<UpdateAvatarRequest>
{
    public UpdateAvatarValidator()
    {
        RuleFor(x => x.StoredFileName)
            .MaximumLength(500).WithMessage("Avatar file name is too long.")
            .When(x => !string.IsNullOrEmpty(x.StoredFileName));
    }
}