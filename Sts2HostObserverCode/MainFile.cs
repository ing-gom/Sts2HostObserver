using System;
using System.IO;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Sts2HostObserver.Runtime;

namespace Sts2HostObserver;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Sts2HostObserver";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; }
        = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static string ManagerDataDir { get; private set; } = "";

    public static void Initialize()
    {
        try
        {
            var userDataDir = OS.GetUserDataDir();
            ManagerDataDir = Path.Combine(userDataDir, ModId);
            Directory.CreateDirectory(ManagerDataDir);

            ObserverState.LoadFromDisk(ManagerDataDir);
            Logger.Info($"observer.json loaded — Enabled={ObserverState.Enabled}. " +
                        $"Toggle via {Path.Combine(ManagerDataDir, "observer.json")} (edit \"enabled\": true).");

            ApplyHarmonyPatches();

            if (ObserverState.Enabled)
            {
                Logger.Warn("OBSERVER MODE ACTIVE — host will NOT take a player slot. " +
                            "Up to 4 clients can join. This is pre-alpha; expect rough edges in lobby UI / run start.");
                SpectatorInputHandler.Attach();
                SpectatorStatusOverlay.Attach();
            }
            else
            {
                Logger.Info("observer mode is OFF — Harmony patches registered but inert. " +
                            "Edit observer.json and restart to enable.");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"init failed: {ex}");
        }
    }

    private static void ApplyHarmonyPatches()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll(typeof(MainFile).Assembly);
        Logger.Info("Harmony patches applied.");
    }
}
