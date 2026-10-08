using Domain.Entities;

namespace Application.Common.Interfaces.Persistence.Repositories;

public interface IUserRepository
{
    // Get a user
    Task<User?> GetById(int id, CancellationToken cancellationToken = default);

    // Get user email
    Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default);
  
    // Get user username
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);
    
    // Check if email is used
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default);

    // Check if username is used
    Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default);

    // Add user
    Task Add(User user, CancellationToken cancellationToken = default);

    // Update user
    void Update(User user);

    // Delete user
    void Delete(User user);

    // Search for user to contact
    Task<IEnumerable<User>> SearchUsers(int currentUserId, string query, int limit = 10, int offset = 0, CancellationToken cancellationToken = default);

    // Blocking
    Task AddBlock(UserBlock block, CancellationToken cancellationToken = default);
    void DeleteBlock(UserBlock block);
    Task<UserBlock?> GetBlock(int blockerId, int blockedUserId, CancellationToken cancellationToken = default);
    Task<bool> IsBlocked(int blockerId, int blockedUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetBlockedUsers(int userId, CancellationToken cancellationToken = default);
}