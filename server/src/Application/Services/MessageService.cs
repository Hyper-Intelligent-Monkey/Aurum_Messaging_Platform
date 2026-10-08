using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.DTOs.Message;
using Domain.Entities;
using Application.Common.Constants;

namespace Application.Services;

public class MessageService : IMessagingService
{
    private readonly IUnitofWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public MessageService(IUnitofWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    // send a message
    public async Task<MessageResponse> SendMessage(
        int senderId, 
        SendMessageRequest request, 
        CancellationToken cancellationToken = default)
    {
        Conversation conversation;

        if (request.ConversationId.HasValue)
        {
            var conversationId = request.ConversationId.Value;
            var existingConversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
            if (existingConversation == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            if(!existingConversation.HasParticipant(senderId))
            {
                throw new UnauthorizedAccessException("Unauthorized: You are not in this conversation.");
            }

            conversation = existingConversation;
        } else if (request.RecipientId.HasValue)
        {
            var recipientId = request.RecipientId.Value;
            var recipient = await _unitOfWork.Users.GetById(recipientId, cancellationToken);
            if (recipient == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var sender = await _unitOfWork.Users.GetById(senderId, cancellationToken);
            if (sender == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var existingDirectConversation = await _unitOfWork.Conversations.FindDirectConversation(senderId, recipientId, cancellationToken);
            if (existingDirectConversation != null)
            {
                conversation = existingDirectConversation;
            }
            else
            {
                conversation = Conversation.CreateConversation(sender, recipient);
                await _unitOfWork.Conversations.Add(conversation, cancellationToken);
                await _unitOfWork.SaveChanges(cancellationToken);
            }
        }
        else
        {
            throw new ArgumentException("Must supply either a conversationId or a recipientId.");
        }

        foreach (var participant in conversation.Participants)
        {
            if (participant.UserId != senderId)
            {
                // Check if the recipient blocked the sender
                var isBlockedByRecipient = await _unitOfWork.Users.IsBlocked(participant.UserId, senderId, cancellationToken);
                if (isBlockedByRecipient)
                {
                    throw new InvalidOperationException("You cannot send messages to this user because you have been blocked.");
                }

                // Check if the sender blocked the recipient
                var isSenderBlockingRecipient = await _unitOfWork.Users.IsBlocked(senderId, participant.UserId, cancellationToken);
                if (isSenderBlockingRecipient)
                {
                    throw new InvalidOperationException("You cannot send messages to a user you have blocked.");
                }
            }
        }

        if (!Enum.TryParse<MessageType>(request.MessageType, true, out var messageType))
        {
            messageType = MessageType.Text;
        }

        var message = Message.Create(
                senderId,
                conversation.Id,
                request.Content,
                messageType,
                request.StoredFileName,
                request.OriginalFileName,
                request.FileSize,
                request.ContentType,
                request.ParentMessageId
            );

        await _unitOfWork.Messages.Add(message, cancellationToken);
        conversation.UpdateLastMessage(message);
        conversation.ResetUnreadCountForUser(senderId);

        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToMessageResponse(message);
    }

    // get all messages in a conversation
    public async Task<IEnumerable<MessageResponse>> GetMessages(
        int conversationId,
        int userId,
        int limit = Constants.MaxMessageLimit,
        DateTime? before = null,
        CancellationToken cancellationToken = default
    )
    {
        limit = Math.Clamp(limit, 1, Constants.MaxMessageLimit);
        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        if(!conversation.HasParticipant(userId))
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        var messages = await _unitOfWork.Messages.GetConversationMessages(conversationId, limit, before, cancellationToken);

        return messages.Select(MapToMessageResponse);
    }

    // edit a message
    public async Task<MessageResponse> EditMessage(
        int userId, 
        EditMessageRequest request, 
        CancellationToken cancellationToken = default)
    {
        var message = CheckMessage(await _unitOfWork.Messages.GetById(request.MessageId, cancellationToken), userId);

        message.EditContent(request.NewContent);

        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToMessageResponse(message);
    }

    // delete a message
    public async Task<MessageResponse> DeleteMessage(int userId, int messageId, CancellationToken cancellationToken = default)
    {
        var message = CheckMessage(await _unitOfWork.Messages.GetById(messageId, cancellationToken), userId);

        // Delete the file if it exists
        if (!string.IsNullOrEmpty(message.StoredFileName))
        {
            await _fileStorageService.DeleteFileAsync(message.StoredFileName, cancellationToken);
        }

        message.SoftDelete();

        _unitOfWork.Messages.Update(message);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToMessageResponse(message);
    }

    // mark messages as seen
    public async Task MarkMessagesAsSeen(int userId, int conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        if (!conversation.HasParticipant(userId))
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        var unreadMessages = await _unitOfWork.Messages.GetUnreadMessagesInConversation(conversationId, userId, cancellationToken);
        foreach (var message in unreadMessages)
        {
            message.MarkAsSeen();
            _unitOfWork.Messages.Update(message);
        }

        conversation.ResetUnreadCountForUser(userId);
        _unitOfWork.Conversations.Update(conversation);

        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // check message authorization
    private static Message CheckMessage(Message? message, int userId)
    {
        if (message == null)
        {
            throw new KeyNotFoundException("Message not found.");
        }

        if (!message.IsSender(userId))
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not the sender of this message.");
        }
        return message;
    }

    // get media files attached to a conversation
    public async Task<IEnumerable<ConversationMediaResponse>> GetConversationMedia(
        int userId, 
        int conversationId, 
        int limit = Constants.DefaultMediaLimit,
        DateTime? before = null, 
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, Constants.MaxMediaLimit);

        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        if (!conversation.HasParticipant(userId))
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        var mediaMessages = await _unitOfWork.Messages.GetConversationMedia(conversationId, limit, before, cancellationToken);

        return mediaMessages.Select(m => new ConversationMediaResponse
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Username ?? string.Empty,
            StoredFileName = m.StoredFileName!,
            OriginalFileName = m.OriginalFileName ?? string.Empty,
            FileSize = m.FileSize ?? 0,
            ContentType = m.ContentType ?? string.Empty,
            FileUrl = $"/api/media/download/{m.StoredFileName}",
            SentAt = m.SentAt
        });
    }

    private static MessageResponse MapToMessageResponse(Message message)
    {
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.Username ?? string.Empty,
            Content = message.Content,
            MessageType = message.Type.ToString(),
            SentAt = message.SentAt,
            IsEdited = message.IsEdited,
            ParentMessageId = message.ParentMessageId,
            StoredFileName = message.StoredFileName,
            OriginalFileName = message.OriginalFileName,
            FileSize = message.FileSize,
            ContentType = message.ContentType,
            FileUrl = string.IsNullOrEmpty(message.StoredFileName) ? null : $"/api/media/download/{message.StoredFileName}",
            IsDelivered = message.IsDelivered,
            DeliveredAt = message.DeliveredAt,
            IsSeen = message.IsSeen,
            SeenAt = message.SeenAt,
            IsDeleted = message.IsDeleted
        };
    }
}