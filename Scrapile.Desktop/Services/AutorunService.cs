namespace Scrapile.Desktop.Services;

using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

/// <summary>
/// Service for managing application autorun at system startup.
/// Supports macOS (LaunchAgent), Windows (Registry), and Linux (XDG autostart).
/// </summary>
public class AutorunService
{
    private const string AppIdentifier = "com.scrapile.app";
    private const string AppName = "Scrapile";

    /// <summary>
    /// Gets or sets whether the application should run at startup.
    /// </summary>
    /// <param name="enabled">True to enable autorun, false to disable.</param>
    public void SetAutorunEnabled(bool enabled)
    {
        if (OperatingSystem.IsMacOS())
        {
            SetMacOSAutorun(enabled);
        }
        else if (OperatingSystem.IsWindows())
        {
            SetWindowsAutorun(enabled);
        }
        else if (OperatingSystem.IsLinux())
        {
            SetLinuxAutorun(enabled);
        }
    }

    /// <summary>
    /// Gets whether autorun is currently enabled at the OS level.
    /// </summary>
    public bool IsAutorunEnabled()
    {
        if (OperatingSystem.IsMacOS())
        {
            return IsMacOSAutorunEnabled();
        }
        else if (OperatingSystem.IsWindows())
        {
            return IsWindowsAutorunEnabled();
        }
        else if (OperatingSystem.IsLinux())
        {
            return IsLinuxAutorunEnabled();
        }

        return false;
    }

    private void SetMacOSAutorun(bool enabled)
    {
        var launchAgentsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents");
        var plistPath = Path.Combine(launchAgentsDir, $"{AppIdentifier}.plist");

        if (enabled)
        {
            // Ensure LaunchAgents directory exists
            Directory.CreateDirectory(launchAgentsDir);

            // Get the .app bundle path
            var appBundlePath = GetMacOSAppBundlePath();
            if (string.IsNullOrEmpty(appBundlePath))
            {
                return;
            }

            var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{AppIdentifier}</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/open</string>
        <string>-a</string>
        <string>{appBundlePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>";

            File.WriteAllText(plistPath, plistContent);
        }
        else
        {
            // Remove the plist file
            if (File.Exists(plistPath))
            {
                File.Delete(plistPath);
            }
        }
    }

    private bool IsMacOSAutorunEnabled()
    {
        var plistPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents", $"{AppIdentifier}.plist");
        return File.Exists(plistPath);
    }

    private string? GetMacOSAppBundlePath()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return null;
        }

        // Navigate from executable inside bundle to .app folder
        // Typical structure: /path/to/Scrapile.app/Contents/MacOS/Scrapile
        var dir = new DirectoryInfo(Path.GetDirectoryName(processPath)!);
        while (dir != null)
        {
            if (dir.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        // Not running from a .app bundle, return the executable path
        return processPath;
    }

    [SupportedOSPlatform("windows")]
    private void SetWindowsAutorun(bool enabled)
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore registry access errors
        }
    }

    [SupportedOSPlatform("windows")]
    private bool IsWindowsAutorunEnabled()
    {
        const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private void SetLinuxAutorun(bool enabled)
    {
        var autostartDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "autostart");
        var desktopFilePath = Path.Combine(autostartDir, "scrapile.desktop");

        if (enabled)
        {
            // Ensure autostart directory exists
            Directory.CreateDirectory(autostartDir);

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            var desktopContent = $@"[Desktop Entry]
Type=Application
Name={AppName}
Exec=""{exePath}""
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
";

            File.WriteAllText(desktopFilePath, desktopContent);
        }
        else
        {
            // Remove the desktop file
            if (File.Exists(desktopFilePath))
            {
                File.Delete(desktopFilePath);
            }
        }
    }

    private bool IsLinuxAutorunEnabled()
    {
        var desktopFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "autostart", "scrapile.desktop");
        return File.Exists(desktopFilePath);
    }
}
