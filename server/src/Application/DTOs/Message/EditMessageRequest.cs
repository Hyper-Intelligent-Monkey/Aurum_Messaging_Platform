namespace Application.DTOs.Message;

public class EditMessageRequest
{
    public int MessageId { get; set; }
    public string NewContent { get; set; } = null!;
}