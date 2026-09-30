using Microsoft.EntityFrameworkCore;
using SimpleAccount.Contracts;
using SimpleAccount.Preferences.Data;

namespace SimpleAccount.Preferences.Services;

/// <summary>Raised when a request names a model that is not in the active catalog.</summary>
public class UnknownModelException(IReadOnlyList<string> modelIds)
    : Exception($"Unknown or inactive model(s): {string.Join(", ", modelIds)}")
{
    public IReadOnlyList<string> ModelIds { get; } = modelIds;
}

public class DuplicateModelException() : Exception("The same model may not appear twice in a preference list.");

public class PreferenceService(PreferencesDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<ModelDto>> GetCatalogAsync(CancellationToken ct) =>
        await db.Catalog
            .Where(m => m.IsActive)
            .OrderBy(m => m.Vendor).ThenBy(m => m.DisplayName)
            .Select(m => new ModelDto(m.Id, m.DisplayName, m.Vendor))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PreferenceDto>> GetForUserAsync(Guid userId, CancellationToken ct) =>
        await db.Preferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Rank)
            .Select(p => new PreferenceDto(p.ModelId, p.Model!.DisplayName, p.Model.Vendor, p.Rank))
            .ToListAsync(ct);

    /// <summary>
    /// Replaces the user's entire list. Delete-then-insert inside one transaction keeps rank
    /// contiguous and avoids the partial-reorder states that per-item patching produces.
    /// </summary>
    public async Task<IReadOnlyList<PreferenceDto>> ReplaceAsync(
        Guid userId, IReadOnlyList<string> modelIds, CancellationToken ct)
    {
        if (modelIds.Distinct(StringComparer.Ordinal).Count() != modelIds.Count)
        {
            throw new DuplicateModelException();
        }

        await EnsureModelsExistAsync(modelIds, ct);

        // Aspire's AddNpgsqlDbContext enables a retrying execution strategy, which refuses
        // user-initiated transactions unless they run inside the strategy. Delete-then-insert
        // is idempotent, so replaying the whole unit on a transient failure is safe.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async cancellationToken =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

            await db.Preferences.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            var now = clock.GetUtcNow();
            for (var rank = 0; rank < modelIds.Count; rank++)
            {
                db.Preferences.Add(new UserModelPreference
                {
                    UserId = userId,
                    ModelId = modelIds[rank],
                    Rank = rank,
                    CreatedAt = now,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }, ct);

        return await GetForUserAsync(userId, ct);
    }

    /// <summary>Appends a model to the end of the list. No-op if already present.</summary>
    public async Task<IReadOnlyList<PreferenceDto>> AddAsync(Guid userId, string modelId, CancellationToken ct)
    {
        await EnsureModelsExistAsync([modelId], ct);

        var existing = await db.Preferences
            .Where(p => p.UserId == userId)
            .Select(p => p.ModelId)
            .ToListAsync(ct);

        if (existing.Contains(modelId, StringComparer.Ordinal))
        {
            return await GetForUserAsync(userId, ct);
        }

        return await ReplaceAsync(userId, [.. existing, modelId], ct);
    }

    /// <summary>Removes a model and renumbers the remainder so ranks stay contiguous.</summary>
    public async Task<IReadOnlyList<PreferenceDto>> RemoveAsync(Guid userId, string modelId, CancellationToken ct)
    {
        var remaining = await db.Preferences
            .Where(p => p.UserId == userId && p.ModelId != modelId)
            .OrderBy(p => p.Rank)
            .Select(p => p.ModelId)
            .ToListAsync(ct);

        return await ReplaceAsync(userId, remaining, ct);
    }

    private async Task EnsureModelsExistAsync(IReadOnlyList<string> modelIds, CancellationToken ct)
    {
        if (modelIds.Count == 0)
        {
            return;
        }

        var known = await db.Catalog
            .Where(m => m.IsActive && modelIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);

        var unknown = modelIds.Except(known, StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
        {
            throw new UnknownModelException(unknown);
        }
    }
}
