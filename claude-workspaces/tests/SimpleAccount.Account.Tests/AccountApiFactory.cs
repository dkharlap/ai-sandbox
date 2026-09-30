using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace SimpleAccount.Account.Tests;

/// <summary>
/// Boots the real service against a throwaway Postgres container, so migrations, citext and
/// the unique-index behaviour are all covered rather than mocked.
/// </summary>
public class AccountApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    // Explicit implementation: xunit v2's IAsyncLifetime returns Task, which would otherwise
    // clash with WebApplicationFactory's ValueTask-returning DisposeAsync.
    async Task IAsyncLifetime.InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:accountsdb", _postgres.GetConnectionString());
        builder.UseSetting("InternalAuth:SigningKey", TestTokens.SigningKey);
    }
}
