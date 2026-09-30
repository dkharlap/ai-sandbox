using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SimpleAccount.Contracts;

namespace SimpleAccount.Gateway.Auth;

public static class GatewayAuthExtensions
{
    public static IHostApplicationBuilder AddGatewayAuth(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AuthMode>();
        builder.Services.AddSingleton<IInternalTokenIssuer, InternalTokenIssuer>();
        builder.Services.AddScoped<AccountProvisioner>();

        // Plain http for the internal hop: these endpoints are not browser-reachable, and
        // https+http would require every developer to trust the Aspire dev certificate.
        builder.Services.AddHttpClient(AccountProvisioner.HttpClientName, client =>
                client.BaseAddress = new Uri("http://account"))
            .AddStandardResilienceHandler();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // Deliberately NOT OpenIdConnect. With OIDC as the challenge scheme, an expired
                // session turns an SPA fetch into a 302 toward Google's HTML login page.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-simpleaccount.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // A challenge can now never become a redirect: XHR gets a status code.
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            ;

        builder.Services.AddAuthorization();

        var authMode = new AuthMode(builder.Configuration, builder.Environment);
        if (!authMode.GoogleConfigured)
        {
            // No credentials: the dev sign-in page stands in, and no OIDC handler is
            // registered at all, so /bff/login cannot challenge a scheme that isn't there.
            return builder;
        }

        builder.Services.AddAuthentication()
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration["Authentication:Google:Authority"]
                    ?? "https://accounts.google.com";
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;

                // query, not the form_post default: form_post forces the correlation and nonce
                // cookies to SameSite=None, a needless third-party-cookie dependency.
                options.ResponseMode = OpenIdConnectResponseMode.Query;

                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");

                options.MapInboundClaims = true;
                options.TokenValidationParameters.NameClaimType = "name";

                options.Events.OnTicketReceived = async ctx =>
                {
                    if (ctx.Principal?.FindFirstValue("email_verified") is not "true")
                    {
                        ctx.Fail("Google account email is not verified.");
                        return;
                    }

                    var provisioner = ctx.HttpContext.RequestServices.GetRequiredService<AccountProvisioner>();
                    await provisioner.ProvisionAsync(ctx.Principal, ctx.HttpContext.RequestAborted);
                };

                // Fail closed: if provisioning fails the user does not get a session.
                options.Events.OnRemoteFailure = ctx =>
                {
                    var logger = ctx.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.Auth");
                    logger.LogWarning(ctx.Failure, "Google sign-in failed");

                    ctx.Response.Redirect("/login-error");
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        return builder;
    }
}
