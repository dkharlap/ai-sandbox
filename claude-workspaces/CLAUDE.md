# SimpleAccount

A standalone .NET 10 / C# solution: Google SSO sign-in and per-user coding-model
preferences, across four services (Postgres, Account, Preferences, Gateway/BFF)
orchestrated by .NET Aspire, with a React + Vite SPA.

## Convention boundary

This project **does not inherit conventions from `~/Documents/.claude/rules/`.**


## Parallel feature work

Several features can be developed at once, one git worktree each, one VSCode window each,
each running its own app host.

### Setup

```sh
cd /Users/dkharlap/Documents/Projects/ai-sandbox
git worktree add ../sa-<feature> -b feature/<feature>
git worktree list                 # confirm the path and branch
```

`../` puts the worktree beside the repo (`~/Documents/Projects/sa-<feature>`), not inside
it — which is what keeps it clear of `.claude/`.

Then **File → Open Folder** on that path in a new VSCode window, so the file tree, terminal
and Claude session all agree on where they are. Note the solution root is one level down:
`sa-<feature>/claude-workspaces/`.

Each worktree costs ~314 MB (87 MB of it `node_modules`) and, while running, a Postgres and
a pgAdmin container. `pnpm install` runs automatically on first start, so the first launch
is slow.

### Per-worktree loop

Fully independent — build, test and run concurrently:

```sh
dotnet test                                     # 37 tests, own throwaway database
dotnet run --project src/SimpleAccount.AppHost  # own dashboard port, own Postgres
```

NOTE: The migration snapshot is the dangerous one: git will merge two snapshots into a file that
matches neither migration, and nothing fails until the next `dotnet ef migrations add`.

### Merge order

```sh
# main checkout, one branch at a time
git merge feature/<feature>

# then in each remaining worktree
git rebase main && dotnet test
```

Rebase after every merge so the next branch is tested against what actually landed. If one
feature changes `Contracts`, merge that one first.

### Teardown

```sh
git worktree remove ../sa-<feature>
git branch -d feature/<feature>
```

### More details (if interested)

**Worktrees must live outside `.claude/`.** One created under `.claude/worktrees/` builds
fine but fails 5 integration tests — the gateway 404s every proxied `/v1/**` route while
`/bff/*` still works. The identical commit in a worktree anywhere else passes all 37. Cause
unknown; location is the only variable (file trees are byte-identical). This rules out
`/worktree` and `EnterWorktree`, which hardcode that path. `.claude/settings.json` sets
`worktree.baseRef: "head"`, which only affects those tools — it does nothing for worktrees
created by hand.

**No pinned ports.** `launchSettings.json` deliberately pins none, so Aspire assigns free
ports per instance and the dashboard URL differs each run (printed at startup). Do not
reintroduce `applicationUrl`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` or
`ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` — any pinned port makes the second instance fail to
bind.

**Databases isolate themselves.** Aspire hashes the app-host *path* into the Postgres
volume name, so each worktree gets its own database.

**Do not try to override ports with environment variables.** `AspireUseCliBundle` makes
`dotnet run` delegate to the `aspire` CLI, which re-applies the launch profile. Running the
built binary directly bypasses that but silently drops HTTPS, breaking the `__Host-` cookie
in a browser.

**Stale Aspire processes survive a plain `pkill` on the project name** — the `aspire.cli`
DCP controllers keep running and hold ports:

```sh
pkill -f aspire.cli.osx-arm64
pkill -f SimpleAccount.AppHost
```

## Running

Zero configuration: `aspire run` (or `dotnet run --project src/SimpleAccount.AppHost`), then
`dotnet test`. Needs the .NET 10 SDK, pnpm, and Docker for Postgres.

With no Google credentials the gateway serves a dev sign-in page listing demo users, running
the same provisioning and cookie sign-in as the real OIDC callback. It is gated on
`AuthMode.DevSignInEnabled`, which requires a non-Production environment AND absent Google
credentials — do not loosen either condition. Set `Google:ClientId` / `Google:ClientSecret`
in the AppHost's user secrets to switch to the real Google flow.
