using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.Services;
using Scrapile.Desktop.Services;

namespace Scrapile.Desktop.ViewModels;

/// <summary>
/// Main window view model that coordinates the application's primary UI.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TabManager _tabManager;
    private readonly DocumentService _documentService;
    private readonly AutoSaveService _autoSaveService;
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private string _title = "Scrapile";

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _hasTabs;

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private TabListViewModel _tabListViewModel;

    [ObservableProperty]
    private EditorViewModel _editorViewModel;

    [ObservableProperty]
    private SearchViewModel _searchViewModel;

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    /// <summary>
    /// Creates a new MainWindowViewModel with injected services.
    /// </summary>
    /// <param name="tabManager">The tab manager service.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="autoSaveService">The auto-save service.</param>
    /// <param name="themeService">The theme service.</param>
    public MainWindowViewModel(
        TabManager tabManager,
        DocumentService documentService,
        AutoSaveService autoSaveService,
        ThemeService themeService)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _autoSaveService = autoSaveService ?? throw new ArgumentNullException(nameof(autoSaveService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        // Create the tab list view model
        _tabListViewModel = new TabListViewModel(_tabManager, _autoSaveService);
        _tabListViewModel.TabSelected += OnTabSelected;
        _tabListViewModel.TabsChanged += OnTabsChanged;
        _tabListViewModel.ReopenDocumentRequested += OnReopenDocumentRequested;

        // Create the editor view model
        _editorViewModel = new EditorViewModel(_tabManager, _documentService, _autoSaveService);
        _editorViewModel.ContentChanged += OnEditorContentChanged;
        _editorViewModel.TitleChanged += OnEditorTitleChanged;

        // Create the search view model
        _searchViewModel = new SearchViewModel(_documentService);
        _searchViewModel.ResultSelected += OnSearchResultSelected;
        _searchViewModel.CloseRequested += OnSearchCloseRequested;

        // Subscribe to auto-save completion to update dirty state
        _autoSaveService.SaveCompleted += OnAutoSaveCompleted;
    }

    /// <summary>
    /// Initializes the view model by loading persisted state.
    /// Call this after the window is loaded.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        // Initialize theme service to apply saved theme preference
        await _themeService.InitializeAsync();

        // Initialize the tab manager to restore previous session
        await _tabManager.InitializeAsync();

        // Load tabs into the TabListViewModel and restore active tab selection
        await TabListViewModel.LoadTabsAsync();

        // Update HasTabs based on restored tabs
        UpdateHasTabs();

        IsInitialized = true;
    }

    /// <summary>
    /// Handles tab selection events from the tab list.
    /// Persists the active tab document ID for session restore.
    /// </summary>
    private async void OnTabSelected(object? sender, TabItemViewModel? tabViewModel)
    {
        SelectedTab = tabViewModel;
        EditorViewModel.CurrentTab = tabViewModel;

        // Persist the active tab selection for session restore
        var documentId = tabViewModel?.DocumentId;
        await _tabManager.SetActiveTabDocumentIdAsync(documentId);
    }

    /// <summary>
    /// Handles changes to the tab collection.
    /// </summary>
    private void OnTabsChanged(object? sender, EventArgs e)
    {
        UpdateHasTabs();
    }

    /// <summary>
    /// Handles content changes from the editor.
    /// Triggers auto-save for the document.
    /// </summary>
    private async void OnEditorContentChanged(object? sender, ContentChangedEventArgs e)
    {
        // Show save status indicator
        EditorViewModel.SaveStatus = "Saving...";

        // Schedule debounced auto-save
        // The AutoSaveService handles the debouncing and saving to disk
        await _autoSaveService.ScheduleSaveAsync(e.DocumentId, e.Content);

        // Refresh the tab stats in the list to show updated word count
        if (SelectedTab != null)
        {
            TabListViewModel.RefreshTabStats(SelectedTab.TabId);
        }
    }

    /// <summary>
    /// Handles title changes from the editor.
    /// Updates the document title in the metadata store and in-memory state.
    /// </summary>
    private async void OnEditorTitleChanged(object? sender, TitleChangedEventArgs e)
    {
        // Update the in-memory title first so RefreshTabStats sees the new value
        _tabManager.UpdateDocumentTitle(e.DocumentId, e.Title);

        // Persist the title to the metadata store
        await _documentService.UpdateTitleAsync(e.DocumentId, e.Title);

        // Refresh the tab in the list to update the display name
        if (SelectedTab != null)
        {
            TabListViewModel.RefreshTabStats(SelectedTab.TabId);
        }
    }

    /// <summary>
    /// Handles auto-save completion events.
    /// Resets the dirty state for the document that was saved.
    /// </summary>
    private async void OnAutoSaveCompleted(object? sender, SaveCompletedEventArgs e)
    {
        // Mark the tab as no longer dirty in the TabManager
        _tabManager.MarkTabSaved(e.DocumentId);

        // Update the editor's dirty state and save status if this is the current tab
        if (SelectedTab?.DocumentId == e.DocumentId)
        {
            EditorViewModel.SetDirty(false);
            EditorViewModel.SaveStatus = "Saved";

            // Clear the save status after a short delay
            await Task.Delay(1500);
            if (EditorViewModel.SaveStatus == "Saved")
            {
                EditorViewModel.SaveStatus = string.Empty;
            }
        }

        // Refresh the tab to update the dirty indicator
        var tabWithStats = _tabManager.GetOpenTabs().FirstOrDefault(t => t.Tab.Document.Id == e.DocumentId);
        if (tabWithStats != null)
        {
            TabListViewModel.RefreshTabStats(tabWithStats.Tab.TabId);
        }
    }

    /// <summary>
    /// Updates the HasTabs property based on the current tab count.
    /// </summary>
    private void UpdateHasTabs()
    {
        HasTabs = _tabManager.GetOpenTabs().Count > 0;
    }

    /// <summary>
    /// Creates a new tab.
    /// </summary>
    public Task CreateNewTabAsync() => TabListViewModel.CreateNewTabAsync();

    /// <summary>
    /// Closes the currently selected tab.
    /// Ensures any unsaved content is saved before closing.
    /// </summary>
    public async Task CloseCurrentTabAsync()
    {
        if (SelectedTab == null)
        {
            return;
        }

        // Pass the current editor content so we save the latest changes
        await TabListViewModel.CloseTabAsync(SelectedTab, EditorViewModel.Content);
    }

    /// <summary>
    /// Selects the next tab.
    /// </summary>
    public void SelectNextTab() => TabListViewModel.SelectNextTab();

    /// <summary>
    /// Selects the previous tab.
    /// </summary>
    public void SelectPreviousTab() => TabListViewModel.SelectPreviousTab();

    /// <summary>
    /// Reopens the most recently closed tab.
    /// Does nothing if the recently closed list is empty.
    /// </summary>
    /// <returns>True if a tab was reopened, false if none available.</returns>
    public async Task<bool> ReopenLastClosedAsync()
    {
        var reopenedTab = await _tabManager.ReopenLastClosedAsync();

        if (reopenedTab == null)
        {
            // No recently closed tabs available (list empty or all deleted)
            return false;
        }

        // Refresh the tab list to show the reopened tab
        await TabListViewModel.LoadTabsAsync();

        // Select the reopened tab
        var tabToSelect = TabListViewModel.Tabs.FirstOrDefault(t => t.DocumentId == reopenedTab.Tab.Document.Id);
        if (tabToSelect != null)
        {
            TabListViewModel.SelectTab(tabToSelect);
        }

        return true;
    }

    /// <summary>
    /// Sets the theme to Light.
    /// </summary>
    [RelayCommand]
    private Task SetLightTheme() => _themeService.SetThemeAsync("Light");

    /// <summary>
    /// Sets the theme to Dark.
    /// </summary>
    [RelayCommand]
    private Task SetDarkTheme() => _themeService.SetThemeAsync("Dark");

    /// <summary>
    /// Sets the theme to System (follows OS preference).
    /// </summary>
    [RelayCommand]
    private Task SetSystemTheme() => _themeService.SetThemeAsync("System");

    /// <summary>
    /// Cycles through themes: System -> Light -> Dark -> System.
    /// </summary>
    [RelayCommand]
    public Task CycleTheme() => _themeService.CycleThemeAsync();

    /// <summary>
    /// Saves all pending changes for dirty tabs.
    /// Call this before application shutdown to ensure no data loss.
    /// </summary>
    /// <returns>A task that completes when all saves are finished.</returns>
    public async Task SaveAllPendingChangesAsync()
    {
        // Get all dirty tabs that need saving
        var dirtyTabs = _tabManager.GetDirtyTabs();

        // Save each dirty tab immediately
        foreach (var tabWithStats in dirtyTabs)
        {
            var documentId = tabWithStats.Tab.Document.Id;
            var content = tabWithStats.Tab.Content;

            await _autoSaveService.SaveImmediatelyAsync(documentId, content);
        }
    }

    /// <summary>
    /// Shows the search overlay.
    /// </summary>
    public void ShowSearch()
    {
        SearchViewModel.Reset();
        IsSearchVisible = true;
    }

    /// <summary>
    /// Hides the search overlay.
    /// </summary>
    public void HideSearch()
    {
        IsSearchVisible = false;
    }

    /// <summary>
    /// Toggles the search overlay visibility.
    /// </summary>
    public void ToggleSearch()
    {
        if (IsSearchVisible)
        {
            HideSearch();
        }
        else
        {
            ShowSearch();
        }
    }

    /// <summary>
    /// Handles search result selection.
    /// Opens the selected document in a tab.
    /// </summary>
    private async void OnSearchResultSelected(object? sender, SearchResultItemViewModel result)
    {
        // Close the search overlay
        HideSearch();

        // Check if the document is already open in a tab
        var existingTab = TabListViewModel.Tabs.FirstOrDefault(t => t.DocumentId == result.DocumentId);
        if (existingTab != null)
        {
            // Select the existing tab
            TabListViewModel.SelectTab(existingTab);
        }
        else
        {
            // Open the document in a new tab
            await _tabManager.OpenDocumentInTabAsync(result.DocumentId);

            // Refresh the tab list
            await TabListViewModel.LoadTabsAsync();

            // Select the newly opened tab
            var newTab = TabListViewModel.Tabs.FirstOrDefault(t => t.DocumentId == result.DocumentId);
            if (newTab != null)
            {
                TabListViewModel.SelectTab(newTab);
            }
        }
    }

    /// <summary>
    /// Handles search close request.
    /// </summary>
    private void OnSearchCloseRequested(object? sender, EventArgs e)
    {
        HideSearch();
    }

    /// <summary>
    /// Handles reopen document requests from the recently closed panel.
    /// </summary>
    private async void OnReopenDocumentRequested(object? sender, Guid documentId)
    {
        // Open the document in a new tab
        var reopenedTab = await _tabManager.ReopenDocumentFromRecentlyClosedAsync(documentId);

        if (reopenedTab == null)
        {
            // Document not found or deleted
            // Refresh the recently closed list to remove any invalid entries
            await TabListViewModel.LoadRecentlyClosedAsync();
            return;
        }

        // Refresh the tab list
        await TabListViewModel.LoadTabsAsync();

        // Refresh the recently closed list to remove the reopened document
        await TabListViewModel.LoadRecentlyClosedAsync();

        // Select the reopened tab
        var tabToSelect = TabListViewModel.Tabs.FirstOrDefault(t => t.DocumentId == documentId);
        if (tabToSelect != null)
        {
            TabListViewModel.SelectTab(tabToSelect);
        }
    }
}
