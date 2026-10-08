using Application.DTOs.Message;

namespace Application.Common.Interfaces.Services;

public interface IMessagingService
{
    // send a text message
    Task<MessageResponse> SendMessage(int senderId, SendMessageRequest request, CancellationToken cancellationToken = default);
    // get conversation messages
    Task<IEnumerable<MessageResponse>> GetMessages(int conversationId, int userId, int limit = 50, DateTime? before = null, CancellationToken cancellationToken = default);
    // edit own message
    Task<MessageResponse> EditMessage(int userId, EditMessageRequest request, CancellationToken cancellationToken = default);
    // delete own message
    Task<MessageResponse> DeleteMessage(int userId, int messageId, CancellationToken cancellationToken = default);
    // mark messages as seen
    Task MarkMessagesAsSeen(int userId, int conversationId, CancellationToken cancellationToken = default);
    // get media files sent in a conversation
    Task<IEnumerable<ConversationMediaResponse>> GetConversationMedia(int userId, int conversationId, int limit = 25, DateTime? before = null, CancellationToken cancellationToken = default);
}