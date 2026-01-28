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

    [ObservableProperty]
    private ObservableCollection<RecentlyClosedItemViewModel> _recentlyClosed = new();

    [ObservableProperty]
    private bool _isRecentlyClosedExpanded = false;

    /// <summary>
    /// Event raised when a tab is selected.
    /// </summary>
    public event EventHandler<TabItemViewModel?>? TabSelected;

    /// <summary>
    /// Event raised when the tab collection changes.
    /// </summary>
    public event EventHandler? TabsChanged;

    /// <summary>
    /// Event raised when a recently closed document should be reopened.
    /// </summary>
    public event EventHandler<Guid>? ReopenDocumentRequested;

    /// <summary>
    /// Event raised when a tab should be duplicated.
    /// </summary>
    public event EventHandler<TabItemViewModel>? DuplicateTabRequested;

    /// <summary>
    /// Event raised when the title editing should be focused.
    /// </summary>
    public event EventHandler? EditTitleRequested;

    /// <summary>
    /// Event raised when a tab's content should be copied to clipboard.
    /// </summary>
    public event EventHandler<TabItemViewModel>? CopyToClipboardRequested;

    /// <summary>
    /// Event raised when a tab's content should be saved to a file.
    /// </summary>
    public event EventHandler<TabItemViewModel>? SaveAsRequested;

    /// <summary>
    /// Event raised when the recently closed list changes (tab closed or reopened).
    /// </summary>
    public event EventHandler? RecentlyClosedChanged;

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
    /// Whether there are any recently closed items.
    /// </summary>
    public bool HasRecentlyClosed => RecentlyClosed.Count > 0;

    /// <summary>
    /// Loads tabs from the TabManager and restores the last active tab selection.
    /// </summary>
    public async Task LoadTabsAsync()
    {
        var openTabs = _tabManager.GetOpenTabs();
        Tabs.Clear();

        foreach (var tab in openTabs)
        {
            Tabs.Add(CreateTabItemViewModel(tab));
        }

        // Restore the last active tab selection
        TabItemViewModel? tabToSelect = null;

        var activeDocumentId = await _tabManager.GetActiveTabDocumentIdAsync();
        if (activeDocumentId.HasValue)
        {
            tabToSelect = Tabs.FirstOrDefault(t => t.DocumentId == activeDocumentId.Value);
        }

        // Fall back to first tab if active tab not found
        if (tabToSelect == null && Tabs.Count > 0)
        {
            tabToSelect = Tabs[0];
        }

        if (tabToSelect != null)
        {
            SelectTab(tabToSelect);
        }

        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Loads tabs from the TabManager (synchronous version, selects first tab).
    /// Use LoadTabsAsync for restoring the active tab selection.
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
        if (tabViewModel == null) return;

        // Find the tab in collection by TabId (not by reference, since RefreshTabStats creates new instances)
        var tabInCollection = Tabs.FirstOrDefault(t => t.TabId == tabViewModel.TabId);
        if (tabInCollection == null)
        {
            return;
        }

        var index = Tabs.IndexOf(tabInCollection);
        var wasSelected = tabInCollection.IsSelected;

        // Remove from collection first to prevent duplicate close attempts
        Tabs.Remove(tabInCollection);

        // Save content before closing if tab is dirty
        if (_autoSaveService != null && tabViewModel.IsDirty)
        {
            // Use provided content if available (from editor), otherwise get from tab manager
            var content = contentToSave ?? _tabManager.GetTab(tabViewModel.TabId)?.Tab.Content ?? string.Empty;
            await _autoSaveService.SaveImmediatelyAsync(tabViewModel.DocumentId, content);
        }

        await _tabManager.CloseTabAsync(tabViewModel.TabId);

        // Select another tab if the closed one was selected
        // Recalculate safe index since collection may have changed during async operations
        if (wasSelected && Tabs.Count > 0)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            if (newIndex >= 0 && newIndex < Tabs.Count)
            {
                SelectTab(Tabs[newIndex]);
            }
        }
        else if (Tabs.Count == 0)
        {
            SelectedTab = null;
            TabSelected?.Invoke(this, null);
        }

        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);

        // Notify that recently closed list has changed
        RecentlyClosedChanged?.Invoke(this, EventArgs.Empty);

        // Refresh recently closed list if expanded
        if (IsRecentlyClosedExpanded)
        {
            await LoadRecentlyClosedAsync();
        }
    }

    /// <summary>
    /// Closes all open tabs.
    /// </summary>
    public async Task CloseAllTabsAsync()
    {
        // Close tabs from end to start to avoid index issues
        while (Tabs.Count > 0)
        {
            var tab = Tabs[Tabs.Count - 1];
            await CloseTabAsync(tab);
        }
    }

    /// <summary>
    /// Closes all tabs above the specified tab (lower index).
    /// </summary>
    /// <param name="tabViewModel">The reference tab (tabs above this will be closed).</param>
    public async Task CloseTabsAboveAsync(TabItemViewModel tabViewModel)
    {
        if (tabViewModel == null) return;

        var tabInCollection = Tabs.FirstOrDefault(t => t.TabId == tabViewModel.TabId);
        if (tabInCollection == null) return;

        var index = Tabs.IndexOf(tabInCollection);
        if (index <= 0) return; // No tabs above

        // Close tabs from just above the target down to index 0
        // Work backwards to avoid index shifting issues
        for (int i = index - 1; i >= 0; i--)
        {
            await CloseTabAsync(Tabs[i]);
        }
    }

    /// <summary>
    /// Closes all tabs below the specified tab (higher index).
    /// </summary>
    /// <param name="tabViewModel">The reference tab (tabs below this will be closed).</param>
    public async Task CloseTabsBelowAsync(TabItemViewModel tabViewModel)
    {
        if (tabViewModel == null) return;

        var tabInCollection = Tabs.FirstOrDefault(t => t.TabId == tabViewModel.TabId);
        if (tabInCollection == null) return;

        var index = Tabs.IndexOf(tabInCollection);
        if (index >= Tabs.Count - 1) return; // No tabs below

        // Close tabs from the end down to just below the target
        while (Tabs.Count > index + 1)
        {
            await CloseTabAsync(Tabs[Tabs.Count - 1]);
        }
    }

    /// <summary>
    /// Duplicates a tab.
    /// </summary>
    /// <param name="tabViewModel">The tab to duplicate.</param>
    public async Task DuplicateTabAsync(TabItemViewModel tabViewModel)
    {
        if (tabViewModel == null) return;

        var duplicatedTab = await _tabManager.DuplicateTabAsync(tabViewModel.TabId);
        if (duplicatedTab == null) return;

        // Find the insert position (after the original tab)
        var originalIndex = Tabs.IndexOf(Tabs.FirstOrDefault(t => t.TabId == tabViewModel.TabId) ?? tabViewModel);
        var insertIndex = originalIndex + 1;

        // Create the view model and insert at the correct position
        var newTabViewModel = CreateTabItemViewModel(duplicatedTab);
        if (insertIndex >= Tabs.Count)
        {
            Tabs.Add(newTabViewModel);
        }
        else
        {
            Tabs.Insert(insertIndex, newTabViewModel);
        }

        // Select the new tab
        SelectTab(newTabViewModel);
        OnPropertyChanged(nameof(HasTabs));
        TabsChanged?.Invoke(this, EventArgs.Empty);

        // Notify that a duplicate was created (for focusing editor)
        DuplicateTabRequested?.Invoke(this, newTabViewModel);
    }

    /// <summary>
    /// Requests focus on the title editing field for the selected tab.
    /// </summary>
    public void RequestEditTitle()
    {
        if (SelectedTab != null)
        {
            EditTitleRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Requests copying a tab's content to clipboard.
    /// </summary>
    /// <param name="tabViewModel">The tab to copy.</param>
    public void RequestCopyToClipboard(TabItemViewModel tabViewModel)
    {
        if (tabViewModel != null)
        {
            CopyToClipboardRequested?.Invoke(this, tabViewModel);
        }
    }

    /// <summary>
    /// Requests saving a tab's content to a file (Save As).
    /// </summary>
    /// <param name="tabViewModel">The tab to save.</param>
    public void RequestSaveAs(TabItemViewModel tabViewModel)
    {
        if (tabViewModel != null)
        {
            SaveAsRequested?.Invoke(this, tabViewModel);
        }
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
    /// Updates the stats for a specific tab in place.
    /// This preserves object identity, keeping context menus and other UI elements attached.
    /// </summary>
    /// <param name="tabId">The tab ID.</param>
    public void RefreshTabStats(Guid tabId)
    {
        var tabWithStats = _tabManager.GetTab(tabId);
        if (tabWithStats == null) return;

        var existingTab = Tabs.FirstOrDefault(t => t.TabId == tabId);
        if (existingTab == null) return;

        // Update in place instead of replacing - preserves UI element attachments
        existingTab.UpdateFrom(tabWithStats);
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

    /// <summary>
    /// Loads recently closed items from the TabManager.
    /// </summary>
    public async Task LoadRecentlyClosedAsync()
    {
        var items = await _tabManager.GetRecentlyClosedAsync();
        RecentlyClosed.Clear();

        foreach (var item in items.Where(i => !i.IsDeleted).Take(10))
        {
            RecentlyClosed.Add(new RecentlyClosedItemViewModel(item, OnReopenRequested));
        }

        OnPropertyChanged(nameof(HasRecentlyClosed));
    }

    /// <summary>
    /// Toggles the recently closed panel expansion state.
    /// </summary>
    [RelayCommand]
    public async Task ToggleRecentlyClosedAsync()
    {
        IsRecentlyClosedExpanded = !IsRecentlyClosedExpanded;

        if (IsRecentlyClosedExpanded)
        {
            await LoadRecentlyClosedAsync();
        }
    }

    /// <summary>
    /// Handles reopen requests from recently closed items.
    /// </summary>
    private void OnReopenRequested(RecentlyClosedItemViewModel item)
    {
        ReopenDocumentRequested?.Invoke(this, item.DocumentId);
    }
}
