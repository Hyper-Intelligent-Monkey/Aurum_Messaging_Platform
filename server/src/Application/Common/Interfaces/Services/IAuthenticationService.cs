using Application.DTOs.Auth;

namespace Application.Common.Interfaces.Services;

public interface IAuthenticationService
{
    // register a user
    Task<AuthenticationResponse> Register(RegisterUserRequest request, CancellationToken cancellationToken = default);
    // log in a user
    Task<AuthenticationResponse> Login(LoginUserRequest request, CancellationToken cancellationToken = default);
    // log in a user using Google OAuth
    Task<AuthenticationResponse> GoogleLogin(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    // confirm email address of new user
    Task ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken = default);
    // request password reset code
    Task ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    // reset password
    Task ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}