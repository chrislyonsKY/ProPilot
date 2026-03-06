using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ProPilot.Models;

namespace ProPilot.Services;

/// <summary>
/// Manages ProPilot settings with project-level storage and user-level fallback.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly string UserSettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProPilot");

    private static readonly string UserSettingsPath = Path.Combine(
        UserSettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public ProPilotSettings LoadSettings()
    {
        // 1. Try user-level settings file
        try
        {
            if (File.Exists(UserSettingsPath))
            {
                var json = File.ReadAllText(UserSettingsPath);
                var settings = JsonSerializer.Deserialize<ProPilotSettings>(json, JsonOptions);
                if (settings != null)
                {
                    Debug.WriteLine($"[ProPilot] Settings loaded from {UserSettingsPath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Failed to load user settings: {ex.Message}");
        }

        // 2. Fallback to hardcoded defaults
        Debug.WriteLine("[ProPilot] Using default settings.");
        return new ProPilotSettings();
    }

    /// <inheritdoc />
    public void SaveSettings(ProPilotSettings settings, bool saveToProject = true)
    {
        // Always save to user-level file as the primary persistence
        try
        {
            Directory.CreateDirectory(UserSettingsDir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(UserSettingsPath, json);
            Debug.WriteLine($"[ProPilot] Settings saved to {UserSettingsPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Failed to save settings: {ex.Message}");
        }
    }
}
