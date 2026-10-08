using Application.Common.Interfaces.Persistence.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Common.Constants;

namespace Infrastructure.Persistence.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly ApplicationDbContext _context;

    public MessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetById(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.ParentMessage)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    // retrieve messages from a conversation
    public async Task<IEnumerable<Message>> GetConversationMessages(
        int conversationId, 
        int limit = Constants.MaxMessageLimit, 
        DateTime? before = null, // This is for retrieving messages that are older than the limit
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.ParentMessage)
            .Where(m => m.ConversationId == conversationId);

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // add a message
    public async Task Add(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    // update a message
    public void Update(Message message)
    {
        _context.Messages.Update(message);
    }

    // get a message by its stored file name
    public async Task<Message?> GetByStoredFileName(string storedFileName, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.StoredFileName == storedFileName, cancellationToken);
    }

    // get unread messages sent by others in a conversation
    public async Task<IEnumerable<Message>> GetUnreadMessagesInConversation(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsSeen)
            .ToListAsync(cancellationToken);
    }

    // gets the media files sent in a conversation
    public async Task<IEnumerable<Message>> GetConversationMedia(
        int conversationId, 
        int limit = 25, 
        DateTime? before = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId 
                     && !m.IsDeleted 
                     && m.StoredFileName != null 
                     && m.StoredFileName != "");

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}