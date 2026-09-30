using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SimpleAccount.Contracts;

namespace SimpleAccount.Gateway.Auth;

public interface IInternalTokenIssuer
{
    /// <summary>Token representing a signed-in user, scoped to one downstream audience.</summary>
    string GetOrMintUserToken(Guid userId, string googleSubject, string? email, string audience);

    /// <summary>
    /// Token for system-to-system calls made before a user id exists — only account
    /// provisioning during the OIDC callback.
    /// </summary>
    string MintSystemToken(string audience, string scope);
}

/// <summary>
/// The gateway is the issuer for the internal trust domain. Google's id_token never leaves
/// here: its audience is this client, so downstream services could not validate it honestly.
/// </summary>
public class InternalTokenIssuer : IInternalTokenIssuer
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CacheHeadroom = TimeSpan.FromSeconds(30);

    private readonly SigningCredentials _credentials;
    private readonly TimeProvider _clock;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiresAt)> _cache = new();

    public InternalTokenIssuer(IConfiguration configuration, TimeProvider clock)
    {
        var key = configuration["InternalAuth:SigningKey"]
            ?? throw new InvalidOperationException(
                "InternalAuth:SigningKey is not configured. The AppHost supplies it as a parameter.");

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        _clock = clock;
    }

    public string GetOrMintUserToken(Guid userId, string googleSubject, string? email, string audience)
    {
        var cacheKey = $"{userId}|{audience}";
        var now = _clock.GetUtcNow();

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt - CacheHeadroom > now)
        {
            return cached.Token;
        }

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.GoogleSubject, googleSubject),
        };
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimNames.Email, email));
        }

        var expires = now.Add(Lifetime);
        var token = Mint(claims, audience, now, expires);
        _cache[cacheKey] = (token, expires);
        return token;
    }

    public string MintSystemToken(string audience, string scope)
    {
        var now = _clock.GetUtcNow();
        return Mint([new Claim(ClaimNames.Scope, scope)], audience, now, now.Add(Lifetime));
    }

    private string Mint(IEnumerable<Claim> claims, string audience, DateTimeOffset now, DateTimeOffset expires) =>
        _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = InternalAudiences.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
        });
}
