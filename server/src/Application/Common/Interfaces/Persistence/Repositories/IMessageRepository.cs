using Domain.Entities;

namespace Application.Common.Interfaces.Persistence.Repositories;

public interface IMessageRepository
{
    // Get a message
    Task<Message?> GetById(int id, CancellationToken cancellationToken = default);

    // Get messages in a conversation
    Task<IEnumerable<Message>> GetConversationMessages(
        int conversationId, 
        int limit, 
        DateTime? before = null, 
        CancellationToken cancellationToken = default);

    // Add a message
    Task Add(Message message, CancellationToken cancellationToken = default);

    // Update a message
    void Update(Message message);

    // Get a message by its stored file name
    Task<Message?> GetByStoredFileName(string storedFileName, CancellationToken cancellationToken = default);

    // Get unread messages sent by others in a conversation
    Task<IEnumerable<Message>> GetUnreadMessagesInConversation(int conversationId, int userId, CancellationToken cancellationToken = default);

    // Get paginated media/file attachments in a conversation
    Task<IEnumerable<Message>> GetConversationMedia(int conversationId, int limit = 25, DateTime? before = null, CancellationToken cancellationToken = default);
}