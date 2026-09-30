using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SimpleAccount.Contracts;

namespace SimpleAccount.Account.Tests;

/// <summary>
/// Mints the same shape of token the gateway does. Tests keep the real JWT middleware and
/// only share this signing key, so the auth path is genuinely exercised.
/// </summary>
public static class TestTokens
{
    public const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-aaaaaaaa";

    private static readonly JsonWebTokenHandler Handler = new();

    private static readonly SigningCredentials Credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256);

    public static string ForUser(Guid userId, string audience = InternalAudiences.Account) =>
        Create([
            new Claim(ClaimNames.UserId, userId.ToString()),
            new Claim(ClaimNames.GoogleSubject, $"google-{userId}"),
        ], audience);

    public static string ForProvisioning(string audience = InternalAudiences.Account) =>
        Create([new Claim(ClaimNames.Scope, InternalAudiences.ProvisioningScope)], audience);

    public static string Expired(Guid userId) =>
        Create([new Claim(ClaimNames.UserId, userId.ToString())],
            InternalAudiences.Account,
            expires: DateTime.UtcNow.AddMinutes(-5));

    private static string Create(Claim[] claims, string audience, DateTime? expires = null) =>
        Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = InternalAudiences.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = Credentials,
        });
}
