using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Sts2HostObserver.Runtime;

public static class ObserverState
{
    private const string ConfigFileName = "observer.json";

    public static bool Enabled { get; private set; }

    public static void LoadFromDisk(string managerDataDir)
    {
        try
        {
            var path = Path.Combine(managerDataDir, ConfigFileName);
            if (!File.Exists(path))
            {
                WriteDefault(path);
                Enabled = false;
                return;
            }
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("enabled", out var prop) && prop.ValueKind == JsonValueKind.True)
            {
                Enabled = true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"failed to load observer.json: {ex.Message}");
            Enabled = false;
        }
    }

    public static void SetEnabled(string managerDataDir, bool enabled)
    {
        Enabled = enabled;
        try
        {
            var path = Path.Combine(managerDataDir, ConfigFileName);
            var json = JsonSerializer.Serialize(new { enabled }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"failed to write observer.json: {ex.Message}");
        }
    }

    private static void WriteDefault(string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { enabled = false }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"failed to write default observer.json: {ex.Message}");
        }
    }
}
