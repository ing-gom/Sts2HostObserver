using System;
using Godot;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2HostObserver.Runtime;

public partial class SpectatorStatusOverlay : Control
{
    private Label? _label;
    private const string NodeName = "Sts2HostObserver_StatusOverlay";

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorRight = 1.0f;
        AnchorBottom = 0.05f;
        OffsetTop = 8;
        OffsetRight = -16;

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_label);

        SpectatorState.OnSpectatedChanged += OnSpectatedChanged;
        Refresh();
    }

    public override void _ExitTree()
    {
        SpectatorState.OnSpectatedChanged -= OnSpectatedChanged;
    }

    private void OnSpectatedChanged(ulong? _) => Refresh();

    public override void _Process(double delta)
    {
        if (Engine.GetProcessFrames() % 60 != 0) return;
        Refresh();
    }

    private void Refresh()
    {
        if (_label == null) return;
        if (!ObserverState.Enabled)
        {
            _label.Text = "";
            Visible = false;
            return;
        }

        try
        {
            var run = RunManager.Instance;
            var spectatedId = SpectatorState.SpectatedPlayerNetId;
            if (run?.State == null || !spectatedId.HasValue)
            {
                _label.Text = "[HostObserver] waiting for run...";
                Visible = true;
                return;
            }
            var player = run.State.GetPlayer(spectatedId.Value);
            var label = player != null
                ? $"[HostObserver] watching: {player.Character?.GetType().Name ?? player.NetId.ToString()}  (Tab to cycle)"
                : $"[HostObserver] watching NetId={spectatedId}  (Tab to cycle)";
            _label.Text = label;
            Visible = true;
        }
        catch (Exception ex)
        {
            _label.Text = $"[HostObserver] error: {ex.Message}";
            Visible = true;
        }
    }

    public static void Attach()
    {
        try
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
            {
                MainFile.Logger.Warn("no SceneTree — cannot attach SpectatorStatusOverlay");
                return;
            }
            Callable.From(() =>
            {
                try
                {
                    var existing = tree.Root.GetNodeOrNull<SpectatorStatusOverlay>(NodeName);
                    if (existing != null) return;
                    var node = new SpectatorStatusOverlay { Name = NodeName };
                    tree.Root.AddChild(node);
                    MainFile.Logger.Info("SpectatorStatusOverlay attached");
                }
                catch (Exception ex) { MainFile.Logger.Warn($"attach error: {ex.Message}"); }
            }).CallDeferred();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"SpectatorStatusOverlay.Attach error: {ex.Message}");
        }
    }
}
