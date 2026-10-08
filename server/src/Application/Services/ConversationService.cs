using Application.Common.Constants;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.DTOs.Conversation;
using Application.DTOs.Message;
using Domain.Entities;

namespace Application.Services;

public class ConversationService : IConversationService
{
    private readonly IUnitofWork _unitOfWork;

    public ConversationService(IUnitofWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // get contacted users
    public async Task<IEnumerable<ConversationResponse>> GetUserConversations(
        int userId,
        int limit = Constants.MaxConversationLimit,
        DateTime? before = null,
        CancellationToken cancellationToken = default
    )
    {
        limit = Math.Clamp(limit, 1, Constants.MaxConversationLimit);
        var conversations = await _unitOfWork.Conversations.GetUserConversations(userId, limit, before, cancellationToken); 

        return conversations.Select(c => MapToConversationResponse(c, userId));
    }

    // get a conversation
    public async Task<ConversationResponse> GetConversationById(
        int conversationId,
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        conversation.ResetUnreadCountForUser(userId);
        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToConversationResponse(conversation, userId);
    }

    // search for contacted users
    public async Task<IEnumerable<ConversationResponse>> SearchUserConversations(
        int userId,
        string query,
        int limit = 10,
        DateTime? before = null,
        CancellationToken cancellationToken = default
    )
    {
        var conversations = await _unitOfWork.Conversations.SearchUserConversations(userId, query, limit, before, cancellationToken);
        return conversations.Select(c => MapToConversationResponse(c, userId));
    }

    // mute a conversation
    public async Task MuteConversation(
        int userId,
        int conversationId,
        int? durationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        if (durationMinutes.HasValue && durationMinutes.Value > 0)
        {
            participant.MuteForMinutes(durationMinutes.Value);
        }
        else
        {
            participant.MuteIndefinitely();
        }

        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // unmute a conversation
    public async Task UnmuteConversation(
        int userId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _unitOfWork.Conversations.GetById(conversationId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("Unauthorized: You are not a participant in this conversation.");
        }

        participant.Unmute();

        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    private static ConversationResponse MapToConversationResponse(Conversation conversation, int userId)
    {
        var currentParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);

        var participants = conversation.Participants.Select(p => new ParticipantResponse
        {
            UserId = p.UserId,
            Username = p.User.Username,
            Avatar = p.User.Avatar,
            IsOnline = p.User.IsOnline,
            LastSeen = p.User.LastSeen,
            IsDeleted = false
        }).ToList();

        if (participants.Count == 1)
        {
            participants.Add(new ParticipantResponse
            {
                UserId = 0,
                Username = "Deleted User",
                Avatar = null,
                IsOnline = false,
                LastSeen = null,
                IsDeleted = true
            });
        }

        var isCurrentlyMuted = currentParticipant != null && currentParticipant.IsMuted && (!currentParticipant.MutedUntil.HasValue || currentParticipant.MutedUntil.Value > DateTime.UtcNow);

        return new ConversationResponse
        {
            Id = conversation.Id,
            UnreadCount = currentParticipant?.UnreadCount ?? 0,
            IsMuted = isCurrentlyMuted,
            MutedUntil = isCurrentlyMuted ? currentParticipant?.MutedUntil : null,
            CreatedAt = conversation.CreatedAt,
            Participants = participants,
            LastMessage = conversation.LastMessage == null ? null : new MessageResponse
            {
                Id = conversation.LastMessage.Id,
                ConversationId = conversation.LastMessage.ConversationId,
                SenderId = conversation.LastMessage.SenderId,
                SenderName = conversation.LastMessage.Sender?.Username ?? "Deleted User",
                Content = conversation.LastMessage.Content,
                MessageType = conversation.LastMessage.Type.ToString(),
                SentAt = conversation.LastMessage.SentAt,
                IsEdited = conversation.LastMessage.IsEdited,
                ParentMessageId = conversation.LastMessage.ParentMessageId,
                OriginalFileName =  conversation.LastMessage.OriginalFileName,
                FileSize = conversation.LastMessage.FileSize,
                ContentType = conversation.LastMessage.ContentType,
                FileUrl = string.IsNullOrEmpty(conversation.LastMessage.StoredFileName) ? null : $"/api/media/download/{conversation.LastMessage.StoredFileName}"

            }
        };
    }
}