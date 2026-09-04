using System;
using System.IO;
using System.Text.Json;
using NetPulseOverlay.Models;

namespace NetPulseOverlay.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in the user's roaming
/// application-data folder (<c>%APPDATA%\NetPulseOverlay\settings.json</c>).
///
/// Robustness:
/// - a missing file yields defaults
/// - a corrupt/unreadable file yields defaults instead of crashing
/// - saves are written to a temp file and moved into place, so an interrupted
///   write can never corrupt the previously stored settings
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    public SettingsService()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetPulseOverlay");
        SettingsFilePath = Path.Combine(directory, "settings.json");
    }

    /// <summary>Gets the full path of the settings file.</summary>
    public string SettingsFilePath { get; }

    /// <summary>Loads the settings; never throws.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsFilePath);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception)
        {
            // Corrupt settings must never prevent the app from running.
            return new AppSettings();
        }
    }

    /// <summary>Saves the settings atomically; never throws.</summary>
    public void Save(AppSettings settings)
    {
        string tempPath = SettingsFilePath + ".tmp";
        try
        {
            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsFilePath, true);
        }
        catch (Exception)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // Best-effort cleanup only; the previous settings file (if any)
                // remains intact.
            }
        }
    }
}