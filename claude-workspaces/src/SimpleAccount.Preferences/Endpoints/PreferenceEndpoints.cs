using System.Security.Claims;
using SimpleAccount.Contracts;
using SimpleAccount.Preferences.Services;
using SimpleAccount.ServiceDefaults;

namespace SimpleAccount.Preferences.Endpoints;

public static class PreferenceEndpoints
{
    public static void MapPreferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1").RequireAuthorization(InternalPolicies.User);

        v1.MapGet("/catalog", async (PreferenceService prefs, CancellationToken ct) =>
                Results.Ok(await prefs.GetCatalogAsync(ct)))
            .WithName("GetCatalog");

        var me = v1.MapGroup("/preferences/me");

        me.MapGet("/", async (ClaimsPrincipal user, PreferenceService prefs, CancellationToken ct) =>
                Results.Ok(await prefs.GetForUserAsync(user.GetUserId(), ct)))
            .WithName("GetMyPreferences");

        me.MapPut("/", async (
                ReplacePreferencesRequest request,
                ClaimsPrincipal user,
                PreferenceService prefs,
                CancellationToken ct) =>
                Results.Ok(await prefs.ReplaceAsync(user.GetUserId(), request.ModelIds, ct)))
            .WithName("ReplaceMyPreferences");

        me.MapPost("/items", async (
                AddPreferenceRequest request,
                ClaimsPrincipal user,
                PreferenceService prefs,
                CancellationToken ct) =>
                Results.Ok(await prefs.AddAsync(user.GetUserId(), request.ModelId, ct)))
            .WithName("AddMyPreference");

        me.MapDelete("/items/{modelId}", async (
                string modelId,
                ClaimsPrincipal user,
                PreferenceService prefs,
                CancellationToken ct) =>
                Results.Ok(await prefs.RemoveAsync(user.GetUserId(), modelId, ct)))
            .WithName("RemoveMyPreference");
    }
}
