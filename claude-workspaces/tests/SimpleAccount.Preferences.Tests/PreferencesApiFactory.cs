using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace SimpleAccount.Preferences.Tests;

/// <summary>
/// Boots the real service against a throwaway Postgres container so the seeded catalog and
/// the (user_id, rank) unique index are exercised for real.
/// </summary>
public class PreferencesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    // Explicit implementation: xunit v2's IAsyncLifetime returns Task, which would otherwise
    // clash with WebApplicationFactory's ValueTask-returning DisposeAsync.
    async Task IAsyncLifetime.InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:prefsdb", _postgres.GetConnectionString());
        builder.UseSetting("InternalAuth:SigningKey", TestTokens.SigningKey);
    }
}
