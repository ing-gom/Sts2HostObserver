using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using Sts2HostObserver.Runtime;

namespace Sts2HostObserver.Patches;

[HarmonyPatch(typeof(LocalContext), nameof(LocalContext.GetMe))]
public static class LocalContextGetMePatch
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception __exception, ref Player __result)
    {
        if (!ObserverState.Enabled) return __exception;
        if (__exception is InvalidOperationException ioex &&
            ioex.Message.Contains("Local player not found"))
        {
            MainFile.Logger.Info("observer mode ON — swallowed 'Local player not found' from LocalContext.GetMe");
            __result = null!;
            return null;
        }
        return __exception;
    }
}
