using System.Net.Http.Json;
using System.Security.Claims;
using SimpleAccount.Contracts;

namespace SimpleAccount.Gateway.Auth;

/// <summary>
/// Creates or refreshes the Account row at login and stamps the resulting internal user id
/// onto the principal, so the cookie carries it for the rest of the session.
/// </summary>
public class AccountProvisioner(
    IHttpClientFactory httpClientFactory,
    IInternalTokenIssuer issuer,
    ILogger<AccountProvisioner> logger)
{
    public const string HttpClientName = "account";

    public async Task ProvisionAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var googleSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google token is missing a subject claim.");
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Google token is missing an email claim.");

        var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            issuer.MintSystemToken(InternalAudiences.Account, InternalAudiences.ProvisioningScope));

        var request = new UpsertUserRequest(
            googleSubject,
            email,
            principal.FindFirstValue(ClaimTypes.GivenName),
            principal.FindFirstValue(ClaimTypes.Surname));

        var response = await client.PutAsJsonAsync("/internal/users/by-sub", request, ct);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<UserDto>(ct)
            ?? throw new InvalidOperationException("Account service returned an empty body.");

        // The internal id is what downstream services key on; Google's sub never reaches them
        // as an identifier.
        principal.AddIdentity(new ClaimsIdentity([
            new Claim(ClaimNames.UserId, user.Id.ToString()),
            new Claim(ClaimNames.GoogleSubject, googleSubject),
        ]));

        logger.LogInformation("Provisioned session for user {UserId}", user.Id);
    }
}
