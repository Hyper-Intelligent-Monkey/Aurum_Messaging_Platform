namespace Application.DTOs.Auth;

public class AuthenticationResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool RequiresUsername { get; set; }
    public bool HasPassword { get; set; }
}