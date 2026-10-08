namespace Application.DTOs.Conversation;

public class ParticipantResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? Avatar { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsDeleted { get; set; }
}