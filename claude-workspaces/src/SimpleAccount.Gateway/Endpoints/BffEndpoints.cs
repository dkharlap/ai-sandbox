using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using SimpleAccount.Contracts;
using SimpleAccount.Gateway.Auth;

namespace SimpleAccount.Gateway.Endpoints;

public record SessionDto(Guid UserId, string? Email, string? Name);

/// <summary>Anonymous, so the signed-out SPA can label its sign-in button correctly.</summary>
public record AuthConfigDto(bool DevMode);

public static class BffEndpoints
{
    public static void MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        var bff = app.MapGroup("/bff");

        bff.MapGet("/config", (AuthMode authMode) => Results.Ok(new AuthConfigDto(authMode.DevSignInEnabled)));

        // The only endpoint that starts a sign-in. With Google configured it challenges OIDC;
        // otherwise it hands off to the dev sign-in page. Everything else returns 401.
        bff.MapGet("/login", (AuthMode authMode, string? returnUrl) =>
        {
            var target = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : "/";

            if (!authMode.GoogleConfigured)
            {
                return Results.Redirect($"/bff/dev-login?returnUrl={Uri.EscapeDataString(target)}");
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = target },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        bff.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).RequireAuthorization();

        bff.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new SessionDto(
                Guid.Parse(user.FindFirstValue(ClaimNames.UserId)!),
                user.FindFirstValue(ClaimTypes.Email),
                user.FindFirstValue("name"))))
            .RequireAuthorization();
    }
}
