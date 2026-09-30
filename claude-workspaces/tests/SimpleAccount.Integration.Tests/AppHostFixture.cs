using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace SimpleAccount.Integration.Tests;

/// <summary>
/// Boots the whole solution the way `aspire run` does: Postgres, both services, the gateway
/// and the SPA. Exercises the gateway's cookie/JWT exchange end to end.
/// </summary>
public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Ephemeral database: without this the test host would attach the developer's named
        // Postgres volume and write these fixture users into the dev database.
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SimpleAccount_AppHost>(["--UseEphemeralDatabase=true"]);

        // No Google credentials are configured, so the gateway serves its dev sign-in
        // endpoints — exactly what a fresh clone gets.

        App = await builder.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway")
            .WaitAsync(TimeSpan.FromMinutes(5));
    }

    public async Task DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }

    /// <summary>
    /// A browser-like client: follows redirects and keeps cookies, over HTTPS because the
    /// session cookie uses the __Host- prefix and so requires a secure origin.
    /// </summary>
    public HttpClient CreateBrowser()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        return new HttpClient(handler) { BaseAddress = App.GetEndpoint("gateway", "https") };
    }
}
