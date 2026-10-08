using Domain.Entities;
using Application.DTOs.Message;
using FluentValidation;

namespace Application.Validation.Message;

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.RecipientId.HasValue)
            .WithMessage("Must specify either a conversation or a recipient.");

        RuleFor(x => x.MessageType)
            .IsEnumName(typeof(MessageType), caseSensitive: false)
            .WithMessage("Invalid message type.");

        When(x => string.Equals(x.MessageType, nameof(MessageType.Text), StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Text message content cannot be empty.")
                .MaximumLength(2000).WithMessage("Message is too long (max 2000 characters).");
        });

        When(x => !string.Equals(x.MessageType, nameof(MessageType.Text), StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.StoredFileName)
                .NotEmpty()
                .WithMessage("File storage name is required for attachments.");

            RuleFor(x => x.OriginalFileName)
                .NotEmpty()
                .WithMessage("Original filename is required for attachments.");

            RuleFor(x => x.FileSize)
                .NotNull()
                .WithMessage("File size is required.")
                .GreaterThan(0)
                .WithMessage("File size must be greater than 0.")
                .LessThanOrEqualTo(5 * 1024 * 1024)
                .WithMessage("File size exceeds the 5MB limit.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("Content type (MIME type) is required for attachments.");
        });
    }       
}