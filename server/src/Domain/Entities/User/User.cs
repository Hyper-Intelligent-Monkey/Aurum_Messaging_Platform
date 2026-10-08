using System.Text.RegularExpressions;
using Domain.Exceptions;

namespace Domain.Entities;

public class User
{
    private readonly List<ConversationParticipant> _conversations = new();

    private User() { }

    public int Id { get; private set; }

    // Personal Details
    public string Username { get; private set; } = null!;
    public string NormalizedUsername { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? HashedPassword { get; private set; }
    public string? Avatar { get; private set; } = null;

    // Personal storage for asset (avatar only)
    public string? StorageFolder { get; private set; }
    
    // Online Status
    public bool IsOnline { get; private set; } = false;
    public DateTime LastSeen { get; private set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Google Auth
    public bool IsEmailConfirmed { get; private set; } = false;
    public string? EmailConfirmationToken { get; private set; }
    public DateTime? EmailConfirmationExpiresAt { get; private set; }

    // Password Reset
    public string? PasswordResetOtp { get; private set; }
    public DateTime? PasswordResetExpiresAt { get; private set; }

    public virtual IReadOnlyCollection<ConversationParticipant> Conversations => _conversations.AsReadOnly();

    public static User Create(string username, string email, string password) // Factory method
    {
        var normalizedUsername = NormalizeUsername(username);
        ValidateEmail(email);
        ValidatePassword(password);

        var user = new User
        {
            Username = normalizedUsername,
            NormalizedUsername = normalizedUsername.ToUpperInvariant(),
            Email = email,
            HashedPassword = password,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            IsOnline = false
        };

        user.SetStorageFolder();
        return user;
    }

    // Helper for ownership checks, not currently used but who knows when it has to shine
    public bool IsSameUser(int userId) => Id !=  0 && Id == userId;

    // Password Management
    public void SetPassword(string newPassword)
    {
        ValidatePassword(newPassword);
        
        HashedPassword = newPassword;
    }

    public void UpdateAvatar(string? avatarPath)
    {
        Avatar = avatarPath;
    }

    public void GoOnline() => IsOnline = true;

    public void GoOffline()
    {
        IsOnline = false;
        LastSeen = DateTime.UtcNow;
    }

    // Set a personal storage folder for user assets (avatar only in this system)
    private void SetStorageFolder()
    {
        if (string.IsNullOrEmpty(StorageFolder))
        {
            StorageFolder = $"users/user_{Guid.NewGuid():N}";
        }
    }

    // Normalize username to follow standards
    public static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username is required.");

        var normalized = username.Trim().Trim('_');

        if (normalized.Length < 3)
            throw new DomainException("Username is too short (min 3 characters).");
            
        if (normalized.Length > 20)
            throw new DomainException("Username is too long (max 20 characters).");

        if (!Regex.IsMatch(normalized, "^[a-zA-Z0-9_]+$"))
            throw new DomainException("Username can only contain letters, numbers, and underscores.");

        return normalized;
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
            throw new DomainException("Invalid email format.");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("Password is required.");

        if (password.Length < 8)
            throw new DomainException("Password is too short (min 8 characters).");
    }

    // with a confirmation token this prevents anyone from confirming your email address
    public void SetEmailConfirmationToken( string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Email confirmation token is required.");
        }
        EmailConfirmationToken = token;
        EmailConfirmationExpiresAt = expiresAt;
    }

    // After registration, confirm email address
    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
        EmailConfirmationToken = null;
        EmailConfirmationExpiresAt = null;
    }

    // code for password reset
    public void SetPasswordResetOtp(string otp, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(otp))
        {
            throw new DomainException("Password reset code cannot be empty.");
        }
        PasswordResetOtp = otp;
        PasswordResetExpiresAt = expiresAt;
    }

    // what name says
    public void ClearPasswordResetOtp()
    {
        PasswordResetOtp = null;
        PasswordResetExpiresAt = null;
    }

    // Alternative factory method for creating an account using Google Auth
    public static User CreateWithGoogle(string username, string email, string? avatar = null)
    {
        var normalizedUsername = NormalizeUsername(username);
        ValidateEmail(email);

        var user = new User
        {
            Username = normalizedUsername,
            NormalizedUsername = normalizedUsername.ToUpperInvariant(),
            Email = email,
            HashedPassword = null,
            Avatar = avatar,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            IsOnline = false
        };

        user.SetStorageFolder();
        return user;
    }
}

