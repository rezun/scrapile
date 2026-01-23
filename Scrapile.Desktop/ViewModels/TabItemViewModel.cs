namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.DTOs;

/// <summary>
/// ViewModel for an individual tab item in the tab list.
/// </summary>
public partial class TabItemViewModel : ViewModelBase
{
    private readonly Action<TabItemViewModel> _onCloseRequested;
    private TabWithStats _tabWithStats;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _hasTitle;

    [ObservableProperty]
    private string _formattedWordCount = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Creates a new TabItemViewModel.
    /// </summary>
    /// <param name="tabWithStats">The tab data with stats.</param>
    /// <param name="onCloseRequested">Callback when close is requested.</param>
    public TabItemViewModel(TabWithStats tabWithStats, Action<TabItemViewModel> onCloseRequested)
    {
        _tabWithStats = tabWithStats ?? throw new ArgumentNullException(nameof(tabWithStats));
        _onCloseRequested = onCloseRequested ?? throw new ArgumentNullException(nameof(onCloseRequested));
        UpdatePropertiesFromTabWithStats();
    }

    /// <summary>
    /// The underlying tab ID.
    /// </summary>
    public Guid TabId => _tabWithStats.Tab.TabId;

    /// <summary>
    /// The document ID associated with this tab.
    /// </summary>
    public Guid DocumentId => _tabWithStats.Tab.Document.Id;

    /// <summary>
    /// The underlying TabWithStats data.
    /// </summary>
    public TabWithStats TabWithStats => _tabWithStats;

    /// <summary>
    /// Updates this view model with new tab data in place.
    /// This preserves the object identity, keeping context menus and other UI elements attached.
    /// </summary>
    /// <param name="tabWithStats">The updated tab data.</param>
    public void UpdateFrom(TabWithStats tabWithStats)
    {
        _tabWithStats = tabWithStats ?? throw new ArgumentNullException(nameof(tabWithStats));
        UpdatePropertiesFromTabWithStats();
    }

    /// <summary>
    /// Updates observable properties from the current TabWithStats data.
    /// </summary>
    private void UpdatePropertiesFromTabWithStats()
    {
        DisplayName = _tabWithStats.DisplayName;
        HasTitle = _tabWithStats.HasTitle;
        FormattedWordCount = _tabWithStats.FormattedWordCount;
        IsDirty = _tabWithStats.Tab.IsDirty;
    }

    /// <summary>
    /// Command to close this tab.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _onCloseRequested(this);
    }
}
