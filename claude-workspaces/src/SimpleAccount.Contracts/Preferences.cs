namespace SimpleAccount.Contracts;

public record ModelDto(
    string Id,
    string DisplayName,
    string Vendor);

public record PreferenceDto(
    string ModelId,
    string DisplayName,
    string Vendor,
    int Rank);

/// <summary>
/// Replaces the caller's whole preference list. Whole-list replace is the primary write
/// path so drag-to-reorder never has to reconcile rank uniqueness client-side.
/// </summary>
public record ReplacePreferencesRequest(IReadOnlyList<string> ModelIds);

public record AddPreferenceRequest(string ModelId);
