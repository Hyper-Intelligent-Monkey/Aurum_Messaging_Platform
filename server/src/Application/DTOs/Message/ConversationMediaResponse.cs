namespace Application.DTOs.Message;

public class ConversationMediaResponse
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int? SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public DateTime SentAt { get; set; }
}

