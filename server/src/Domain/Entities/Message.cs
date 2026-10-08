using Domain.Exceptions;

namespace Domain.Entities;

public enum MessageType { Text, Image, Video, Audio, Document, Location, Other }

public class Message
{
    // prevents direct instantiation
    private Message() { }

    public int Id { get; private set; }
    public MessageType Type { get; private set; } = MessageType.Text;
    public string? Content { get; private set; } // Caption or Text
    
    // File Details
    public string? StoredFileName { get; private set; }  // Unique name on disk
    public string? OriginalFileName { get; private set; } // Original name
    public long? FileSize { get; private set; }
    public string? ContentType { get; private set; }

    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    
    // Status Logic
    public bool IsDelivered { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public bool IsSeen { get; private set; }
    public DateTime? SeenAt { get; private set; }
    
    // Edit Logic
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    
    // Deletion Logic
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Reply Logic
    public int? ParentMessageId { get; private set; }
    public virtual Message? ParentMessage { get; private set; }

    public int? SenderId { get; private set; }
    public virtual User? Sender { get; private set; }
    public int ConversationId { get; private set; }
    public virtual Conversation Conversation { get; private set; } = null!;

    public static Message Create( // Factory method
        int senderId, 
        int conversationId, 
        string? content, 
        MessageType type, 
        string? storedFileName = null, 
        string? originalFileName = null,
        long? fileSize = null,
        string? contentType = null,
        int? parentMessageId = null)
    {
        Validate(senderId, conversationId, content, type, storedFileName, fileSize);

        return new Message
        {
            SenderId = senderId,
            ConversationId = conversationId,
            Content = content,
            Type = type,
            StoredFileName = storedFileName,
            OriginalFileName = originalFileName,
            FileSize = fileSize,
            ContentType = contentType,
            ParentMessageId = parentMessageId,
            SentAt = DateTime.UtcNow,
            IsDelivered = false,
            IsSeen = false,
            IsEdited = false,
            IsDeleted = false
        };
    }

    // Logic Checks (Authorization)
    public bool IsSender(int userId) => SenderId == userId;
    public bool CanBeViewedBy(int userId) => Conversation.HasParticipant(userId);

    public void MarkAsDelivered()
    {
        if (!IsDelivered)
        {
            IsDelivered = true;
            DeliveredAt = DateTime.UtcNow;
        }
    }

    public void MarkAsSeen()
    {
        if (!IsSeen)
        {
            IsSeen = true;
            SeenAt = DateTime.UtcNow;
            MarkAsDelivered(); 
        }
    }

    // Edit
    private static readonly TimeSpan EditTimeLimit = TimeSpan.FromMinutes(15);
    public bool CanBeEdited => !IsDeleted && Type == MessageType.Text && (DateTime.UtcNow - SentAt) <= EditTimeLimit;

    public void EditContent(string newContent)
    {
        if (IsDeleted) throw new DomainException("Cannot edit a deleted message.");
        if (Type != MessageType.Text) throw new DomainException("Only text messages can be edited.");

        if (DateTime.UtcNow - SentAt > EditTimeLimit)
        {
            throw new DomainException("Message can not be edited.");
        }
        
        if (string.IsNullOrWhiteSpace(newContent)) 
            throw new DomainException("Edited content cannot be empty.");

        Content = newContent;
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsDeleted) throw new DomainException("Message is already deleted.");
        
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Type = MessageType.Text;
        Content = "This message was deleted";
        StoredFileName = null;
        OriginalFileName = null;
        FileSize = null;
    }

    private static void Validate(int senderId, int conversationId, string? content, MessageType type, string? storedFileName, long? fileSize)
    {
        if (senderId <= 0) throw new DomainException("A valid sender is required.");
        if (conversationId <= 0) throw new DomainException("A valid conversation is required.");

        if (type == MessageType.Text && string.IsNullOrWhiteSpace(content))
            throw new DomainException("Text message cannot be empty.");

        if (content != null && content.Length > 4000)
            throw new DomainException("Message content cannot exceed 4000 characters.");

        if (type != MessageType.Text)
        {
            if (string.IsNullOrEmpty(storedFileName))
                throw new DomainException("File message must have a stored filename.");
            
            if (!fileSize.HasValue || fileSize <= 0)
                throw new DomainException("File messages must have a valid file size.");
            
            if (fileSize > 5 * 1024 * 1024) 
                throw new DomainException("File cannot exceed 5MB.");
        }
    }
}