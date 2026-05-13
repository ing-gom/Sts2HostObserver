using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Unlocks;
using Sts2HostObserver.Runtime;

namespace Sts2HostObserver.Patches;

[HarmonyPatch(typeof(StartRunLobby))]
public static class SkipAddLocalHostPlayerPatch
{
    [HarmonyPatch(nameof(StartRunLobby.AddLocalHostPlayer), new[] { typeof(UnlockState), typeof(int) })]
    [HarmonyPrefix]
    public static bool PrefixUnlock(ref LobbyPlayer? __result)
    {
        if (!ObserverState.Enabled) return true;
        MainFile.Logger.Info("observer mode ON — skipping AddLocalHostPlayer(UnlockState, int)");
        __result = null;
        return false;
    }

    [HarmonyPatch(nameof(StartRunLobby.AddLocalHostPlayerInternal))]
    [HarmonyPrefix]
    public static bool PrefixInternal(ref LobbyPlayer? __result)
    {
        if (!ObserverState.Enabled) return true;
        MainFile.Logger.Info("observer mode ON — skipping AddLocalHostPlayerInternal");
        __result = null;
        return false;
    }
}
