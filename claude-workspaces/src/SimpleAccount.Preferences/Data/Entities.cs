namespace SimpleAccount.Preferences.Data;

/// <summary>Read-only catalog of selectable coding models. Seeded by migration.</summary>
public class CatalogModel
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Vendor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UserModelPreference
{
    /// <summary>
    /// The internal account id. A logical reference only — no cross-schema FK, so Account
    /// and Preferences stay independently migratable.
    /// </summary>
    public Guid UserId { get; set; }

    public required string ModelId { get; set; }

    /// <summary>Position in the user's ordered list, 0-based and unique per user.</summary>
    public int Rank { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public CatalogModel? Model { get; set; }
}
