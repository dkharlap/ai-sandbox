using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SimpleAccount.Contracts;

namespace SimpleAccount.Preferences.Tests;

public class PreferenceEndpointsTests(PreferencesApiFactory factory) : IClassFixture<PreferencesApiFactory>
{
    private HttpClient ClientFor(Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.ForUser(userId));
        return client;
    }

    private static async Task<List<PreferenceDto>> ReadPreferences(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<PreferenceDto>>())!;
    }

    [Fact]
    public async Task Catalog_returns_the_seeded_models()
    {
        var response = await ClientFor(Guid.CreateVersion7()).GetAsync("/v1/catalog");
        response.EnsureSuccessStatusCode();
        var catalog = await response.Content.ReadFromJsonAsync<List<ModelDto>>();

        Assert.Equal(5, catalog!.Count);
        Assert.Contains(catalog, m => m.Id == "claude-opus-5");
    }

    [Fact]
    public async Task Preferences_start_empty_for_a_new_user()
    {
        var preferences = await ReadPreferences(await ClientFor(Guid.CreateVersion7()).GetAsync("/v1/preferences/me"));
        Assert.Empty(preferences);
    }

    [Fact]
    public async Task Replace_stores_the_list_in_the_order_given()
    {
        var client = ClientFor(Guid.CreateVersion7());
        var preferences = await ReadPreferences(await client.PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["gemini-3-pro", "claude-opus-5", "gpt-5-codex"])));

        Assert.Equal(["gemini-3-pro", "claude-opus-5", "gpt-5-codex"], preferences.Select(p => p.ModelId));
        Assert.Equal([0, 1, 2], preferences.Select(p => p.Rank));
    }

    [Fact]
    public async Task Reordering_renumbers_ranks_without_tripping_the_unique_index()
    {
        var client = ClientFor(Guid.CreateVersion7());
        await client.PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-opus-5", "claude-sonnet-5", "claude-haiku-4-5"]));

        // Exact reversal is the case that would collide if ranks were patched one at a time.
        var reordered = await ReadPreferences(await client.PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-haiku-4-5", "claude-sonnet-5", "claude-opus-5"])));

        Assert.Equal(["claude-haiku-4-5", "claude-sonnet-5", "claude-opus-5"], reordered.Select(p => p.ModelId));
        Assert.Equal([0, 1, 2], reordered.Select(p => p.Rank));
    }

    [Fact]
    public async Task Adding_appends_to_the_end_and_is_idempotent()
    {
        var client = ClientFor(Guid.CreateVersion7());
        await client.PutAsJsonAsync("/v1/preferences/me", new ReplacePreferencesRequest(["claude-opus-5"]));

        var afterAdd = await ReadPreferences(
            await client.PostAsJsonAsync("/v1/preferences/me/items", new AddPreferenceRequest("gpt-5-codex")));
        Assert.Equal(["claude-opus-5", "gpt-5-codex"], afterAdd.Select(p => p.ModelId));

        var afterDuplicate = await ReadPreferences(
            await client.PostAsJsonAsync("/v1/preferences/me/items", new AddPreferenceRequest("gpt-5-codex")));
        Assert.Equal(2, afterDuplicate.Count);
    }

    [Fact]
    public async Task Removing_closes_the_gap_in_ranks()
    {
        var client = ClientFor(Guid.CreateVersion7());
        await client.PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-opus-5", "gpt-5-codex", "gemini-3-pro"]));

        var remaining = await ReadPreferences(await client.DeleteAsync("/v1/preferences/me/items/gpt-5-codex"));

        Assert.Equal(["claude-opus-5", "gemini-3-pro"], remaining.Select(p => p.ModelId));
        Assert.Equal([0, 1], remaining.Select(p => p.Rank));
    }

    [Fact]
    public async Task Preferences_are_scoped_to_the_calling_user()
    {
        var alice = Guid.CreateVersion7();
        var bob = Guid.CreateVersion7();
        await ClientFor(alice).PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-opus-5"]));

        var bobsPreferences = await ReadPreferences(await ClientFor(bob).GetAsync("/v1/preferences/me"));
        Assert.Empty(bobsPreferences);
    }

    [Fact]
    public async Task An_unknown_model_is_rejected_with_problem_details()
    {
        var response = await ClientFor(Guid.CreateVersion7()).PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["not-a-real-model"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Contains("not-a-real-model", problem!["detail"].ToString());
    }

    [Fact]
    public async Task A_duplicated_model_is_rejected()
    {
        var response = await ClientFor(Guid.CreateVersion7()).PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-opus-5", "claude-opus-5"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preferences_without_a_token_is_401()
    {
        var response = await factory.CreateClient().GetAsync("/v1/preferences/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_minted_for_the_account_service_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.ForUser(Guid.CreateVersion7(), InternalAudiences.Account));

        var response = await client.GetAsync("/v1/preferences/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
