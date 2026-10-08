namespace Application.Common.Interfaces.Security;

public class GoogleUserInfo
{
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Picture { get; set; } = null!;
}

public interface IGoogleAuthService
{
    // validate the id token and return the user info, when using Google
    Task<GoogleUserInfo> ValidateIdToken(string idToken, CancellationToken cancellationToken = default);
}