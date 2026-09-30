using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SimpleAccount.Gateway.Auth;

namespace SimpleAccount.Gateway.Endpoints;

public record DemoUser(string Subject, string Email, string FirstName, string LastName);

/// <summary>
/// Stands in for the Google handshake when no credentials are configured, so a fresh clone
/// runs with no setup. Registered only when <see cref="AuthMode.DevSignInEnabled"/> is true,
/// which requires a non-Production environment AND no Google credentials.
///
/// It deliberately runs the same <see cref="AccountProvisioner"/> and cookie sign-in as the
/// real OIDC callback, so everything downstream of the token exchange is the tested path.
/// </summary>
public static class DevAuthEndpoints
{
    public static readonly DemoUser[] DemoUsers =
    [
        new("dev-ada",    "ada@example.com",    "Ada",    "Lovelace"),
        new("dev-grace",  "grace@example.com",  "Grace",  "Hopper"),
        new("dev-alan",   "alan@example.com",   "Alan",   "Turing"),
    ];

    public static void MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/bff/dev-login");

        dev.MapGet("/", (string? returnUrl) => Results.Content(RenderPage(returnUrl), "text/html"));

        dev.MapGet("/signin", async (
            HttpContext http,
            AccountProvisioner provisioner,
            string sub,
            string email,
            string? firstName,
            string? lastName,
            string? returnUrl) =>
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, sub),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.GivenName, firstName ?? ""),
                    new Claim(ClaimTypes.Surname, lastName ?? ""),
                    new Claim("name", $"{firstName} {lastName}".Trim()),
                    new Claim("email_verified", "true"),
                ],
                authenticationType: "DevSignIn",
                nameType: "name",
                roleType: ClaimTypes.Role));

            await provisioner.ProvisionAsync(principal, http.RequestAborted);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            var target = returnUrl is not null && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : "/";
            return Results.Redirect(target);
        });
    }

    private static string RenderPage(string? returnUrl)
    {
        var suffix = returnUrl is null ? "" : $"&returnUrl={WebUtility.UrlEncode(returnUrl)}";

        var buttons = string.Join("\n", DemoUsers.Select(u => $"""
              <a class="user" href="/bff/dev-login/signin?sub={WebUtility.UrlEncode(u.Subject)}&email={WebUtility.UrlEncode(u.Email)}&firstName={WebUtility.UrlEncode(u.FirstName)}&lastName={WebUtility.UrlEncode(u.LastName)}{suffix}">
                <strong>{WebUtility.HtmlEncode($"{u.FirstName} {u.LastName}")}</strong>
                <span>{WebUtility.HtmlEncode(u.Email)}</span>
              </a>
        """));

        return $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Dev sign-in</title>
          <style>
            :root { color-scheme: light dark; --bg:#fbfbfd; --surface:#fff; --border:#e3e3e8;
                    --text:#1d1d20; --muted:#6b6b76; --accent:#3b5bdb; }
            @media (prefers-color-scheme: dark) {
              :root { --bg:#131316; --surface:#1c1c21; --border:#2e2e36;
                      --text:#ececf1; --muted:#9b9ba6; --accent:#748ffc; }
            }
            * { box-sizing: border-box; }
            body { margin:0; background:var(--bg); color:var(--text);
                   font:16px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif;
                   display:flex; align-items:center; justify-content:center; min-height:100vh; padding:1rem; }
            main { width:100%; max-width:26rem; background:var(--surface);
                   border:1px solid var(--border); border-radius:12px; padding:1.5rem; }
            h1 { font-size:1.2rem; margin:0 0 .25rem; }
            p.lede { color:var(--muted); margin:0 0 1.25rem; }
            .user { display:flex; flex-direction:column; gap:.1rem; padding:.7rem .9rem;
                    border:1px solid var(--border); border-radius:8px; margin-bottom:.6rem;
                    text-decoration:none; color:inherit; }
            .user:hover { border-color:var(--accent); }
            .user span { color:var(--muted); font-size:.9rem; }
            .note { margin-top:1.25rem; padding-top:1rem; border-top:1px solid var(--border);
                    color:var(--muted); font-size:.85rem; }
            code { background:var(--bg); padding:.1rem .3rem; border-radius:4px; }
          </style>
        </head>
        <body>
          <main>
            <h1>Dev sign-in</h1>
            <p class="lede">No Google credentials configured, so pick a demo user.</p>
        {{buttons}}
            <p class="note">
              This page exists only because <code>Google:ClientId</code> is unset and the
              environment is not Production. Set real credentials to get the Google flow instead.
            </p>
          </main>
        </body>
        </html>
        """;
    }
}
