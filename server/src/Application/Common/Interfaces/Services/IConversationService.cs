using Application.DTOs.Conversation;

namespace Application.Common.Interfaces.Services;

public interface IConversationService
{
    // get contacted users
    Task<IEnumerable<ConversationResponse>> GetUserConversations(int userId, int limit = 30, DateTime? before = null, CancellationToken cancellationToken = default);   
    // get a conversation
    Task<ConversationResponse> GetConversationById(int conversationId, int userId, CancellationToken cancellationToken = default);
    // search for contacted users
    Task<IEnumerable<ConversationResponse>> SearchUserConversations(int userId, string query, int limit = 10, DateTime? before = null, CancellationToken cancellationToken = default);
    // mute a conversation
    Task MuteConversation(int userId, int conversationId, int? durationMinutes = null, CancellationToken cancellationToken = default);
    // unmute a conversation
    Task UnmuteConversation(int userId, int conversationId, CancellationToken cancellationToken = default);
}