using Domain.Exceptions;
namespace Domain.Entities;

public class Conversation
{
    private readonly List<ConversationParticipant> _participants = new();
    private readonly List<Message> _messages = new();


    private Conversation() { }

    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; private set; } = DateTime.UtcNow;
    public string? StorageFolder { get; private set; }

    public int? LastMessageId { get; private set; }
    public virtual Message? LastMessage { get; private set; }
    public int? LastMessageSenderId { get; private set; }

    public virtual IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();
    public virtual IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    public static Conversation Create()
    {
        return new Conversation
        {
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
    }

    public static Conversation CreateConversation(User sender, User recipient)
    {
        var conversation = Create();
        conversation.AddParticipant(sender);
        conversation.AddParticipant(recipient);
        return conversation;
    }

    // checks if a user is already a participant in this conversation
    // if not, adds them and creates a storage folder for the conversation
    public void AddParticipant(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (_participants.Count >= 2)
            throw new DomainException("A direct conversation cannot have more than 2 participants.");

        if (_participants.Any(p => p.UserId == user.Id && p.UserId != 0))
            throw new DomainException("User is already a participant.");

        var participant = ConversationParticipant.Create(Id, user.Id);
        _participants.Add(participant);
        
        if (_participants.Count == 2)
        {
            StorageFolder = $"conversations/conversation_{Guid.NewGuid():N}";
        }
    }

    public void IncrementUnreadCount(int? senderId)
    {
        foreach (var participant in _participants.Where(p => !senderId.HasValue || p.UserId != senderId.Value))
        {
            participant.IncrementUnreadCount();
        }
    }

    // updates the last message in the conversation
    // this is used to update the last message sent
    public void UpdateLastMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (Id != 0 && message.ConversationId != Id)
            throw new DomainException("Cannot update last message from a different conversation.");

        LastMessageAt = message.SentAt;
        LastMessageSenderId = message.SenderId;
        LastMessageId = message.Id;
        LastMessage = message;

        IncrementUnreadCount(message.SenderId);
    }

    public void ResetUnreadCountForUser(int userId)
    {
        GetParticipant(userId)?.ResetUnreadCount();
    }

    // checks if a user is already a participant in the existing participants
    public bool HasParticipant(int userId)
    {
        return _participants.Any(p => p.UserId == userId);
    }

    public ConversationParticipant? GetParticipant(int userId)
    {
        return _participants.FirstOrDefault(p => p.UserId == userId);
    }
}


