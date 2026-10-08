using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.DTOs.User;
using Domain.Entities;
using Application.Common.Constants;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUnitofWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    
    public UserService( IUnitofWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    private static User CheckForUser(User? user)
    {
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }
        return user;
    }

    // Get user by ID
    public async Task<UserResponse> GetById(int userId, CancellationToken cancellationToken = default)
    {
        var user = CheckForUser(await _unitOfWork.Users.GetById(userId, cancellationToken));

        return MapToUserResponse(user);
    }

    // Update user avatar
    public async Task<UserResponse> UpdateAvatar(
        int userId, 
        UpdateAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = CheckForUser(await _unitOfWork.Users.GetById(userId, cancellationToken));
        
        user.UpdateAvatar(string.IsNullOrWhiteSpace(request.StoredFileName) ? null : request.StoredFileName);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToUserResponse(user);
    }

    // Set initial password for user without a password (e.g., Google OAuth registered)
    public async Task SetPassword(
        int userId, 
        SetPasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = CheckForUser(await _unitOfWork.Users.GetById(userId, cancellationToken));

        if (!string.IsNullOrEmpty(user.HashedPassword))
        {
            throw new InvalidOperationException("Please use change password.");
        }

        var newHashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(newHashedPassword);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // Change user password
    public async Task ChangePassword(
        int userId, 
        ChangePasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = CheckForUser(await _unitOfWork.Users.GetById(userId, cancellationToken));

        if (string.IsNullOrEmpty(user.HashedPassword))
        {
            throw new InvalidOperationException("Please set a password first.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.HashedPassword);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Invalid current password.");
        }

        var newHashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(newHashedPassword);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // Search users (returns ONLY uncontacted users for current user)
    public async Task<IEnumerable<UserResponse>> SearchUsers(int currentUserId, string query, int limit = 10, int offset = 0, CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.SearchUsers(currentUserId, query, limit, offset, cancellationToken);

        return users.Select(u => {
            var response = MapToUserResponse(u);
            response.IsOnline = false;
            response.LastSeen = null;
            return response;
        });
    }


    // Block user
    public async Task BlockUser(int blockerId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        if (blockerId == blockedUserId)
        {
            throw new InvalidOperationException("You cannot block yourself.");
        }

        var blockedUser = await _unitOfWork.Users.GetById(blockedUserId, cancellationToken);
        if (blockedUser == null)
        {
            throw new KeyNotFoundException("User to block was not found.");
        }

        var isAlreadyBlocked = await _unitOfWork.Users.IsBlocked(blockerId, blockedUserId, cancellationToken);
        if (isAlreadyBlocked)
        {
            return;
        }

        var block = UserBlock.Create(blockerId, blockedUserId);
        await _unitOfWork.Users.AddBlock(block, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // Unblock user
    public async Task UnblockUser(int blockerId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        var block = await _unitOfWork.Users.GetBlock(blockerId, blockedUserId, cancellationToken);
        if (block != null)
        {
            _unitOfWork.Users.DeleteBlock(block);
            await _unitOfWork.SaveChanges(cancellationToken);
        }
    }

    // Get blocked users
    public async Task<IEnumerable<UserResponse>> GetBlockedUsers(int userId, CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.GetBlockedUsers(userId, cancellationToken);
        return users.Select(MapToUserResponse);
    }

    // Get block status between current user and target user
    public async Task<BlockStatusResponse> GetBlockStatus(int currentUserId, int targetUserId, CancellationToken cancellationToken = default)
    {
        var isBlockedByMe = await _unitOfWork.Users.IsBlocked(currentUserId, targetUserId, cancellationToken);
        var isBlockedByThem = await _unitOfWork.Users.IsBlocked(targetUserId, currentUserId, cancellationToken);

        return new BlockStatusResponse
        {
            IsBlockedByMe = isBlockedByMe,
            IsBlockedByThem = isBlockedByThem
        };
    }

    // Set user online
    public async Task<UserResponse?> SetUserOnline(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetById(userId, cancellationToken);
        if (user == null) return null;

        user.GoOnline();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToUserResponse(user);
    }

    // Set user offline
    public async Task<UserResponse?> SetUserOffline(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetById(userId, cancellationToken);
        if (user == null) return null;

        user.GoOffline();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChanges(cancellationToken);

        return MapToUserResponse(user);
    }

    // Map to user response
    private static UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen,
            HasPassword = !string.IsNullOrEmpty(user.HashedPassword)
        };
    }

}