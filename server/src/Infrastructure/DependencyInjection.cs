using System.Text;
using System.Security.Claims;
using Application.Common.Interfaces.Persistence.Repositories;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Application.Common.Interfaces.Services;
using Infrastructure.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), 
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            )
        );

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = jwtSection.Get<JwtSettings>() 
            ?? throw new InvalidOperationException(string.Format("Configuration section '{0}' is missing.", JwtSettings.SectionName));

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException("JWT Secret is not configured.");
        }

        services.Configure<JwtSettings>(jwtSection);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            // validate the jwt contents
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };

            options.Events = new JwtBearerEvents
            {
                // validate the user in the token
                OnTokenValidated = async context =>
                {
                    // get the db context
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    // get the user id from the jwt token
                    var userIdStr = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out var userId))
                    {
                        var exists = await dbContext.Users.AnyAsync(u => u.Id == userId);
                        if (!exists)
                        {
                            context.Fail("User account no longer exists.");
                        }
                    }
                },
                // Extracts the JWT from URL query parameters for direct browser downloads
                // because 'Authorization' can not be sent in the headers during WebSocket handshakes
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    // Only allow query-string tokens for SignalR hubs (or components that use websockets) 
                    // and media download endpoints
                    if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/api/media/download")))
                    {
                        // Hand the token over to the JWT validator
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // repositories responsible for getting data from the database
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // centralized unit of work 
        services.AddScoped<IUnitofWork, UnitOfWork>();

        // file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // email settings for sending emails
        var emailSection = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(emailSection);
        services.AddScoped<IEmailService, EmailService>();

        // google auth service
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // cleaning unconfirmed user accounts
        services.AddHostedService<UnconfirmedUserCleanupService>();

        return services;
    }
}