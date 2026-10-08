using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class UnconfirmedUserCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnconfirmedUserCleanupService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1); // check every hour

    public UnconfirmedUserCleanupService(
        IServiceProvider serviceProvider, 
        ILogger<UnconfirmedUserCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // runs every hour to check for unconfirmed users older than 24 hours to delete
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var cutoff = DateTime.UtcNow.AddHours(-24);

                var expiredUsers = await dbContext.Users
                    .Where(u => !u.IsEmailConfirmed && u.CreatedAt < cutoff)
                    .ToListAsync(stoppingToken);

                var Count = expiredUsers.Count;

                if (Count > 0)
                {
                    _logger.LogInformation("Purging {Count} unconfirmed user accounts older than 24 hours.", Count);
                    dbContext.Users.RemoveRange(expiredUsers);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up unconfirmed users.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }


}