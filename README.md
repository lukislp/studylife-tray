# StudyLife Tray

[![CI](https://github.com/lukislp/studylife-tray/actions/workflows/ci.yml/badge.svg)](https://github.com/lukislp/studylife-tray/actions/workflows/ci.yml) [![OpenSSF Scorecard](https://img.shields.io/ossf-scorecard/github.com/lukislp/studylife-tray?label=openssf+scorecard&style=flat)](https://scorecard.dev/viewer/?uri=github.com/lukislp/studylife-tray) [![CodeQL](https://github.com/lukislp/studylife-tray/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lukislp/studylife-tray/security/code-scanning)
[![Release](https://img.shields.io/github/v/release/lukislp/studylife-tray)](https://github.com/lukislp/studylife-tray/releases)
[![License: AGPL-3.0](https://img.shields.io/github/license/lukislp/studylife-tray)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

A Windows system tray app that shows your [StudyLife](https://github.com/lukislp/studylife)
focus-session status - independent of any browser, so it works even when you don't have one
open, and doesn't count against a browser's limited number of installable extensions.

## How it works

1. Connect it to your self-hosted StudyLife server (base URL only) via the tray icon's
   Settings window - the same passkey-backed consent flow the browser-based add-ons use,
   adapted for a native app (your default browser opens to approve the connection, then hands
   control back to this app over a local loopback redirect - RFC 8252).
2. The app polls `GET /api/timerstate` roughly every 30 seconds and updates the tray icon's
   tooltip to reflect whether a focus session is currently running.
3. Double-click the tray icon (or use "Open StudyLife" from its menu) to jump straight to your
   server in the browser.

There is no page-side "instant reaction" path here the way the browser extensions have (that
mechanism relies on a page dispatching a browser event a content script can hear - a native
desktop app has no page to listen to), so 30 seconds is the only cadence, not a fallback for
something faster.

## Why the API key is this narrow

The Tray app's StudyLife key can reach exactly one endpoint: `GET /api/timerstate` (plus
`whoami` for diagnostics) - identical scope to
[studylife-focusguard](https://github.com/lukislp/studylife-focus). It can't read your notes,
sessions, courses, or settings, and it can't write anything at all.

## Development

Requires the .NET 10 SDK with the Windows Forms workload (Windows only - `net10.0-windows`).

```
dotnet build
dotnet test
```

## License

AGPL-3.0 - see [LICENSE](LICENSE).
