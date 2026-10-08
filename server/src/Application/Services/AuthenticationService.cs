using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitofWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthenticationService(
        IUnitofWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        IGoogleAuthService googleAuthService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _googleAuthService = googleAuthService;
    }

    // register a user
    public async Task<AuthenticationResponse> Register(
        RegisterUserRequest request, 
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = User.NormalizeUsername(request.Username);

        var existingUsername = await _unitOfWork.Users.GetByUsername(normalizedUsername, cancellationToken);
        if (existingUsername != null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var existingEmail = await _unitOfWork.Users.GetByEmail(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            throw new InvalidOperationException("Email is already used.");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(normalizedUsername, request.Email, hashedPassword);

        var confirmationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.SetEmailConfirmationToken(confirmationToken, DateTime.UtcNow.AddHours(24));

        await _unitOfWork.Users.Add(user, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
        
        var serverBaseUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5067";
        var confirmationLink = $"{serverBaseUrl}/api/auth/confirm-email?userId={user.Id}&token={confirmationToken}";


        await _emailService.SendEmailConfirmation(user.Email, user.Username, confirmationLink, cancellationToken);

        return new AuthenticationResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            Token = string.Empty, // prevents log in after sign up
            HasPassword = true
        };
    }

    // log in a user
    public async Task<AuthenticationResponse> Login(
        LoginUserRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmail(request.Email, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (string.IsNullOrEmpty(user.HashedPassword))
        {
            throw new UnauthorizedAccessException("Please log in using Google.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.HashedPassword);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new UnauthorizedAccessException("Please confirm your email address.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthenticationResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            Token = token,
            HasPassword = !string.IsNullOrEmpty(user.HashedPassword)
        };
    }

    // log in a user using Google OAuth
    public async Task<AuthenticationResponse> GoogleLogin(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleAuthService.ValidateIdToken(request.IdToken, cancellationToken);

        var user = await _unitOfWork.Users.GetByEmail(googleUser.Email, cancellationToken);
        if (user != null)
        {
            var modified = false;

            if (!user.IsEmailConfirmed)
            {
                user.ConfirmEmail();
                modified = true;
            }

            if (string.IsNullOrEmpty(user.Avatar) && !string.IsNullOrEmpty(googleUser.Picture))
            {
                user.UpdateAvatar(googleUser.Picture);
                modified = true;
            }

            if (modified)
            {
                await _unitOfWork.SaveChanges(cancellationToken);
            }

            var token = _jwtTokenGenerator.GenerateToken(user);
            return new AuthenticationResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Avatar = user.Avatar,
                Token = token,
                RequiresUsername = false,
                HasPassword = !string.IsNullOrEmpty(user.HashedPassword)
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var chosenUsername = request.Username.Trim().Trim('_');
            if (chosenUsername.Length < 3 || chosenUsername.Length > 20)
            {
                throw new InvalidOperationException("Username must be between 3 and 20 characters.");
            }

            if (!Regex.IsMatch(chosenUsername, "^[a-zA-Z0-9_]+$"))
            {
                throw new InvalidOperationException("Username can only contain letters, numbers and underscores.");
            }

            var existingWithUsername = await _unitOfWork.Users.GetByUsername(chosenUsername, cancellationToken);
            if (existingWithUsername != null)
            {
                throw new InvalidOperationException("Username is already taken.");
            }

            var newUser = User.CreateWithGoogle(
                chosenUsername, 
                googleUser.Email, 
                string.IsNullOrEmpty(googleUser.Picture) ? null : googleUser.Picture);

            await _unitOfWork.Users.Add(newUser, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            var token = _jwtTokenGenerator.GenerateToken(newUser);

            return new AuthenticationResponse
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Avatar = newUser.Avatar,
                Token = token,
                RequiresUsername = false,
                HasPassword = false
            };
        }

        // prompt new user to confirm or customize their username when registering with Google
        var sanitizedName = SanitizeUsername(googleUser.Name, googleUser.Email);

        return new AuthenticationResponse
        {
            Id = 0,
            Username = sanitizedName,
            Email = googleUser.Email,
            Avatar = googleUser.Picture,
            Token = string.Empty,
            RequiresUsername = true,
            HasPassword = false
        };
    }

    // confirm email address of new user
    public async Task ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetById(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (user.IsEmailConfirmed) return;


        if (string.IsNullOrWhiteSpace(user.EmailConfirmationToken) || user.EmailConfirmationToken != request.Token)
        {
            throw new InvalidOperationException("Invalid confirmation token.");
        }

        if (user.EmailConfirmationExpiresAt.HasValue && user.EmailConfirmationExpiresAt.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Confirmation link has expired.");
        }

        user.ConfirmEmail();
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // request password reset code
    public async Task ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmail(request.Email, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("Email is not registered.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new InvalidOperationException("Confirm your account in your email inbox.");
        }


        var otp = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        user.SetPasswordResetOtp(otp, DateTime.UtcNow.AddMinutes(5));

        await _unitOfWork.SaveChanges(cancellationToken);

        await _emailService.SendPasswordResetOtp(user.Email, user.Username, otp, cancellationToken);
    }

    // reset password
    public async Task ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmail(request.Email, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetOtp) || user.PasswordResetOtp != request.Otp)
        {
            throw new InvalidOperationException("Invalid verification code.");
        }

        if (user.PasswordResetExpiresAt.HasValue && user.PasswordResetExpiresAt.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Verification code has expired.");
        }
        
        var newHashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(newHashedPassword);
        user.ClearPasswordResetOtp();

        await _unitOfWork.SaveChanges(cancellationToken);
    }

    // helper for sanitizing usernames
    private static string SanitizeUsername(string name, string email)
    {
        var cleaned = Regex.Replace(name ?? string.Empty, @"[^a-zA-Z0-9_]", "").Trim('_');

        if (cleaned.Length < 3)
        {
            cleaned = Regex.Replace(email.Split('@')[0], @"[^a-zA-Z0-9_]", "").Trim('_');
        }

        if (cleaned.Length < 3)
        {
            cleaned = "user";
        }

        if (cleaned.Length > 20)
        {
            cleaned = cleaned.Substring(0, 20).TrimEnd('_');
        }

        return cleaned;
    }
}