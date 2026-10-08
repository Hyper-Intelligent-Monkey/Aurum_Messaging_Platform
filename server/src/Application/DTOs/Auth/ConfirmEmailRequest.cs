namespace Application.DTOs.Auth;

public class ConfirmEmailRequest
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}