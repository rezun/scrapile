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
    private readonly TabWithStats _tabWithStats;
    private readonly Action<TabItemViewModel> _onCloseRequested;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Creates a new TabItemViewModel.
    /// </summary>
    /// <param name="tabWithStats">The tab data with stats.</param>
    /// <param name="onCloseRequested">Callback when close is requested.</param>
    public TabItemViewModel(TabWithStats tabWithStats, Action<TabItemViewModel> onCloseRequested)
    {
        _tabWithStats = tabWithStats ?? throw new ArgumentNullException(nameof(tabWithStats));
        _onCloseRequested = onCloseRequested ?? throw new ArgumentNullException(nameof(onCloseRequested));
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
    /// The display name for the tab (title if set, otherwise content preview).
    /// </summary>
    public string DisplayName => _tabWithStats.DisplayName;

    /// <summary>
    /// Whether this tab has a user-provided title.
    /// </summary>
    public bool HasTitle => _tabWithStats.HasTitle;

    /// <summary>
    /// The formatted word count (e.g., "245 words").
    /// </summary>
    public string FormattedWordCount => _tabWithStats.FormattedWordCount;

    /// <summary>
    /// Whether the tab has unsaved changes.
    /// </summary>
    public bool IsDirty => _tabWithStats.Tab.IsDirty;

    /// <summary>
    /// The underlying TabWithStats data.
    /// </summary>
    public TabWithStats TabWithStats => _tabWithStats;

    /// <summary>
    /// Command to close this tab.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _onCloseRequested(this);
    }

    /// <summary>
    /// Updates this view model with new tab data.
    /// </summary>
    /// <param name="tabWithStats">The updated tab data.</param>
    /// <returns>A new TabItemViewModel with the updated data.</returns>
    public TabItemViewModel WithUpdatedStats(TabWithStats tabWithStats)
    {
        return new TabItemViewModel(tabWithStats, _onCloseRequested)
        {
            IsSelected = this.IsSelected
        };
    }
}
