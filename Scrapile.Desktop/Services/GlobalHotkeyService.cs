namespace Scrapile.Desktop.Services;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Data;

/// <summary>
/// Service for managing global keyboard shortcuts using SharpHook.
/// Provides cross-platform support for Windows, macOS (with Accessibility permission), and Linux (X11).
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    private TaskPoolGlobalHook? _hook;
    private EventMask _registeredModifiers;
    private KeyCode _registeredKey;
    private string? _currentShortcut;
    private bool _isDisposed;

    /// <summary>
    /// Event raised when the registered hotkey is triggered.
    /// </summary>
    public event EventHandler? HotkeyTriggered;

    /// <summary>
    /// Known system shortcut conflicts by platform.
    /// </summary>
    private static readonly HashSet<string> CommonConflicts = new(StringComparer.OrdinalIgnoreCase)
    {
        // All platforms
        "Ctrl+C", "Ctrl+V", "Ctrl+X", "Ctrl+Z", "Ctrl+A", "Ctrl+S",
        "Cmd+C", "Cmd+V", "Cmd+X", "Cmd+Z", "Cmd+A", "Cmd+S",
        // Windows
        "Ctrl+Alt+Delete", "Ctrl+Shift+Esc", "Win+D", "Win+E", "Win+L", "Win+R",
        // macOS
        "Cmd+Space", "Cmd+Tab", "Cmd+Q", "Cmd+W", "Cmd+H", "Cmd+M",
        "Ctrl+Space", // Spotlight alternative
        // Linux common
        "Super+D", "Super+L", "Alt+F4"
    };

    public GlobalHotkeyService()
    {
    }

    /// <summary>
    /// Starts the global hook and registers the shortcut if one is configured.
    /// Only installs the low-level hook if a shortcut is actually set, to avoid
    /// interfering with system-wide keyboard input (e.g. Alt key behavior on Windows).
    /// </summary>
    public void Start(string? shortcut)
    {
        if (!string.IsNullOrEmpty(shortcut))
        {
            StartHook();
            RegisterHotkey(shortcut);
        }
    }

    private void StartHook()
    {
        if (_hook != null)
        {
            return;
        }

        try
        {
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _ = _hook.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start global hotkey hook: {ex.Message}");
            _hook = null;
        }
    }

    private void StopHook()
    {
        if (_hook == null)
        {
            return;
        }

        _hook.KeyPressed -= OnKeyPressed;

        var hook = _hook;
        _hook = null;
        Task.Run(() =>
        {
            try
            {
                hook.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        });
    }

    /// <summary>
    /// Registers a hotkey from a shortcut string.
    /// Starts the low-level hook if not already running, or stops it if the shortcut is cleared.
    /// </summary>
    /// <param name="shortcut">Shortcut string like "Ctrl+Alt+S" or "Cmd+Shift+Space"</param>
    /// <returns>True if registration succeeded, false otherwise.</returns>
    public bool RegisterHotkey(string? shortcut)
    {
        UnregisterHotkey();

        if (string.IsNullOrWhiteSpace(shortcut))
        {
            // No shortcut configured - stop the hook to avoid interfering with system input
            StopHook();
            return true;
        }

        if (!TryParseShortcut(shortcut, out var modifiers, out var key))
        {
            return false;
        }

        // Start the hook if not already running
        StartHook();

        _registeredModifiers = modifiers;
        _registeredKey = key;
        _currentShortcut = shortcut;
        return true;
    }

    /// <summary>
    /// Unregisters the current hotkey.
    /// </summary>
    public void UnregisterHotkey()
    {
        _registeredModifiers = EventMask.None;
        _registeredKey = KeyCode.VcUndefined;
        _currentShortcut = null;
    }

    /// <summary>
    /// Gets the currently registered shortcut string.
    /// </summary>
    public string? GetCurrentShortcut() => _currentShortcut;

    /// <summary>
    /// Checks if a shortcut conflicts with known system shortcuts.
    /// </summary>
    /// <param name="shortcut">The shortcut to check.</param>
    /// <returns>A warning message if conflict detected, null otherwise.</returns>
    public static string? CheckForConflicts(string? shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return null;
        }

        // Normalize the shortcut for comparison
        var normalized = NormalizeShortcut(shortcut);

        if (CommonConflicts.Contains(normalized))
        {
            return $"'{shortcut}' may conflict with a system shortcut.";
        }

        // Check for Win/Super key shortcuts on Windows/Linux
        if ((OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) &&
            (shortcut.Contains("Win+", StringComparison.OrdinalIgnoreCase) ||
             shortcut.Contains("Super+", StringComparison.OrdinalIgnoreCase)))
        {
            return "Shortcuts using Win/Super key may not work reliably.";
        }

        return null;
    }

    /// <summary>
    /// Checks if macOS Accessibility permission is granted.
    /// On other platforms, always returns true.
    /// </summary>
    public static bool HasAccessibilityPermission()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return true;
        }

        try
        {
            return AXIsProcessTrusted();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if running on Wayland (Linux only).
    /// Global hotkeys don't work on Wayland.
    /// </summary>
    public static bool IsWayland()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");

        return !string.IsNullOrEmpty(waylandDisplay) ||
               string.Equals(xdgSessionType, "wayland", StringComparison.OrdinalIgnoreCase);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (_registeredKey == KeyCode.VcUndefined)
        {
            return;
        }

        // Check if the key matches
        if (e.Data.KeyCode != _registeredKey)
        {
            return;
        }

        // Get current modifiers from the event and normalize side-specific modifiers
        var currentMods = e.RawEvent.Mask;
        var normalizedMods = NormalizeModifiers(currentMods);

        if (normalizedMods == _registeredModifiers)
        {
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Normalizes side-specific modifiers (LeftShift, RightShift, etc.) to generic modifiers (Shift, etc.)
    /// </summary>
    private static EventMask NormalizeModifiers(EventMask mask)
    {
        var result = EventMask.None;

        // Shift: check for LeftShift, RightShift, or generic Shift
        if ((mask & (EventMask.LeftShift | EventMask.RightShift | EventMask.Shift)) != 0)
        {
            result |= EventMask.Shift;
        }

        // Ctrl: check for LeftCtrl, RightCtrl, or generic Ctrl
        if ((mask & (EventMask.LeftCtrl | EventMask.RightCtrl | EventMask.Ctrl)) != 0)
        {
            result |= EventMask.Ctrl;
        }

        // Alt: check for LeftAlt, RightAlt, or generic Alt
        if ((mask & (EventMask.LeftAlt | EventMask.RightAlt | EventMask.Alt)) != 0)
        {
            result |= EventMask.Alt;
        }

        // Meta: check for LeftMeta, RightMeta, or generic Meta
        if ((mask & (EventMask.LeftMeta | EventMask.RightMeta | EventMask.Meta)) != 0)
        {
            result |= EventMask.Meta;
        }

        return result;
    }

    private static bool TryParseShortcut(string shortcut, out EventMask modifiers, out KeyCode key)
    {
        modifiers = EventMask.None;
        key = KeyCode.VcUndefined;

        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return false;
        }

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return false; // Need at least one modifier and a key
        }

        // Parse all parts except the last one as modifiers
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var mod = parts[i].ToLowerInvariant();
            switch (mod)
            {
                case "ctrl":
                case "control":
                    modifiers |= EventMask.Ctrl;
                    break;
                case "alt":
                case "option":
                    modifiers |= EventMask.Alt;
                    break;
                case "shift":
                    modifiers |= EventMask.Shift;
                    break;
                case "cmd":
                case "command":
                case "meta":
                case "win":
                case "super":
                    modifiers |= EventMask.Meta;
                    break;
                default:
                    return false; // Unknown modifier
            }
        }

        // Parse the key (last part)
        var keyPart = parts[^1].ToUpperInvariant();
        key = keyPart switch
        {
            "SPACE" => KeyCode.VcSpace,
            "ENTER" or "RETURN" => KeyCode.VcEnter,
            "TAB" => KeyCode.VcTab,
            "ESCAPE" or "ESC" => KeyCode.VcEscape,
            "BACKSPACE" => KeyCode.VcBackspace,
            "DELETE" or "DEL" => KeyCode.VcDelete,
            "HOME" => KeyCode.VcHome,
            "END" => KeyCode.VcEnd,
            "PAGEUP" or "PGUP" => KeyCode.VcPageUp,
            "PAGEDOWN" or "PGDN" => KeyCode.VcPageDown,
            "UP" => KeyCode.VcUp,
            "DOWN" => KeyCode.VcDown,
            "LEFT" => KeyCode.VcLeft,
            "RIGHT" => KeyCode.VcRight,
            "F1" => KeyCode.VcF1,
            "F2" => KeyCode.VcF2,
            "F3" => KeyCode.VcF3,
            "F4" => KeyCode.VcF4,
            "F5" => KeyCode.VcF5,
            "F6" => KeyCode.VcF6,
            "F7" => KeyCode.VcF7,
            "F8" => KeyCode.VcF8,
            "F9" => KeyCode.VcF9,
            "F10" => KeyCode.VcF10,
            "F11" => KeyCode.VcF11,
            "F12" => KeyCode.VcF12,
            "`" or "GRAVE" or "TILDE" => KeyCode.VcBackQuote,
            "-" or "MINUS" => KeyCode.VcMinus,
            "=" or "EQUALS" or "PLUS" => KeyCode.VcEquals,
            "[" or "OPENBRACKET" => KeyCode.VcOpenBracket,
            "]" or "CLOSEBRACKET" => KeyCode.VcCloseBracket,
            "\\" or "BACKSLASH" => KeyCode.VcBackslash,
            ";" or "SEMICOLON" => KeyCode.VcSemicolon,
            "'" or "QUOTE" or "APOSTROPHE" => KeyCode.VcQuote,
            "," or "COMMA" => KeyCode.VcComma,
            "." or "PERIOD" => KeyCode.VcPeriod,
            "/" or "SLASH" => KeyCode.VcSlash,
            _ => ParseLetterOrNumber(keyPart)
        };

        return key != KeyCode.VcUndefined && modifiers != EventMask.None;
    }

    private static KeyCode ParseLetterOrNumber(string keyPart)
    {
        if (keyPart.Length != 1)
        {
            return KeyCode.VcUndefined;
        }

        var c = keyPart[0];

        // Letters A-Z
        if (c >= 'A' && c <= 'Z')
        {
            return (KeyCode)((int)KeyCode.VcA + (c - 'A'));
        }

        // Numbers 0-9
        if (c >= '0' && c <= '9')
        {
            return (KeyCode)((int)KeyCode.Vc0 + (c - '0'));
        }

        return KeyCode.VcUndefined;
    }

    private static string NormalizeShortcut(string shortcut)
    {
        // Normalize for comparison - convert to consistent format
        var normalized = shortcut
            .Replace("Control", "Ctrl", StringComparison.OrdinalIgnoreCase)
            .Replace("Command", "Cmd", StringComparison.OrdinalIgnoreCase)
            .Replace("Option", "Alt", StringComparison.OrdinalIgnoreCase)
            .Replace("Super", "Win", StringComparison.OrdinalIgnoreCase)
            .Replace("Meta", "Cmd", StringComparison.OrdinalIgnoreCase);

        return normalized;
    }

    // macOS Accessibility API
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        StopHook();
    }
}
