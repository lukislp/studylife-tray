# Contributing to studylife-tray

Thanks for taking the time. This is a single-maintainer project, so the process is deliberately
small - but it is the same for every change, including the maintainer's own.

## How changes get in

1. Open an issue first for anything bigger than a typo or an obvious bug fix, so the direction can
   be agreed before you spend time on it. Use the templates under `.github/ISSUE_TEMPLATE/`.
2. Fork the repository (or branch, if you have write access) and make your change on a branch.
3. Open a pull request against `main`. The pull-request template asks for what changed and why.
4. `main` is protected: a PR merges only once its required checks are green
   (`ci.yml`'s `build-and-test` job, plus `review / dependency-review` and CodeQL code scanning)
   and the branch is up to date with `main`. Nobody pushes to `main` directly, not even the
   maintainer.

## What a pull request needs

- **Conventional Commit title.** (`fix:`, `feat:`, `ci:`, `docs:`, ...) There is no release
  automation wired up in this repo yet, so a commit type does not currently trigger a version bump
  or a `CHANGELOG.md` entry - but the title still documents the kind of change at a glance, and
  it's the convention the rest of this account's repos use, so keep using it here too. If the PR is
  squash-merged, the squashed commit message - usually the PR title - is what ends up in history.
- **Tests for new functionality.** New behaviour and bug fixes come with tests under
  `tests/StudyLifeTray.Tests/`. `ApiClientTests.cs` covers `ApiClient`'s HTTP-facing behaviour
  (polling `/api/timerstate`, the tray-assertion exchange, and how each failure mode - unauthorized,
  server error, network failure - maps to a `PollResultKind`) against a stub `HttpMessageHandler`
  that records requests instead of hitting a real server. `SettingsStoreTests.cs` covers
  `SettingsStore.NormalizeServerUrl` (stripping a pasted URL down to its origin) and
  `TrayAppSettings.IsConnected`.
- **No secrets or personal data.** Nothing under `src/` or `tests/` should reference a real
  server URL, hostname, or email - use a placeholder like `https://studylife.example.com`, the way
  the existing tests do.
- **Wire shapes.** `ApiClient.cs` talks to a couple of StudyLife REST endpoints
  (`GET /api/timerstate`, `POST /api/auth/tray-assertion-exchange`) through small hand-written DTOs
  such as `TimerStateDtoPayload`. The server accepts unknown JSON properties silently, so a wrong
  field name produces a green build and a tray icon that quietly never updates - check a field
  against the server's actual DTO/entity before relying on it, don't guess from the docs alone.

## Running things locally

```
dotnet restore
dotnet build -c Release
dotnet test
```

This is a Windows Forms app targeting `net10.0-windows`, so building and testing it requires the
.NET 10 SDK with the Windows Forms workload, on Windows - the same reason CI runs on
`windows-latest` rather than Linux.

## Security issues

Please do not open a public issue for a vulnerability - use the private reporting path described
in [SECURITY.md](SECURITY.md).
