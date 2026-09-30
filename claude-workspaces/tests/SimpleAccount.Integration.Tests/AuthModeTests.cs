using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SimpleAccount.Gateway.Auth;

namespace SimpleAccount.Integration.Tests;

/// <summary>
/// Dev sign-in bypasses the identity provider, so the conditions that enable it are the
/// security boundary and are pinned here.
/// </summary>
public class AuthModeTests
{
    private sealed class Env(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static AuthMode Create(string environment, bool withGoogle)
    {
        var settings = withGoogle
            ? new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "id",
                ["Authentication:Google:ClientSecret"] = "secret",
            }
            : [];

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AuthMode(configuration, new Env(environment));
    }

    [Fact]
    public void Dev_signin_is_enabled_with_no_google_credentials_in_development()
    {
        var mode = Create(Environments.Development, withGoogle: false);

        Assert.True(mode.DevSignInEnabled);
        Assert.False(mode.GoogleConfigured);
    }

    [Fact]
    public void Dev_signin_is_disabled_in_production_even_without_google_credentials()
    {
        // The important one: a misconfigured production deploy must fail closed rather than
        // exposing a password-less sign-in page.
        var mode = Create(Environments.Production, withGoogle: false);

        Assert.False(mode.DevSignInEnabled);
    }

    [Fact]
    public void Google_takes_precedence_over_dev_signin_when_configured()
    {
        var mode = Create(Environments.Development, withGoogle: true);

        Assert.True(mode.GoogleConfigured);
        Assert.False(mode.DevSignInEnabled);
    }

    [Theory]
    [InlineData("id", "")]
    [InlineData("", "secret")]
    [InlineData("  ", "  ")]
    public void A_half_configured_google_client_does_not_count_as_configured(string id, string secret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = id,
            ["Authentication:Google:ClientSecret"] = secret,
        }).Build();

        Assert.False(new AuthMode(configuration, new Env(Environments.Development)).GoogleConfigured);
    }
}
