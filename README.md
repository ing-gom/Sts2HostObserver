# Sts2HostObserver

Run a Slay the Spire 2 multiplayer lobby as a **dedicated host** — open a session, watch up to 4 clients play, never sit in a player slot yourself.

🇰🇷 [한국어 README](README.ko.md)

> ⚠️ **v0.1.0 — pre-alpha scaffold.**
> Core Harmony patches are written but not live-tested. STS2 multiplayer needs two simultaneous instances to verify, and lobby UI / character select null-safety patches may still be needed once we see the exact NRE locations. This release exists to publish the scaffold; turn the feature on at your own risk.

## What it does

STS2's multiplayer always has the host occupy player slot 0. `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` is the explicit entry point that adds the host to `Players` during lobby init. This mod adds a Harmony Prefix that **skips that call when `ObserverState.Enabled == true`**, so the lobby boots with 0 host players and 4 free slots for clients.

Two patches:
- **`SkipAddLocalHostPlayerPatch`** — Prefix on `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` and `AddLocalHostPlayerInternal`. Returns `null` and skips the original.
- **`LocalContextGetMePatch`** — Harmony Finalizer on `LocalContext.GetMe`. Without a host player in `RunState.Players`, code paths that call `LocalContext.GetMe(state)` throw `InvalidOperationException("Local player not found")`. The finalizer swallows that exception in observer mode and returns `null` for the caller to handle.

The rest of STS2 surprisingly handles 4 clients well:
- `NRemoteLobbyPlayerContainer._nodes` is a dynamic list — 4 slots show automatically.
- `LobbyBeginRunMessage.playersInLobby = Players` — host-less `Players` propagates to `RunState.Players` for free.
- `ActChangeSynchronizer._readyPlayers.Count = RunState.Players.Count` — turn gate waits for 4 ready, not 5. No deadlock from missing host input.
- All in-game synchronizers (rest site / event / merchant / treasure) are netId-keyed, so the host's NetId silently does nothing.

## How to turn it on

1. Install the mod.
2. Launch STS2 once — the mod writes `<user_data>/Sts2HostObserver/observer.json` with `{ "enabled": false }`.
3. Quit STS2, edit `observer.json` to `{ "enabled": true }`, save.
4. Launch STS2 again, host a Standard or Daily multiplayer lobby. Up to 4 clients can join. Your screen will likely show empty UI where the host's character slot would have been — that's expected pre-alpha behavior.

## Known pre-alpha gaps

- **Lobby UI character slot may flicker / blank.** `_lobby.LocalPlayer` returns `default(LobbyPlayer)` (`id=0`, `character=null`), so any UI binding to `LocalPlayer.character.Name` etc. will need defensive null checks. Patches will be added as we discover them.
- **`RunManager.UpdateRichPresence()`, room completion handlers** — these call `LocalContext.GetMe(State).Character` without null-checks. The finalizer in this mod catches the throw, but the caller still gets a `null` Player so downstream code might NRE next.
- **No in-game toggle UI yet.** Toggle requires editing `observer.json` + restart.
- **Not tested with two real STS2 instances.** Code-only verification so far.

## Caveats

- This is a host-side mod only. Clients don't need it.
- It does NOT bypass the `affects_gameplay` mod-match check — pair with [Sts2MultiplayerSync](https://github.com/ing-gom/Sts2MultiplayerSync) if you want both observer-host and mismatched mod sets.
- The host's own `LocalContext.NetId` is still set, but with no matching `Player`, most "do something with my player" code paths will silently no-op or get null. Treat the host as a true spectator.

## License

MIT. See `LICENSE`.
