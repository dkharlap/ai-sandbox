namespace SimpleAccount.Account.Data;

public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// Google's `sub`. The identity key: immutable and never reused, unlike email.
    /// </summary>
    public required string GoogleSubject { get; set; }

    /// <summary>Mutable profile data, refreshed from claims on every login. Not unique.</summary>
    public required string Email { get; set; }

    public string? FirstName { get; set; }

    /// <summary>Nullable: Google does not guarantee a `family_name` claim.</summary>
    public string? LastName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
