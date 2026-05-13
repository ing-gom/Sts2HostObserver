using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2HostObserver.Runtime;

public partial class SpectatorInputHandler : Node
{
    private const Key CycleKey = Key.Tab;

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!ObserverState.Enabled) return;
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;
        if (keyEvent.Keycode != CycleKey) return;

        try
        {
            var run = RunManager.Instance;
            var players = run?.State?.Players;
            if (players == null || players.Count == 0)
            {
                MainFile.Logger.Info("Tab pressed but no active run — no spectate cycle");
                return;
            }
            var netIds = players.Select(p => p.NetId).ToList();
            var next = SpectatorState.CycleNext(netIds);
            MainFile.Logger.Info($"Tab pressed — cycled to NetId={next}");
            GetViewport().SetInputAsHandled();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"input handler error: {ex.Message}");
        }
    }

    public static void Attach()
    {
        try
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
            {
                MainFile.Logger.Warn("no SceneTree — cannot attach SpectatorInputHandler");
                return;
            }
            Callable.From(() =>
            {
                try
                {
                    var existing = tree.Root.GetNodeOrNull<SpectatorInputHandler>("Sts2HostObserver_InputHandler");
                    if (existing != null) return;
                    var node = new SpectatorInputHandler { Name = "Sts2HostObserver_InputHandler" };
                    tree.Root.AddChild(node);
                    MainFile.Logger.Info($"SpectatorInputHandler attached — press {CycleKey} to cycle spectated player");
                }
                catch (Exception ex) { MainFile.Logger.Warn($"attach error: {ex.Message}"); }
            }).CallDeferred();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SpectatorInputHandler.Attach error: {ex.Message}");
        }
    }
}
