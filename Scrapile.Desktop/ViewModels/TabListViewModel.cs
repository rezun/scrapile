namespace Scrapile.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.DTOs;
using Scrapile.Application.Services;

/// <summary>
/// ViewModel for the vertical tab list component.
/// </summary>
public partial class TabListViewModel : ViewModelBase
{
    private readonly TabManager _tabManager;
    private readonly AutoSaveService? _autoSaveService;

    [ObservableProperty]
    private ObservableCollection<TabItemViewModel> _tabs = new();

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    /// <summary>
    /// Event raised when a tab is selected.
    /// </summary>
    public event EventHandler<TabItemViewModel?>? TabSelected;

    /// <summary>
    /// Event raised when the tab collection changes.
    /// </summary>
    public event EventHandler? TabsChanged;

    /// <summary>
    /// Creates a new TabListViewModel.
    /// </summary>
    /// <param name="tabManager">The tab manager service.</param>
    /// <param name="autoSaveService">The auto-save service (optional, for saving before close).</param>
    public TabListViewModel(TabManager tabManager, AutoSaveService? autoSaveService = null)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _autoSaveService = autoSaveService;
    }

    /// <summary>
    /// Whether there are any tabs open.
    /// </summary>
    public bool HasTabs => Tabs.Count > 0;

    /// <summary>
    /// Loads tabs from the TabManager.
    /// </summary>
    public void LoadTabs()
    {
        var openTabs = _tabManager.GetOpenTabs();
        Tabs.Clear();

        foreach (var tab in openTabs)
        {
            Tabs.Add(CreateTabItemViewModel(tab));
        }

        // Select the first tab if any exist and none is selected
        if (Tabs.Count > 0 && SelectedTab == null)
        {
            SelectTab(Tabs[0]);
        }

        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Creates a new tab.
    /// </summary>
    [RelayCommand]
    public async Task CreateNewTabAsync()
    {
        var newTab = await _tabManager.CreateTabAsync();
        var tabViewModel = CreateTabItemViewModel(newTab);
        Tabs.Add(tabViewModel);
        SelectTab(tabViewModel);
        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes a tab.
    /// Saves any unsaved content before closing.
    /// </summary>
    /// <param name="tabViewModel">The tab to close.</param>
    /// <param name="contentToSave">Optional content to save (used when closing via keyboard shortcut with current editor content).</param>
    public async Task CloseTabAsync(TabItemViewModel tabViewModel, string? contentToSave = null)
    {
        var index = Tabs.IndexOf(tabViewModel);
        var wasSelected = tabViewModel.IsSelected;

        // Save content before closing if tab is dirty
        if (_autoSaveService != null && tabViewModel.IsDirty)
        {
            // Use provided content if available (from editor), otherwise get from tab manager
            var content = contentToSave ?? _tabManager.GetTab(tabViewModel.TabId)?.Tab.Content ?? string.Empty;
            await _autoSaveService.SaveImmediatelyAsync(tabViewModel.DocumentId, content);
        }

        await _tabManager.CloseTabAsync(tabViewModel.TabId);
        Tabs.Remove(tabViewModel);

        // Select another tab if the closed one was selected
        if (wasSelected && Tabs.Count > 0)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            SelectTab(Tabs[newIndex]);
        }
        else if (Tabs.Count == 0)
        {
            SelectedTab = null;
            TabSelected?.Invoke(this, null);
        }

        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Selects a tab.
    /// </summary>
    /// <param name="tabViewModel">The tab to select.</param>
    public void SelectTab(TabItemViewModel? tabViewModel)
    {
        // Deselect all tabs
        foreach (var tab in Tabs)
        {
            tab.IsSelected = false;
        }

        // Select the specified tab
        if (tabViewModel != null)
        {
            tabViewModel.IsSelected = true;
        }

        SelectedTab = tabViewModel;
        TabSelected?.Invoke(this, tabViewModel);
    }

    /// <summary>
    /// Selects the next tab in the list (wraps around).
    /// </summary>
    public void SelectNextTab()
    {
        if (Tabs.Count == 0) return;

        var currentIndex = SelectedTab != null ? Tabs.IndexOf(SelectedTab) : -1;
        var nextIndex = (currentIndex + 1) % Tabs.Count;
        SelectTab(Tabs[nextIndex]);
    }

    /// <summary>
    /// Selects the previous tab in the list (wraps around).
    /// </summary>
    public void SelectPreviousTab()
    {
        if (Tabs.Count == 0) return;

        var currentIndex = SelectedTab != null ? Tabs.IndexOf(SelectedTab) : 0;
        var prevIndex = (currentIndex - 1 + Tabs.Count) % Tabs.Count;
        SelectTab(Tabs[prevIndex]);
    }

    /// <summary>
    /// Opens a document in a new tab or focuses it if already open.
    /// </summary>
    /// <param name="documentId">The document ID to open.</param>
    /// <returns>The tab view model, or null if document not found.</returns>
    public async Task<TabItemViewModel?> OpenDocumentAsync(Guid documentId)
    {
        // Check if already open
        var existingTab = Tabs.FirstOrDefault(t => t.DocumentId == documentId);
        if (existingTab != null)
        {
            SelectTab(existingTab);
            return existingTab;
        }

        // Open in new tab
        var newTab = await _tabManager.OpenDocumentInTabAsync(documentId);
        if (newTab == null) return null;

        var tabViewModel = CreateTabItemViewModel(newTab);
        Tabs.Add(tabViewModel);
        SelectTab(tabViewModel);
        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);
        return tabViewModel;
    }

    /// <summary>
    /// Updates the stats for a specific tab.
    /// </summary>
    /// <param name="tabId">The tab ID.</param>
    public void RefreshTabStats(Guid tabId)
    {
        var tabWithStats = _tabManager.GetTab(tabId);
        if (tabWithStats == null) return;

        var existingTab = Tabs.FirstOrDefault(t => t.TabId == tabId);
        if (existingTab == null) return;

        var index = Tabs.IndexOf(existingTab);
        var wasSelected = existingTab.IsSelected;

        var updatedTab = CreateTabItemViewModel(tabWithStats);
        updatedTab.IsSelected = wasSelected;

        Tabs[index] = updatedTab;

        if (wasSelected)
        {
            SelectedTab = updatedTab;
        }
    }

    /// <summary>
    /// Creates a TabItemViewModel from TabWithStats.
    /// </summary>
    private TabItemViewModel CreateTabItemViewModel(TabWithStats tabWithStats)
    {
        return new TabItemViewModel(tabWithStats, OnTabCloseRequested);
    }

    /// <summary>
    /// Handles tab close requests from TabItemViewModel.
    /// </summary>
    private async void OnTabCloseRequested(TabItemViewModel tabViewModel)
    {
        await CloseTabAsync(tabViewModel);
    }
}
