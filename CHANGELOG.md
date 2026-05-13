# Changelog

All notable changes to Sts2HostObserver are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0-prealpha] - 2026-05-13

### Added — Phase 1+2+3 (spectator view)
- **`LocalContextNetIdRedirectPatch` (keystone).** Harmony Prefix on the `LocalContext.NetId` getter. When `SpectatorState.SpectatedPlayerNetId` is set, returns that NetId instead of the host's own. Every `LocalContext.GetMe(...)` call in the codebase funnels through this property, so combat / hand / event / merchant / rest / treasure / map UI all silently follow the spectated client's view — no per-screen null-safety patches required (as long as a valid client is spectated).
- **`LocalContextNetIdSetterCapturePatch`.** Postfix on the setter, captures the real host NetId the moment `RunManager.Launch` writes it. Lets `SpectatorState.RealHostNetId` exclude the host from the spectatable list.
- **`SpectatorState`.** Tracks `SpectatedPlayerNetId` + `RealHostNetId`, fires `OnSpectatedChanged` event, provides `TryAutoSpectateFromCurrentRun()` (picks first non-host player from `RunManager.Instance.State.Players`) and `CycleNext(...)` for round-robin.
- **Lazy auto-spectate.** First `LocalContext.NetId` read after a client joins automatically picks the first non-host player — the host sees Player 1's view by default, no manual key required.
- **`SpectatorInputHandler`.** Godot Node attached to `tree.Root` listening for unhandled input. **Tab** key cycles to the next non-host client.
- **`SpectatorStatusOverlay`.** Top-right HUD label showing `[HostObserver] watching: <CharacterName>  (Tab to cycle)`. Auto-refreshes on `OnSpectatedChanged` and once per second from `_Process`.

### Verified live (single-instance)
- Build clean, mod loads, `Harmony patches applied.` confirmed.
- ObserverState round-trip via `observer.json`.

### Not yet
- **Live two-instance test** is the big one — confirms `LocalContext.NetId` redirect actually causes combat/event/etc UI to follow the spectated client. All four patches and the input/overlay layer are written and compile, but render behavior needs eyes-on.
- **Lobby UI before first client joins.** `_lobby.LocalPlayer` resolves via `NetService.NetId` directly (not via `LocalContext`), so our redirect doesn't fix that path. If a lobby screen reads `_lobby.LocalPlayer.character.X` it can still NRE in the empty-lobby moment.
- **i18n.** Log lines English-only; status overlay text English-only. Will follow Sts2MultiplayerSync's 16-locale pattern in a later version.
- **Tab key conflict resolution.** If STS2's own UI consumes Tab first, our handler doesn't fire. Rebindable keybinding pending.

## [0.1.0-prealpha] - 2026-05-13

### Added (scaffold only)
- **`SkipAddLocalHostPlayerPatch`** — Harmony Prefix on `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` and `AddLocalHostPlayerInternal(SerializableUnlockState, int)`. When `ObserverState.Enabled` is true, returns `null` and skips the original, leaving the lobby's `Players` list without a host entry.
- **`LocalContextGetMePatch`** — Harmony Finalizer on `LocalContext.GetMe`. Catches `InvalidOperationException("Local player not found")` that fires when host has no `Player` in `RunState.Players`, sets `__result = null`, and returns `null` (swallowing the exception).
- **`ObserverState`** — boot-time toggle loaded from `<user_data>/Sts2HostObserver/observer.json` (`{ "enabled": false }` by default).
- **`MainFile.Initialize`** — registers Harmony, loads ObserverState, logs status.

### Coexistence
- Patches don't overlap with [Sts2MultiplayerSync](https://github.com/ing-gom/Sts2MultiplayerSync), [Sts2SkinManager](https://github.com/ing-gom/Sts2SkinManager), or other Sts2* sister mods.
- This mod is `affects_gameplay=false` — does not contribute to client/host mod-mismatch checks.
