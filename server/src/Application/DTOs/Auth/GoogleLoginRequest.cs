namespace Application.DTOs.Auth;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = null!;
    public string? Username { get; set; }
}