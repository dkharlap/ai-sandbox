namespace SimpleAccount.Contracts;

/// <summary>
/// Claim names on the short-lived JWT the gateway mints for internal service calls.
/// The gateway is the issuer for this trust domain; Google's own token never leaves it.
/// </summary>
public static class ClaimNames
{
    /// <summary>Our internal user id (a <see cref="Guid"/>), not Google's subject.</summary>
    public const string UserId = "uid";

    /// <summary>Google's immutable subject identifier.</summary>
    public const string GoogleSubject = "gsub";

    public const string Email = "email";

    /// <summary>Present on system-to-system tokens that carry no user identity.</summary>
    public const string Scope = "scope";
}

public static class InternalAudiences
{
    public const string Account = "account";
    public const string Preferences = "preferences";
    public const string Issuer = "simpleaccount-gateway";

    /// <summary>Scope on the provisioning token used before a user id exists.</summary>
    public const string ProvisioningScope = "provisioning";
}
