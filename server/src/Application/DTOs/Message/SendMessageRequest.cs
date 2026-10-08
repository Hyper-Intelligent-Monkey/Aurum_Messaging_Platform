namespace Application.DTOs.Message;

public class SendMessageRequest
{
    public int? ConversationId { get; set; }
    public int? RecipientId { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = "Text";
    
    public int? ParentMessageId { get; set; }

    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
}