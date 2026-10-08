using Application.Common.Interfaces.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo> ValidateIdToken(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // configuration for google auth
            var clientId = _configuration["Google:ClientId"] ?? _configuration["GOOGLE_CLIENT_ID"];

            var settings = new GoogleJsonWebSignature.ValidationSettings();

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                settings.Audience = [clientId];
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            // returns the user info in the id token
            return new GoogleUserInfo
            {
                Email = payload.Email,
                Name = !string.IsNullOrEmpty(payload.Name) ? payload.Name : payload.Email.Split('@')[0],
                Picture = payload.Picture ?? string.Empty
            };
        } catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException("Invalid Google token.", ex);
        } catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Google authentication failed.", ex);
        }
    }



}