# Changelog

All notable changes to Sts2HostObserver are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-prealpha] - 2026-05-13

### Added (scaffold only)
- **`SkipAddLocalHostPlayerPatch`** — Harmony Prefix on `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` and `AddLocalHostPlayerInternal(SerializableUnlockState, int)`. When `ObserverState.Enabled` is true, returns `null` and skips the original, leaving the lobby's `Players` list without a host entry.
- **`LocalContextGetMePatch`** — Harmony Finalizer on `LocalContext.GetMe`. Catches `InvalidOperationException("Local player not found")` that fires when host has no `Player` in `RunState.Players`, sets `__result = null`, and returns `null` (swallowing the exception).
- **`ObserverState`** — boot-time toggle loaded from `<user_data>/Sts2HostObserver/observer.json` (`{ "enabled": false }` by default).
- **`MainFile.Initialize`** — registers Harmony, loads ObserverState, logs status.

### Not yet
- **Live two-instance test.** Code-only verification so far.
- **Lobby UI null-safety patches.** `_lobby.LocalPlayer.character` is `null` in observer mode; expect NREs in lobby UI that need defensive Prefix/Postfix patches once located.
- **In-game toggle UI.** Edit `observer.json` + restart for now.
- **i18n.** Single-language (English log lines + this README).

### Coexistence
- Patches don't overlap with [Sts2MultiplayerSync](https://github.com/ing-gom/Sts2MultiplayerSync), [Sts2SkinManager](https://github.com/ing-gom/Sts2SkinManager), or other Sts2* sister mods.
- This mod is `affects_gameplay=false` — does not contribute to client/host mod-mismatch checks.
