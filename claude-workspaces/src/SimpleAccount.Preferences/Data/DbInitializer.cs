using Microsoft.EntityFrameworkCore;

namespace SimpleAccount.Preferences.Data;

/// <summary>
/// Applies pending migrations before the host starts serving. Implemented as IHostedService
/// rather than BackgroundService on purpose: BackgroundService.ExecuteAsync does not block
/// startup, so requests would race the migration and hit a missing table. Idempotent, so
/// every replica can run it.
/// </summary>
public class DbInitializer(
    IServiceProvider services,
    ILogger<DbInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PreferencesDbContext>();

        logger.LogInformation("Applying Preferences migrations");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Preferences migrations applied");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
