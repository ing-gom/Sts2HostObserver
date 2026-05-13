using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2HostObserver.Runtime;

public static class SpectatorState
{
    public static ulong? SpectatedPlayerNetId { get; private set; }

    public static ulong? RealHostNetId { get; private set; }

    public static event Action<ulong?>? OnSpectatedChanged;

    public static bool IsActive => ObserverState.Enabled && SpectatedPlayerNetId.HasValue;

    public static bool TryAutoSpectateFromCurrentRun()
    {
        if (SpectatedPlayerNetId.HasValue) return true;
        try
        {
            var run = RunManager.Instance;
            var players = run?.State?.Players;
            if (players == null || players.Count == 0) return false;
            var hostId = RealHostNetId;
            foreach (var p in players)
            {
                if (hostId.HasValue && p.NetId == hostId.Value) continue;
                SpectatePlayer(p.NetId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"TryAutoSpectateFromCurrentRun error: {ex.Message}");
            return false;
        }
    }

    public static void RecordRealHostNetId(ulong netId)
    {
        RealHostNetId = netId;
        MainFile.Logger.Info($"recorded real host NetId = {netId}");
    }

    public static void SpectatePlayer(ulong netId)
    {
        if (!ObserverState.Enabled) return;
        if (SpectatedPlayerNetId == netId) return;
        SpectatedPlayerNetId = netId;
        MainFile.Logger.Info($"now spectating NetId={netId}");
        try { OnSpectatedChanged?.Invoke(netId); }
        catch (Exception ex) { MainFile.Logger.Warn($"OnSpectatedChanged handler error: {ex.Message}"); }
    }

    public static void ClearSpectated()
    {
        if (SpectatedPlayerNetId == null) return;
        SpectatedPlayerNetId = null;
        MainFile.Logger.Info("cleared spectated player");
        try { OnSpectatedChanged?.Invoke(null); }
        catch (Exception ex) { MainFile.Logger.Warn($"OnSpectatedChanged handler error: {ex.Message}"); }
    }

    public static ulong? PickFirstClient(IEnumerable<ulong> peerNetIds)
    {
        if (!ObserverState.Enabled) return null;
        var hostId = RealHostNetId;
        foreach (var id in peerNetIds.OrderBy(x => x))
        {
            if (hostId.HasValue && id == hostId.Value) continue;
            return id;
        }
        return null;
    }

    public static ulong? CycleNext(IEnumerable<ulong> peerNetIds)
    {
        if (!ObserverState.Enabled) return SpectatedPlayerNetId;
        var hostId = RealHostNetId;
        var others = peerNetIds.Where(id => !hostId.HasValue || id != hostId.Value).OrderBy(x => x).ToList();
        if (others.Count == 0) return SpectatedPlayerNetId;
        if (!SpectatedPlayerNetId.HasValue) return others[0];
        var idx = others.IndexOf(SpectatedPlayerNetId.Value);
        var next = idx < 0 ? others[0] : others[(idx + 1) % others.Count];
        SpectatePlayer(next);
        return next;
    }
}
