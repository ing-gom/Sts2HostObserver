using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using Sts2HostObserver.Runtime;

namespace Sts2HostObserver.Patches;

// When ObserverState.Enabled and a spectated player is set, LocalContext.NetId
// transparently returns the spectated player's NetId. This is the keystone
// patch — every LocalContext.GetMe(...) call funnels through NetId, so once
// it returns a real client's id, the in-game UI follows that client's view
// automatically without per-screen patches.
[HarmonyPatch(typeof(LocalContext), nameof(LocalContext.NetId), MethodType.Getter)]
public static class LocalContextNetIdRedirectPatch
{
    public static bool Prefix(ref ulong? __result)
    {
        if (!ObserverState.Enabled) return true;
        if (!SpectatorState.SpectatedPlayerNetId.HasValue)
        {
            SpectatorState.TryAutoSpectateFromCurrentRun();
        }
        if (!SpectatorState.SpectatedPlayerNetId.HasValue) return true;
        __result = SpectatorState.SpectatedPlayerNetId.Value;
        return false;
    }
}

// Capture the real host NetId at the moment LocalContext.NetId is first set
// from NetService.NetId (RunManager.Launch). We need it to exclude the host
// from "list of clients to spectate" — the host's NetId still gets recorded
// internally even though our getter redirects it.
[HarmonyPatch(typeof(LocalContext), nameof(LocalContext.NetId), MethodType.Setter)]
public static class LocalContextNetIdSetterCapturePatch
{
    public static void Postfix(ulong? value)
    {
        if (!ObserverState.Enabled) return;
        if (value.HasValue) SpectatorState.RecordRealHostNetId(value.Value);
    }
}
