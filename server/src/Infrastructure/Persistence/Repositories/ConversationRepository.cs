using Application.Common.Interfaces.Persistence.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Common.Constants;

namespace Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get a conversation by ID
    public async Task<Conversation?> GetById(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    // Get all conversations of a user
    public async Task<IEnumerable<Conversation>> GetUserConversations(int userId, int limit = 30, DateTime? before = null, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, Constants.MaxConversationLimit);
       var query = _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
            .Where(c => c.Participants.Any(p => p.UserId == userId));

        if (before.HasValue)
        {
            query = query.Where(c => c.LastMessageAt < before.Value);
        }
        
        return await query
            .OrderByDescending(c => c.LastMessageAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // Find a direct conversation between 2 users
    public async Task<Conversation?> FindDirectConversation(int userId1, int userId2, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Count == 2 &&
                        c.Participants.Any(p => p.UserId == userId1) &&
                        c.Participants.Any(p => p.UserId == userId2))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Add a conversation
    public async Task Add(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
    }

    // Update a conversation
    public void Update(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
    }

    // Search for a contacted user
    public async Task<IEnumerable<Conversation>> SearchUserConversations(
        int userId, 
        string query, 
        int limit = 10,
        DateTime? before = null,
        CancellationToken cancellationToken = default
        )
    {
        limit = Math.Clamp(limit, 1, Constants.SearchLimit);
        if (string.IsNullOrWhiteSpace(query))
            return await GetUserConversations(userId, limit, before, cancellationToken);

        var searchTerm = query.Trim().ToLower();


        var searchQuery = _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.LastMessage)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Where(c => c.Participants.Any(p => p.UserId != userId && 
                                            p.User.Username.ToLower().Contains(searchTerm)));
        
        if (before.HasValue)
        {
            searchQuery = searchQuery.Where(c => c.LastMessageAt < before.Value);
        }
        
        return await searchQuery
            .OrderByDescending(c => c.LastMessageAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}