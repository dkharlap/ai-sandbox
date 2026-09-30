using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Shared HMAC key for the gateway-minted internal JWTs. Generated on first run and persisted
// to user secrets so it stays stable across restarts.
var internalJwtKey = builder.AddParameter(
    "internal-jwt-key",
    new GenerateParameterDefault { MinLength = 64, Special = false },
    secret: true,
    persist: true);

// Google OIDC credentials are optional. With none set, the gateway falls back to its
// built-in dev sign-in page, so a fresh clone runs with no configuration at all. To use
// real Google sign-in:
//   dotnet user-secrets set Google:ClientId     <id>     --project src/SimpleAccount.AppHost
//   dotnet user-secrets set Google:ClientSecret <secret> --project src/SimpleAccount.AppHost
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];

// Integration tests pass --UseEphemeralDatabase=true so `dotnet test` gets a throwaway
// database. Without it the test host would share the developer's named data volume and
// write its fixture users straight into the dev database.
var useEphemeralDatabase = builder.Configuration.GetValue<bool>("UseEphemeralDatabase");

var postgres = builder.AddPostgres("postgres");

if (!useEphemeralDatabase)
{
    postgres = postgres.WithDataVolume().WithPgAdmin();
}

var accountsDb = postgres.AddDatabase("accountsdb");
var prefsDb = postgres.AddDatabase("prefsdb");

var account = builder.AddProject<Projects.SimpleAccount_Account>("account")
    .WithReference(accountsDb).WaitFor(accountsDb)
    .WithEnvironment("InternalAuth__SigningKey", internalJwtKey);

var preferences = builder.AddProject<Projects.SimpleAccount_Preferences>("preferences")
    .WithReference(prefsDb).WaitFor(prefsDb)
    .WithEnvironment("InternalAuth__SigningKey", internalJwtKey);

// Only the gateway is reachable from the browser. Account and Preferences stay internal, so
// the minted-JWT boundary is the only way in.
var web = builder.AddViteApp("web", "../web")
    .WithPnpm();

// The browser only ever talks to the gateway: Google redirects back to the gateway's
// registered redirect_uri, so the session cookie must be set on the gateway origin. In
// development the gateway proxies everything else to the Vite dev server (HMR included).
var gateway = builder.AddProject<Projects.SimpleAccount_Gateway>("gateway")
    .WithReference(account).WaitFor(account)
    .WithReference(preferences).WaitFor(preferences)
    .WithReference(web).WaitFor(web)
    .WithEnvironment("InternalAuth__SigningKey", internalJwtKey)
    .WithExternalHttpEndpoints();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    gateway
        .WithEnvironment("Authentication__Google__ClientId", googleClientId)
        .WithEnvironment("Authentication__Google__ClientSecret", googleClientSecret);
}

builder.Build().Run();
