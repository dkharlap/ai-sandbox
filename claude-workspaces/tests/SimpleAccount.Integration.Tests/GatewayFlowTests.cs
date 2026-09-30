using System.Net;
using System.Net.Http.Json;
using SimpleAccount.Contracts;

namespace SimpleAccount.Integration.Tests;

public class GatewayFlowTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    private static async Task SignInAsync(HttpClient browser, string sub, string email)
    {
        // The same dev sign-in a developer uses with no Google credentials, which runs the
        // real provisioning and cookie sign-in path.
        var response = await browser.GetAsync(
            $"/bff/dev-login/signin?sub={sub}&email={Uri.EscapeDataString(email)}&firstName=Ada&lastName=Lovelace");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task Api_call_without_a_session_is_401_not_a_redirect_to_google()
    {
        var browser = fixture.CreateBrowser();

        var response = await browser.GetAsync("/v1/preferences/me");

        // The regression guard for the challenge-scheme bug: an SPA fetch must get a status
        // code, never a 302 toward Google's HTML login page.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Config_reports_dev_mode_when_google_is_not_configured()
    {
        var config = await fixture.CreateBrowser()
            .GetFromJsonAsync<Dictionary<string, bool>>("/bff/config");

        Assert.True(config!["devMode"]);
    }

    [Fact]
    public async Task Login_hands_off_to_the_dev_signin_page_when_google_is_not_configured()
    {
        var response = await fixture.CreateBrowser().GetAsync("/bff/login?returnUrl=%2F");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.StartsWith("/bff/dev-login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Bff_me_without_a_session_is_401()
    {
        var response = await fixture.CreateBrowser().GetAsync("/bff/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Signing_in_provisions_an_account_and_establishes_a_session()
    {
        var browser = fixture.CreateBrowser();
        await SignInAsync(browser, "e2e-session", "session@example.com");

        var response = await browser.GetAsync("/bff/me");
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.True(session!.ContainsKey("userId"));
    }

    [Fact]
    public async Task The_cookie_alone_authenticates_proxied_calls_to_both_services()
    {
        var browser = fixture.CreateBrowser();
        await SignInAsync(browser, "e2e-proxy", "proxy@example.com");

        // Account, through the gateway's minted token.
        var user = await browser.GetFromJsonAsync<UserDto>("/v1/users/me");
        Assert.Equal("proxy@example.com", user!.Email);
        Assert.Equal("Ada", user.FirstName);

        // Preferences, with a token minted for a different audience.
        var catalog = await browser.GetFromJsonAsync<List<ModelDto>>("/v1/catalog");
        Assert.Equal(5, catalog!.Count);
    }

    [Fact]
    public async Task Preferences_round_trip_through_the_gateway()
    {
        var browser = fixture.CreateBrowser();
        await SignInAsync(browser, "e2e-prefs", "prefs@example.com");

        var put = await browser.PutAsJsonAsync("/v1/preferences/me",
            new ReplacePreferencesRequest(["claude-opus-5", "gpt-5-codex"]));
        put.EnsureSuccessStatusCode();

        var preferences = await browser.GetFromJsonAsync<List<PreferenceDto>>("/v1/preferences/me");

        Assert.Equal(["claude-opus-5", "gpt-5-codex"], preferences!.Select(p => p.ModelId));
    }

    [Fact]
    public async Task Signing_in_twice_reuses_the_same_account()
    {
        var first = fixture.CreateBrowser();
        await SignInAsync(first, "e2e-repeat", "repeat@example.com");
        var firstUser = await first.GetFromJsonAsync<UserDto>("/v1/users/me");

        var second = fixture.CreateBrowser();
        await SignInAsync(second, "e2e-repeat", "repeat@example.com");
        var secondUser = await second.GetFromJsonAsync<UserDto>("/v1/users/me");

        Assert.Equal(firstUser!.Id, secondUser!.Id);
    }

    [Fact]
    public async Task A_client_supplied_bearer_token_cannot_bypass_the_cookie()
    {
        var browser = fixture.CreateBrowser();
        browser.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-real-token");

        var response = await browser.GetAsync("/v1/preferences/me");

        // The gateway strips client-supplied Authorization headers before proxying, so this
        // is rejected at the cookie boundary rather than reaching a service.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var browser = fixture.CreateBrowser();
        await SignInAsync(browser, "e2e-logout", "logout@example.com");

        (await browser.PostAsync("/bff/logout", null)).EnsureSuccessStatusCode();

        var response = await browser.GetAsync("/bff/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
