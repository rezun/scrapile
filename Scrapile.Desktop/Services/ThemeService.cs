namespace Scrapile.Desktop.Services;

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Scrapile.Domain.Interfaces;

/// <summary>
/// Service for managing application theme (Light, Dark, System).
/// </summary>
public class ThemeService
{
    private readonly IMetadataStore _metadataStore;
    private static readonly string[] ThemeOrder = { "System", "Light", "Dark" };

    /// <summary>
    /// Gets the current theme setting.
    /// </summary>
    public string CurrentTheme { get; private set; } = "System";

    public ThemeService(IMetadataStore metadataStore)
    {
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
    }

    /// <summary>
    /// Initializes the theme service by loading the saved theme preference.
    /// </summary>
    public async Task InitializeAsync()
    {
        var metadata = await _metadataStore.LoadAsync();
        CurrentTheme = metadata.Theme ?? "System";
        ApplyTheme();
    }

    /// <summary>
    /// Sets the theme and persists the preference.
    /// </summary>
    /// <param name="theme">The theme to set: "Light", "Dark", or "System".</param>
    public async Task SetThemeAsync(string theme)
    {
        if (theme != "Light" && theme != "Dark" && theme != "System")
        {
            throw new ArgumentException("Invalid theme. Must be Light, Dark, or System.", nameof(theme));
        }

        CurrentTheme = theme;
        ApplyTheme();
        await SaveThemeAsync();
    }

    /// <summary>
    /// Cycles through themes: System -> Light -> Dark -> System.
    /// </summary>
    public async Task CycleThemeAsync()
    {
        var currentIndex = Array.IndexOf(ThemeOrder, CurrentTheme);
        var nextIndex = (currentIndex + 1) % ThemeOrder.Length;
        await SetThemeAsync(ThemeOrder[nextIndex]);
    }

    /// <summary>
    /// Converts the current theme string to an Avalonia ThemeVariant.
    /// </summary>
    public ThemeVariant GetThemeVariant()
    {
        return CurrentTheme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default  // "System" follows OS preference
        };
    }

    /// <summary>
    /// Applies the current theme to the application.
    /// </summary>
    private void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = GetThemeVariant();
        }
    }

    /// <summary>
    /// Saves the current theme preference to metadata.
    /// </summary>
    private async Task SaveThemeAsync()
    {
        var metadata = await _metadataStore.LoadAsync();
        metadata.Theme = CurrentTheme;
        await _metadataStore.SaveAsync(metadata);
    }
}
