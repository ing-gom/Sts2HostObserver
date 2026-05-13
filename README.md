# Sts2HostObserver

Run a Slay the Spire 2 multiplayer lobby as a **dedicated host** — open a session, watch up to 4 clients play, never sit in a player slot yourself.

🇰🇷 [한국어 README](README.ko.md)

> ⚠️ **v0.1.0 — pre-alpha scaffold.**
> Core Harmony patches are written but not live-tested. STS2 multiplayer needs two simultaneous instances to verify, and lobby UI / character select null-safety patches may still be needed once we see the exact NRE locations. This release exists to publish the scaffold; turn the feature on at your own risk.

## What it does

STS2's multiplayer always has the host occupy player slot 0. `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` is the explicit entry point that adds the host to `Players` during lobby init. This mod (a) skips that call when `ObserverState.Enabled == true`, so the lobby boots with 0 host players and up to 4 free slots, and (b) **redirects `LocalContext.NetId` to one of the actual clients**, so the host's in-game UI follows that client's view as a true spectator — combat, hand, intent, event, rest site, merchant, treasure, map are all driven by `LocalContext.GetMe(...)` which now resolves to the spectated client.

Four patches + a Godot input/UI layer:
- **`SkipAddLocalHostPlayerPatch`** — Prefix on `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` and `AddLocalHostPlayerInternal`. Returns `null` and skips the original so `Players` stays free.
- **`LocalContextGetMePatch`** — Harmony Finalizer on `LocalContext.GetMe(...)`. If `LocalContext.NetId` still doesn't resolve (lobby phase before any client joins), the finalizer swallows the `"Local player not found"` throw and returns `null` for the caller.
- **`LocalContextNetIdRedirectPatch` (keystone)** — Harmony Prefix on the `LocalContext.NetId` getter. When `SpectatorState.SpectatedPlayerNetId` is set, returns that NetId instead of the host's. Every `GetMe()` call in the codebase funnels through this property, so the in-game UI silently follows the spectated client.
- **`LocalContextNetIdSetterCapturePatch`** — Postfix on the setter, captures the real host NetId the moment `RunManager.Launch` writes it (so `SpectatorState.RealHostNetId` knows who to exclude from the spectatable list).
- **`SpectatorInputHandler`** — Godot Node attached to `tree.Root`. Press **Tab** to cycle to the next non-host client.
- **`SpectatorStatusOverlay`** — small top-right label showing the currently-spectated client.

`SpectatorState` runs **lazy auto-spectate** — the first `LocalContext.NetId` read after a client joins automatically picks the first non-host player, so the host doesn't need to press Tab to see anything; they just see Player 1's view by default and can Tab around from there.

The rest of STS2 surprisingly handles 4 clients well:
- `NRemoteLobbyPlayerContainer._nodes` is a dynamic list — 4 slots show automatically.
- `LobbyBeginRunMessage.playersInLobby = Players` — host-less `Players` propagates to `RunState.Players` for free.
- `ActChangeSynchronizer._readyPlayers.Count = RunState.Players.Count` — turn gate waits for 4 ready, not 5. No deadlock from missing host input.
- All in-game synchronizers (rest site / event / merchant / treasure) are netId-keyed, so the host's NetId silently does nothing.
- `ActionQueueSynchronizer` already broadcasts every player's actions to every peer, so the host receives full state for spectating — we just had to route the rendering layer to one of the clients via `LocalContext.NetId`.

## How to turn it on

1. Install the mod.
2. Launch STS2 once — the mod writes `<user_data>/Sts2HostObserver/observer.json` with `{ "enabled": false }`.
3. Quit STS2, edit `observer.json` to `{ "enabled": true }`, save.
4. Launch STS2 again, host a Standard or Daily multiplayer lobby. Up to 4 clients can join. Your screen will likely show empty UI where the host's character slot would have been — that's expected pre-alpha behavior.

## Controls

- **Tab** — cycle to next non-host client to spectate.
- The top-right of the screen shows `[HostObserver] watching: <CharacterName>  (Tab to cycle)`.

## Known pre-alpha gaps

- **Lobby phase before any client joins.** Until a client joins, `LocalContext.NetId` has no valid client to redirect to → `GetMe` returns null → some lobby UI bindings (`_lobby.LocalPlayer.character.Name` etc.) may flicker or blank. Once one client joins, lazy auto-spectate kicks in and the host's view stabilizes.
- **`_lobby.LocalPlayer` is separate from `LocalContext.NetId`.** Our redirect doesn't touch `StartRunLobby.LocalPlayer` (it uses `NetService.NetId` directly, not via `LocalContext`). If a screen consults `_lobby.LocalPlayer` directly and accesses `.character` without a null check, it will NRE. Patches will be added as we discover them in real two-instance testing.
- **Tab key conflict.** Tab is unhandled-input — if a STS2 UI screen consumes Tab first, our handler won't fire. Rebindable in a future version.
- **Not tested with two real STS2 instances.** All verification is code-reading + single-instance boot so far.

## Caveats

- This is a host-side mod only. Clients don't need it.
- It does NOT bypass the `affects_gameplay` mod-match check — pair with [Sts2MultiplayerSync](https://github.com/ing-gom/Sts2MultiplayerSync) if you want both observer-host and mismatched mod sets.
- The host's own `LocalContext.NetId` is still set, but with no matching `Player`, most "do something with my player" code paths will silently no-op or get null. Treat the host as a true spectator.

## License

MIT. See `LICENSE`.
