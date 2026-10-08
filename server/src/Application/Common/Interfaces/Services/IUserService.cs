using Application.DTOs.User;

namespace Application.Common.Interfaces.Services;

public interface IUserService
{
    // Get user by ID
    Task<UserResponse> GetById(int userId, CancellationToken cancellationToken = default);
    // Update user avatar
    Task<UserResponse> UpdateAvatar(int userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default);
    // Set initial password for user without a password (e.g., Google OAuth registered)
    Task SetPassword(int userId, SetPasswordRequest request, CancellationToken cancellationToken = default);
    // Change user password
    Task ChangePassword(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    // Search uncontacted users
    Task<IEnumerable<UserResponse>> SearchUsers(int currentUserId, string query, int limit = 10, int offset = 0, CancellationToken cancellationToken = default);
    // Blocks a user
    Task BlockUser(int blockerId, int blockedUserId, CancellationToken cancellationToken = default);
    // Unblocks a user
    Task UnblockUser(int blockerId, int blockedUserId, CancellationToken cancellationToken = default);
    // Get blocked users
    Task<IEnumerable<UserResponse>> GetBlockedUsers(int userId, CancellationToken cancellationToken = default);
    // Get block status between current user and target user
    Task<BlockStatusResponse> GetBlockStatus(int currentUserId, int targetUserId, CancellationToken cancellationToken = default);
    // Set user online
    Task<UserResponse?> SetUserOnline(int userId, CancellationToken cancellationToken = default);
    // Set user offline
    Task<UserResponse?> SetUserOffline(int userId, CancellationToken cancellationToken = default);
}