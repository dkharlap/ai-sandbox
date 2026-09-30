using System.Security.Claims;
using SimpleAccount.Contracts;

namespace SimpleAccount.ServiceDefaults;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the internal user id the gateway stamped onto the token. Endpoints guarded by
    /// <see cref="InternalPolicies.User"/> are guaranteed to have it.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimNames.UserId);
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new InvalidOperationException($"Token is missing a valid '{ClaimNames.UserId}' claim.");
    }
}
