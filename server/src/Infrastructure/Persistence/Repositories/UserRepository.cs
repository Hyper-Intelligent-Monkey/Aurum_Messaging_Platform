using Application.Common.Interfaces.Persistence.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Common.Constants;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetById(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }
    public async Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return await _context.Users.AnyAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public async Task Add(User user, CancellationToken cancellation = default)
    {
        await _context.Users.AddAsync(user, cancellation);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }

    // search for uncontacted users
    public async Task<IEnumerable<User>> SearchUsers(int currentUserId, string query, int limit = 10, int offset = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<User>();

        var searchTerm = query.Trim().ToLower();
        limit = Math.Clamp(limit, 1, 50);
        offset = Math.Max(0, offset);

        // IDs of conversations that currentUserId already has a conversation with
        var existingConversationIds = _context.ConversationParticipants
            .Where(p => p.UserId == currentUserId)
            .Select(p => p.ConversationId);
        
        // IDs of users that currentUserId already has a conversation with
        var contactedUserIds = _context.ConversationParticipants
            .Where(p => existingConversationIds.Contains(p.ConversationId) && p.UserId != currentUserId)
            .Select(p => p.UserId);

        return await _context.Users
            .Where(u => u.IsEmailConfirmed && u.Id != currentUserId && !contactedUserIds.Contains(u.Id))
            .Where(u => u.Username.ToLower().Contains(searchTerm))
            .OrderBy(u => u.Username)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // blocks a user
    public async Task AddBlock(UserBlock block, CancellationToken cancellationToken = default)
    {
        await _context.UserBlocks.AddAsync(block, cancellationToken);
    }

    public void DeleteBlock(UserBlock block)
    {
        _context.UserBlocks.Remove(block);
    }

    // gets a blocked user
    public async Task<UserBlock?> GetBlock(int blockerId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId, cancellationToken);
    }

    // checks if a user is blocked
    public async Task<bool> IsBlocked(int blockerId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.UserBlocks
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId, cancellationToken);
    }

    // gets all blocked users
    public async Task<IEnumerable<User>> GetBlockedUsers(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserBlocks
            .Where(b => b.BlockerId == userId)
            .Include(b => b.BlockedUser)
            .Select(b => b.BlockedUser)
            .ToListAsync(cancellationToken);
    }
}

