namespace Application.DTOs.Message;

public class MessageResponse
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? SenderId { get; set; }
    public string SenderName { get; set; } = null!;

    public string? Content { get; set; }
    public string MessageType { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public int? ParentMessageId { get; set; }

    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? FileUrl { get; set; }

    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsSeen { get; set; }
    public DateTime? SeenAt { get; set; }
    public bool IsDeleted { get; set; }
}