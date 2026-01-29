using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.ViewModels;

using AvaloniaKey = Avalonia.Input.Key;
using AvaloniaKeyModifiers = Avalonia.Input.KeyModifiers;

namespace Scrapile.Desktop.Views;

/// <summary>
/// First-run welcome window code-behind.
/// </summary>
public partial class WelcomeWindow : Window
{
    private TaskCompletionSource<string>? _completionSource;

    /// <summary>
    /// Gets the selected storage directory after the window closes.
    /// </summary>
    public string? SelectedStorageDirectory { get; private set; }

    /// <summary>
    /// Gets whether autorun at startup was selected.
    /// </summary>
    public bool AutorunAtStartup { get; private set; }

    /// <summary>
    /// Gets the configured global shortcut.
    /// </summary>
    public string? GlobalShortcut { get; private set; }

    public WelcomeWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is WelcomeViewModel viewModel)
        {
            viewModel.FolderBrowserRequested += OnFolderBrowserRequested;
            viewModel.ContinueRequested += OnContinueRequested;
        }
    }

    /// <summary>
    /// Shows the welcome window and waits for the user to select a storage directory.
    /// </summary>
    /// <returns>The selected storage directory path.</returns>
    public Task<string> ShowAndGetResultAsync()
    {
        _completionSource = new TaskCompletionSource<string>();

        Closed += (_, _) =>
        {
            var viewModel = DataContext as WelcomeViewModel;
            var directory = SelectedStorageDirectory ?? viewModel?.StorageDirectory ?? string.Empty;
            _completionSource.TrySetResult(directory);
        };

        Show();
        Activate();

        return _completionSource.Task;
    }

    private async void OnFolderBrowserRequested(object? sender, FolderBrowserEventArgs e)
    {
        var viewModel = DataContext as WelcomeViewModel;
        if (viewModel == null) return;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Document Storage Directory",
            AllowMultiple = false
        };

        // Try to set initial folder if provided
        if (!string.IsNullOrEmpty(e.InitialDirectory) && Directory.Exists(e.InitialDirectory))
        {
            try
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(e.InitialDirectory);
                if (folder != null)
                {
                    options.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // Ignore if we can't get the folder
            }
        }

        var result = await StorageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
        {
            var selectedPath = result[0].Path.LocalPath;
            viewModel.SetStorageDirectory(selectedPath);
        }
    }

    private void OnContinueRequested(object? sender, EventArgs e)
    {
        var viewModel = DataContext as WelcomeViewModel;
        SelectedStorageDirectory = viewModel?.StorageDirectory;
        AutorunAtStartup = viewModel?.AutorunAtStartup ?? false;
        GlobalShortcut = viewModel?.GlobalShortcut;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var viewModel = DataContext as WelcomeViewModel;

        // Handle shortcut recording
        if (viewModel?.IsRecordingShortcut == true)
        {
            e.Handled = true;

            // Cancel on Escape
            if (e.Key == AvaloniaKey.Escape)
            {
                viewModel.CancelRecording();
                return;
            }

            // Ignore modifier-only presses
            if (IsModifierKey(e.Key))
            {
                return;
            }

            // Need at least one modifier
            var mods = e.KeyModifiers & (AvaloniaKeyModifiers.Control | AvaloniaKeyModifiers.Alt |
                                         AvaloniaKeyModifiers.Shift | AvaloniaKeyModifiers.Meta);
            if (mods == AvaloniaKeyModifiers.None)
            {
                return;
            }

            // Format the shortcut
            var shortcut = FormatShortcut(mods, e.Key);
            viewModel.StopRecording(shortcut);
        }
    }

    private static bool IsModifierKey(AvaloniaKey key)
    {
        return key == AvaloniaKey.LeftCtrl || key == AvaloniaKey.RightCtrl ||
               key == AvaloniaKey.LeftAlt || key == AvaloniaKey.RightAlt ||
               key == AvaloniaKey.LeftShift || key == AvaloniaKey.RightShift ||
               key == AvaloniaKey.LWin || key == AvaloniaKey.RWin;
    }

    private static string FormatShortcut(AvaloniaKeyModifiers modifiers, AvaloniaKey key)
    {
        var parts = new List<string>();
        var isMac = OperatingSystem.IsMacOS();

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Alt))
        {
            parts.Add(isMac ? "Option" : "Alt");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(AvaloniaKeyModifiers.Meta))
        {
            parts.Add(isMac ? "Cmd" : "Win");
        }

        // Get key name
        var keyName = key switch
        {
            AvaloniaKey.Space => "Space",
            AvaloniaKey.Enter => "Enter",
            AvaloniaKey.Tab => "Tab",
            AvaloniaKey.Escape => "Escape",
            AvaloniaKey.Back => "Backspace",
            AvaloniaKey.Delete => "Delete",
            AvaloniaKey.Home => "Home",
            AvaloniaKey.End => "End",
            AvaloniaKey.PageUp => "PageUp",
            AvaloniaKey.PageDown => "PageDown",
            AvaloniaKey.Up => "Up",
            AvaloniaKey.Down => "Down",
            AvaloniaKey.Left => "Left",
            AvaloniaKey.Right => "Right",
            AvaloniaKey.F1 => "F1",
            AvaloniaKey.F2 => "F2",
            AvaloniaKey.F3 => "F3",
            AvaloniaKey.F4 => "F4",
            AvaloniaKey.F5 => "F5",
            AvaloniaKey.F6 => "F6",
            AvaloniaKey.F7 => "F7",
            AvaloniaKey.F8 => "F8",
            AvaloniaKey.F9 => "F9",
            AvaloniaKey.F10 => "F10",
            AvaloniaKey.F11 => "F11",
            AvaloniaKey.F12 => "F12",
            AvaloniaKey.OemTilde => "`",
            AvaloniaKey.OemMinus => "-",
            AvaloniaKey.OemPlus => "=",
            AvaloniaKey.OemOpenBrackets => "[",
            AvaloniaKey.OemCloseBrackets => "]",
            AvaloniaKey.OemPipe => "\\",
            AvaloniaKey.OemSemicolon => ";",
            AvaloniaKey.OemQuotes => "'",
            AvaloniaKey.OemComma => ",",
            AvaloniaKey.OemPeriod => ".",
            AvaloniaKey.OemQuestion => "/",
            _ => GetKeyName(key)
        };

        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private static string GetKeyName(AvaloniaKey key)
    {
        // Handle letter keys
        if (key >= AvaloniaKey.A && key <= AvaloniaKey.Z)
        {
            return ((char)('A' + (key - AvaloniaKey.A))).ToString();
        }

        // Handle number keys
        if (key >= AvaloniaKey.D0 && key <= AvaloniaKey.D9)
        {
            return ((char)('0' + (key - AvaloniaKey.D0))).ToString();
        }

        // Handle numpad
        if (key >= AvaloniaKey.NumPad0 && key <= AvaloniaKey.NumPad9)
        {
            return "Num" + ((char)('0' + (key - AvaloniaKey.NumPad0)));
        }

        return key.ToString();
    }
}
