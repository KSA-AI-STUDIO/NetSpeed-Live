using System;
using System.IO;
using Microsoft.Win32;

namespace NetPulseOverlay.Services;

/// <summary>
/// Registers/unregisters the application for autostart using the current
/// user's <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> key —
/// no administrator privileges required.
///
/// Development guard: when the app runs from a build output folder
/// (<c>...\bin\Debug\...</c> or <c>...\bin\Release\...</c> without
/// <c>\publish\</c>), the path is a temporary development location and is
/// NEVER written to the registry. The published executable
/// (<c>...\publish\...</c> or any non-bin location) is the intended startup
/// target; the autostart preference is kept so reconciliation registers it as
/// soon as a published build runs.
///
/// All registry operations are wrapped: failures (policy, permissions,
/// corruption) are reported through return values and never crash the app.
/// </summary>
public sealed class StartupService
{
    /// <summary>Registry path of the per-user autostart key.</summary>
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Registry value name used for this application.</summary>
    public const string ValueName = "NetPulseOverlay";

    /// <summary>Gets the executable path that would be registered, or null when unknown.</summary>
    public string? ExecutablePath { get; }

    /// <summary>
    /// Gets a value indicating whether the current execution is a development
    /// build (bin output). Development paths are never registered.
    /// </summary>
    public bool IsDevelopmentExecution { get; }

    public StartupService()
    {
        (ExecutablePath, IsDevelopmentExecution) = ResolveStartupTarget();
    }

    /// <summary>Gets a value indicating whether a registry entry exists.</summary>
    public bool IsRegistered()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Gets the registered command line, or null when absent.</summary>
    public string? GetRegisteredCommand()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Enables or disables autostart and returns true when the registry is in
    /// the requested state afterwards. Enabling is refused for development
    /// executions so temporary paths are never registered. Called on startup
    /// with the persisted preference to reconcile the registry (heals moved
    /// executables, removes stale entries) and by the tray toggle.
    /// </summary>
    public bool SetEnabled(bool enabled)
    {
        try
        {
            if (!enabled)
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key is null)
                {
                    return true; // run key absent -> already unregistered
                }

                if (key.GetValue(ValueName) is not null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }

                return true;
            }

            if (IsDevelopmentExecution || string.IsNullOrWhiteSpace(ExecutablePath))
            {
                // Never register temporary development paths. The preference is
                // kept; reconciliation registers once a published build runs.
                return false;
            }

            using RegistryKey writeKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            writeKey.SetValue(ValueName, Quote(ExecutablePath!));
            return true;
        }
        catch (Exception)
        {
            // Registry failures must never crash the application.
            return false;
        }
    }

    /// <summary>Quotes the command so paths with spaces survive the Run key.</summary>
    private static string Quote(string path) => "\"" + path + "\"";

    private static (string? Path, bool IsDevelopment) ResolveStartupTarget()
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return (null, true);
            }

            string fullPath = Path.GetFullPath(exePath);
            bool inBuildOutput = fullPath.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase);
            bool inPublishOutput = fullPath.Contains(@"\publish\", StringComparison.OrdinalIgnoreCase);
            bool isDevelopment = inBuildOutput && !inPublishOutput;
            return (fullPath, isDevelopment);
        }
        catch (Exception)
        {
            return (null, true);
        }
    }
}