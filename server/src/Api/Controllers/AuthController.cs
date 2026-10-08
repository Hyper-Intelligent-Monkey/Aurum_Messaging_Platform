using Application.Common.Interfaces.Services;
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // registers a new user with data from the request body
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>> Register(
        [FromBody] RegisterUserRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.Register(request, cancellationToken);
        return Ok(response);
    }

    // logs in an existing user with data from the request body
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        [FromBody] LoginUserRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.Login(request, cancellationToken);
        return Ok(response);
    }

    // confirms an email address for a new user from the query parameters
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] int userId,
        [FromQuery] string token,
        CancellationToken cancellationToken
    )
    {
        var clientBaseUrl = Environment.GetEnvironmentVariable("CLIENT_URL") ?? "http://localhost:5173";
        try
        {
            await _authenticationService.ConfirmEmail(new ConfirmEmailRequest { UserId = userId, Token = token }, cancellationToken);
            return Redirect($"{clientBaseUrl}/login?confirmed=true");
        }
        catch
        {
            return Redirect($"{clientBaseUrl}/login?confirmed=false");
        }
    }

    // requests a password reset code for an existing user from the request body
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        await _authenticationService.ForgotPassword(request, cancellationToken);
        return Ok(new {message = "A 6-digit code has been sent to your email"});
    }

    // resets a user's password with the 6-digit code from the request body
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        await _authenticationService.ResetPassword(request, cancellationToken);
        return Ok(new {message = "Password reset successfully"});
    }
    
    // logs in or registers using Google OAuth
    [HttpPost("google")]
    public async Task<ActionResult<AuthenticationResponse>> GoogleLogin(
        [FromBody] GoogleLoginRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.GoogleLogin(request, cancellationToken);
        return Ok(response);
    }
}