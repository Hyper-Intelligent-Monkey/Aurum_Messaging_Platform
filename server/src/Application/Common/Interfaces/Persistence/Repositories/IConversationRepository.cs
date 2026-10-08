using Domain.Entities;

namespace Application.Common.Interfaces.Persistence.Repositories;

public interface IConversationRepository
{
    // Get a conversation
    Task<Conversation?> GetById(int id, CancellationToken cancellationToken = default);

    // Get conversations of a user
    Task<IEnumerable<Conversation>> GetUserConversations(int userId, int limit = 30, DateTime? before = null, CancellationToken cancellationToken = default);

    // Get conversation of 2 users
    Task<Conversation?> FindDirectConversation(int userId1, int userId2, CancellationToken cancellationToken = default);

    // Add a conversation
    Task Add(Conversation conversation, CancellationToken cancellationToken = default);

    // Update a conversation
    void Update(Conversation conversation);

    // Search for a contacted user
    Task<IEnumerable<Conversation>> SearchUserConversations(int userId, string query, int limit = 10, DateTime? before = null, CancellationToken cancellationToken = default);
}