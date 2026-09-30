namespace SimpleAccount.Gateway.Auth;

/// <summary>
/// Decides whether real Google sign-in is available. With no credentials configured the
/// gateway falls back to its built-in dev sign-in page, so a fresh clone runs unconfigured.
/// </summary>
public class AuthMode(IConfiguration configuration, IHostEnvironment environment)
{
    public bool GoogleConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);

    /// <summary>
    /// Dev sign-in is available only outside Production, and only when Google is not set up.
    /// Both conditions must hold, so it can never become a bypass in a deployed environment.
    /// </summary>
    public bool DevSignInEnabled => !environment.IsProduction() && !GoogleConfigured;
}
