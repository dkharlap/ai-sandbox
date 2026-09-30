# SimpleAccount

Google SSO sign-in plus per-user coding-model preferences, built as four services on
.NET 10 with a React SPA.

## Services

| Service | Role |
|---|---|
| **Postgres** | One server, a schema per owning service (`accounts`, `prefs`) |
| **Account** | Owns users; upserts first/last name and email from Google claims |
| **Preferences** | Owns model preferences and the seeded model catalog |
| **Gateway (BFF)** | The only origin the browser talks to — terminates Google OIDC, holds the session cookie, proxies to the other two |

The React + Vite SPA is served through the gateway, so there is a single origin in both
development and production.

## Authentication

Two trust domains:

- **Browser ↔ gateway** — an HttpOnly, `SameSite=Lax`, `__Host-`-prefixed cookie. No token
  ever reaches JavaScript.
- **Gateway ↔ services** — a two-minute JWT the gateway mints per downstream audience. The
  gateway strips any client-supplied `Authorization` header and the cookie before proxying,
  so a caller cannot present its own token.

Google's `id_token` is deliberately *not* forwarded: its audience is the gateway's client id,
so downstream services would have to disable audience validation to accept it.

Identity is keyed on Google's `sub`, never email — emails change, and released addresses get
reassigned.

## Running

No configuration required. With Docker running:

```sh
dotnet run --project src/SimpleAccount.AppHost
```

The console prints a dashboard URL with a login token (the port is assigned per run, so it
differs each time; several instances can run side by side). Open it, click through to the
`gateway` resource's HTTPS endpoint, and pick one of three demo users.

Run the tests with `dotnet test`.

Prerequisites: the .NET 10 SDK, Node with pnpm, and a running Docker daemon (Postgres runs
in a container; the first start pulls `postgres:17-alpine`). Aspire installs the SPA's npm
packages itself, so no `pnpm install` step is needed.

The `aspire` CLI is an optional alternative (`aspire run`). It installs to `~/.dotnet/tools`,
which is not on the default macOS PATH — add it first:

```sh
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Optional: real Google sign-in

```sh
dotnet user-secrets set "Google:ClientId"     "<id>"     --project src/SimpleAccount.AppHost
dotnet user-secrets set "Google:ClientSecret" "<secret>" --project src/SimpleAccount.AppHost
```

Then register `https://localhost:<gateway-port>/signin-google` as an authorized redirect URI
in the Google Cloud console; the port is shown in the Aspire dashboard. With credentials
present the dev sign-in page disappears and the OIDC flow takes over.

### Dev sign-in

With no Google credentials the gateway serves `/bff/dev-login`, a page listing demo users.
Choosing one runs the *same* `AccountProvisioner` and cookie sign-in as the real OIDC
callback, so only the Google token exchange itself is skipped.

It is enabled only when **both** hold: the environment is not Production, *and* no Google
credentials are configured. A production deploy that forgets its credentials therefore fails
closed rather than exposing a password-less login — pinned by `AuthModeTests`.

## Tests

- `tests/SimpleAccount.{Account,Preferences}.Tests` — each service against a throwaway Postgres
  container (Testcontainers), keeping the real JWT middleware and overriding only the
  signing key.
- `tests/SimpleAccount.Integration.Tests` — the whole solution via `Aspire.Hosting.Testing`,
  covering the cookie/JWT exchange, plus `AuthModeTests` for the dev sign-in guard. The
  tests sign in through the same `/bff/dev-login` path a developer uses.
