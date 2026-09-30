using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SimpleAccount.Contracts;

namespace SimpleAccount.Account.Tests;

public class UserEndpointsTests(AccountApiFactory factory) : IClassFixture<AccountApiFactory>
{
    private HttpClient ClientWith(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<UserDto> UpsertAsync(string sub, string email, string? first = "Ada", string? last = "Lovelace")
    {
        var client = ClientWith(TestTokens.ForProvisioning());
        var response = await client.PutAsJsonAsync("/internal/users/by-sub",
            new UpsertUserRequest(sub, email, first, last));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    [Fact]
    public async Task Upsert_creates_a_user_on_first_login()
    {
        var user = await UpsertAsync("sub-create", "ada@example.com");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("Ada", user.FirstName);
    }

    [Fact]
    public async Task Upsert_is_idempotent_and_refreshes_mutable_profile_fields()
    {
        var first = await UpsertAsync("sub-idempotent", "old@example.com", "Old", "Name");
        var second = await UpsertAsync("sub-idempotent", "new@example.com", "New", "Name");

        // Same identity, because google_subject is the key — not a second row.
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("new@example.com", second.Email);
        Assert.Equal("New", second.FirstName);
    }

    [Fact]
    public async Task Two_subjects_sharing_an_email_are_distinct_users()
    {
        // A released address reassigned to a new person must not collide.
        var a = await UpsertAsync("sub-shared-a", "shared@example.com");
        var b = await UpsertAsync("sub-shared-b", "shared@example.com");

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public async Task Upsert_rejects_a_request_with_no_subject()
    {
        var client = ClientWith(TestTokens.ForProvisioning());
        var response = await client.PutAsJsonAsync("/internal/users/by-sub",
            new UpsertUserRequest("", "ada@example.com", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_returns_the_caller_profile()
    {
        var created = await UpsertAsync("sub-me", "me@example.com");

        var response = await ClientWith(TestTokens.ForUser(created.Id)).GetAsync("/v1/users/me");
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal(created.Id, user!.Id);
    }

    [Fact]
    public async Task Put_me_updates_the_name()
    {
        var created = await UpsertAsync("sub-rename", "rename@example.com");

        var response = await ClientWith(TestTokens.ForUser(created.Id))
            .PutAsJsonAsync("/v1/users/me", new UpdateUserRequest("Grace", "Hopper"));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal("Grace", user!.FirstName);
        Assert.Equal("Hopper", user.LastName);
    }

    [Fact]
    public async Task Get_me_without_a_token_is_401()
    {
        var response = await factory.CreateClient().GetAsync("/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_for_another_audience_is_rejected()
    {
        var token = TestTokens.ForUser(Guid.CreateVersion7(), InternalAudiences.Preferences);
        var response = await ClientWith(token).GetAsync("/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var response = await ClientWith(TestTokens.Expired(Guid.CreateVersion7())).GetAsync("/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_provisioning_token_cannot_read_user_data()
    {
        var response = await ClientWith(TestTokens.ForProvisioning()).GetAsync("/v1/users/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
