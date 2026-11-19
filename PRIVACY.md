# Privacy Policy - StudyAtrium Tray

StudyAtrium Tray is a desktop app for a self-hosted [StudyAtrium](https://github.com/lukislp/studylife)
instance. There is no vendor server involved - your data goes exactly two places: your own
StudyAtrium server, and your own device's local application data folder.

**Controller:** the operator of your instance. **Publisher:** see the imprint at
<https://lktec.org/impressum>.

## What this app reads

- **Whether a focus session is currently running**, via `GET /api/timerstate` on the StudyAtrium
  server you configure, roughly every 30 seconds. The response describes the current timer state:
  whether it is running or in a break, the current round, the end time of the current phase, and
  the id of the running session. The app shows only "running or not" in the tray icon's tooltip.
- The app's own code requests nothing else. The key itself is limited by your server to two
  read-only endpoints (see `ApiKeyScopes.Tray` in the `studylife` repo): `GET /api/timerstate`
  and `GET /api/auth/whoami` (your numeric user id and the name of the key's slot, used for
  diagnostics). It cannot read your notes, sessions, courses, or settings, and it cannot write
  anything. That is a limit enforced by the server, not by this app: anyone who obtains the key
  can read your timer state, which reveals when you are studying, so treat it like a password.

## What this app stores

Locally, under `%APPDATA%\StudyLifeTray\settings.json`, never transmitted anywhere except back
to your own StudyAtrium server as part of authenticating the poll above:

- Your StudyAtrium server's base URL (plain text).
- Your Tray API key (obtained via the passkey-backed browser-consent connect flow - you never
  see or copy/paste the key itself). The key is encrypted with Windows DPAPI for your Windows
  user account before it is written, so the file alone does not reveal it, and another Windows
  user or a copy of the file on another machine cannot decrypt it. It does not protect against
  software running as your own Windows user, which can ask DPAPI to decrypt it. Settings files
  written by earlier versions held the key in plain text; they are converted the first time this
  version starts.

## What this app never does

- Never collects analytics, telemetry, or crash reports.
- Never contacts any server other than the one you explicitly configure.
- Never writes anything to your StudyAtrium account - the key it uses has no write scope.

## Deleting your data

- **Disconnect** in the app's settings window removes the stored key from `settings.json` (the
  server URL stays so you can reconnect). The app also disconnects itself if the server rejects
  the key.
- To remove everything, quit the app and delete the folder `%APPDATA%\StudyLifeTray`.
- Deleting local data does not invalidate the key. To revoke it, disconnect the Tray app in the external connections card on your StudyAtrium instance's Setup page.

## Source

This app is open source (AGPL-3.0): <https://github.com/lukislp/studylife-tray>.