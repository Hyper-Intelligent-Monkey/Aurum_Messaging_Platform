using Application.DTOs.Message;
using FluentValidation;

namespace Application.Validation.Message;

public class EditMessageValidator : AbstractValidator<EditMessageRequest>
{
    public EditMessageValidator()
    {
        RuleFor(x => x.MessageId)
            .GreaterThan(0).WithMessage("Valid message ID is required.");

        RuleFor(x => x.NewContent)
            .NotEmpty().WithMessage("Edited message content cannot be empty.")
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
    }
}