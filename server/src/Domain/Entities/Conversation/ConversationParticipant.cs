namespace Domain.Entities;

public class ConversationParticipant
{
    private ConversationParticipant() {}

    public int Id { get; private set; }

    public int ConversationId { get; private set; }
    public virtual Conversation Conversation { get; private set; } = null!;

    public int UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    public int UnreadCount { get; private set; } = 0;
    public DateTime LastReadAt { get; private set; } = DateTime.UtcNow;

    public bool IsMuted { get; private set; } = false;
    public DateTime? MutedUntil { get; private set; }

    public static ConversationParticipant Create(int conversationId, int userId)
    {
        return new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userId,
            LastReadAt = DateTime.UtcNow,
            UnreadCount = 0,
            IsMuted = false
        };
    }

    public void IncrementUnreadCount()
    {
        UnreadCount++;
    }

    public void ResetUnreadCount()
    {
        UnreadCount = 0;
        LastReadAt = DateTime.UtcNow;
    }

    public void MuteForMinutes(int minutes)
    {
        Mute(DateTime.UtcNow.AddMinutes(minutes));
    }

    public void MuteIndefinitely() => Mute(null);

    private void Mute(DateTime? until = null)
    {
        IsMuted = true;
        MutedUntil = until;
    }

    public void Unmute()
    {
        IsMuted = false;
        MutedUntil = null;
    }

}