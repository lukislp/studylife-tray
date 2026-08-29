# Privacy Policy - StudyLife Tray

StudyLife Tray is a desktop app for a self-hosted [StudyLife](https://github.com/lukislp/studylife)
instance. There is no vendor server involved - your data goes exactly two places: your own
StudyLife server, and your own device's local application data folder.

## What this app reads

- **Whether a focus session is currently running**, via `GET /api/timerstate` on the StudyLife
  server you configure, roughly every 30 seconds. The app's API key cannot read anything else
  from your account - not your notes, sessions, courses, or settings (see the `studylife` repo's
  `ApiKeyScopes.Tray` for the server-enforced scope).

## What this app stores

Locally, under `%APPDATA%\StudyLifeTray\settings.json`, never transmitted anywhere except back
to your own StudyLife server as part of authenticating the poll above:

- Your StudyLife server's base URL.
- Your Tray API key (obtained via the passkey-backed browser-consent connect flow - you never
  see or copy/paste the key itself).

## What this app never does

- Never collects analytics, telemetry, or crash reports.
- Never contacts any server other than the one you explicitly configure.
- Never writes anything to your StudyLife account - the API key it uses cannot, even if it
  wanted to (read-only by server-side design).

## Source

This app is open source (AGPL-3.0): <https://github.com/lukislp/studylife-tray>.
