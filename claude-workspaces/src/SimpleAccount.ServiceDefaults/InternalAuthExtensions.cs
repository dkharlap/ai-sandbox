using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SimpleAccount.Contracts;

namespace SimpleAccount.ServiceDefaults;

public static class InternalAuthExtensions
{
    /// <summary>
    /// Validates the short-lived JWT the gateway mints. The browser never holds this token —
    /// it reaches the service only via the gateway's YARP transform, which strips any
    /// client-supplied Authorization header first.
    /// </summary>
    public static IHostApplicationBuilder AddInternalJwtAuth(
        this IHostApplicationBuilder builder,
        string audience)
    {
        var signingKey = builder.Configuration["InternalAuth:SigningKey"]
            ?? throw new InvalidOperationException(
                "InternalAuth:SigningKey is not configured. The AppHost supplies it as a parameter.");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = InternalAudiences.Issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(InternalPolicies.Provisioning, policy =>
                policy.RequireClaim(ClaimNames.Scope, InternalAudiences.ProvisioningScope))
            .AddPolicy(InternalPolicies.User, policy =>
                policy.RequireClaim(ClaimNames.UserId));

        return builder;
    }
}

public static class InternalPolicies
{
    /// <summary>System-to-system calls made before a user id exists.</summary>
    public const string Provisioning = "provisioning";

    /// <summary>Calls acting on behalf of a signed-in user.</summary>
    public const string User = "user";
}
