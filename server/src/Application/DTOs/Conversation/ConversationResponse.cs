using Application.DTOs.Message;

namespace Application.DTOs.Conversation;

public class ConversationResponse
{
    public int Id { get; set; }
    public List<ParticipantResponse> Participants { get; set; } = [];
    public MessageResponse? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public DateTime? MutedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
}