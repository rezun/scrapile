namespace Scrapile.Desktop.Services;

using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

/// <summary>
/// Service for managing the system tray icon and its context menu.
/// </summary>
public class TrayIconService : IDisposable
{
    private TrayIcon? _trayIcon;
    private NativeMenu? _trayMenu;
    private NativeMenuItem? _showHideMenuItem;
    private bool _isDisposed;
    private bool _isWindowVisible = true;

    /// <summary>
    /// Event raised when Show/Hide is clicked or tray icon is double-clicked.
    /// </summary>
    public event EventHandler? ShowHideRequested;

    /// <summary>
    /// Event raised when Settings is clicked.
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// Event raised when Quit is clicked.
    /// </summary>
    public event EventHandler? QuitRequested;

    public TrayIconService()
    {
    }

    /// <summary>
    /// Initializes the tray icon with menu items.
    /// </summary>
    public void Initialize()
    {
        if (_trayIcon != null)
        {
            return;
        }

        try
        {
            // Create menu items
            _showHideMenuItem = new NativeMenuItem("Hide Scrapile");
            _showHideMenuItem.Click += (_, _) => ShowHideRequested?.Invoke(this, EventArgs.Empty);

            var settingsMenuItem = new NativeMenuItem("Settings...");
            settingsMenuItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);

            var quitMenuItem = new NativeMenuItem("Quit Scrapile");
            quitMenuItem.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);

            // Create menu
            _trayMenu = new NativeMenu();
            _trayMenu.Items.Add(_showHideMenuItem);
            _trayMenu.Items.Add(new NativeMenuItemSeparator());
            _trayMenu.Items.Add(settingsMenuItem);
            _trayMenu.Items.Add(new NativeMenuItemSeparator());
            _trayMenu.Items.Add(quitMenuItem);

            // Create tray icon
            _trayIcon = new TrayIcon
            {
                ToolTipText = "Scrapile",
                Menu = _trayMenu,
                IsVisible = true
            };

            // Load icon
            LoadIcon();

            // Handle clicks
            _trayIcon.Clicked += OnTrayIconClicked;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize tray icon: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the Show/Hide menu item text based on window visibility.
    /// </summary>
    /// <param name="isVisible">Whether the window is currently visible.</param>
    public void UpdateWindowVisibility(bool isVisible)
    {
        _isWindowVisible = isVisible;
        if (_showHideMenuItem != null)
        {
            _showHideMenuItem.Header = isVisible ? "Hide Scrapile" : "Show Scrapile";
        }
    }

    /// <summary>
    /// Sets the tray icon visibility.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = visible;
        }
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        // Single click on tray icon toggles visibility
        ShowHideRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadIcon()
    {
        if (_trayIcon == null)
        {
            return;
        }

        try
        {
            // Try to load the app icon from resources using the AssetLoader
            var uri = new Uri("avares://Scrapile/Assets/app-icon.png");
            using var stream = AssetLoader.Open(uri);
            var bitmap = new Bitmap(stream);

            // Create a WindowIcon which TrayIcon can use
            _trayIcon.Icon = new WindowIcon(bitmap);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load tray icon: {ex.Message}");
            // Tray will work without an icon on most platforms
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_trayIcon != null)
        {
            _trayIcon.Clicked -= OnTrayIconClicked;
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayMenu = null;
        _showHideMenuItem = null;
    }
}
