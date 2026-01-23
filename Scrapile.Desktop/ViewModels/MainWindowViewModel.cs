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
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _title = "Scrapile";

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _hasTabs;

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private bool _isTabListOnLeft = true;

    [ObservableProperty]
    private TabListViewModel _tabListViewModel;

    [ObservableProperty]
    private EditorViewModel _editorViewModel;

    [ObservableProperty]
    private SearchViewModel _searchViewModel;

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    /// <summary>
    /// Event raised when the title field should be focused.
    /// </summary>
    public event EventHandler? FocusTitleRequested;

    /// <summary>
    /// Event raised when the settings window should be opened.
    /// </summary>
    public event EventHandler? OpenSettingsRequested;

    /// <summary>
    /// Creates a new MainWindowViewModel with injected services.
    /// </summary>
    /// <param name="tabManager">The tab manager service.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="autoSaveService">The auto-save service.</param>
    /// <param name="themeService">The theme service.</param>
    /// <param name="settingsService">The settings service.</param>
    public MainWindowViewModel(
        TabManager tabManager,
        DocumentService documentService,
        AutoSaveService autoSaveService,
        ThemeService themeService,
        SettingsService settingsService)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _autoSaveService = autoSaveService ?? throw new ArgumentNullException(nameof(autoSaveService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Create the tab list view model
        _tabListViewModel = new TabListViewModel(_tabManager, _autoSaveService);
        _tabListViewModel.TabSelected += OnTabSelected;
        _tabListViewModel.TabsChanged += OnTabsChanged;
        _tabListViewModel.ReopenDocumentRequested += OnReopenDocumentRequested;
        _tabListViewModel.DuplicateTabRequested += OnDuplicateTabRequested;
        _tabListViewModel.EditTitleRequested += OnEditTitleRequested;
        _tabListViewModel.CopyToClipboardRequested += OnCopyToClipboardRequested;
        _tabListViewModel.SaveAsRequested += OnSaveAsRequested;

        // Create the editor view model
        _editorViewModel = new EditorViewModel(_tabManager, _documentService, _autoSaveService, _settingsService);
        _editorViewModel.ContentChanged += OnEditorContentChanged;
        _editorViewModel.TitleChanged += OnEditorTitleChanged;

        // Create the search view model
        _searchViewModel = new SearchViewModel(_documentService);
        _searchViewModel.ResultSelected += OnSearchResultSelected;
        _searchViewModel.CloseRequested += OnSearchCloseRequested;

        // Subscribe to auto-save completion to update dirty state
        _autoSaveService.SaveCompleted += OnAutoSaveCompleted;

        // Subscribe to settings changes for tab position
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    /// <summary>
    /// Handles settings change events to update layout properties.
    /// </summary>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.SettingName == "TabPosition" || e.SettingName == "All")
        {
            ApplyTabPositionSetting();
        }
    }

    /// <summary>
    /// Applies the current tab position setting.
    /// </summary>
    private void ApplyTabPositionSetting()
    {
        IsTabListOnLeft = _settingsService.GetTabPosition() == "Left";
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

        // Initialize settings service first (other services may depend on settings)
        await _settingsService.InitializeAsync();

        // Apply initial tab position setting
        ApplyTabPositionSetting();

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
    /// Opens the settings dialog.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the settings service for the settings window.
    /// </summary>
    public SettingsService SettingsService => _settingsService;

    /// <summary>
    /// Gets the theme service for the settings window.
    /// </summary>
    public ThemeService ThemeService => _themeService;

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

    /// <summary>
    /// Handles duplicate tab requests from the context menu.
    /// </summary>
    private void OnDuplicateTabRequested(object? sender, TabItemViewModel duplicatedTab)
    {
        // Focus the editor for the newly duplicated tab
        FocusTitleRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles edit title requests from the context menu.
    /// </summary>
    private void OnEditTitleRequested(object? sender, EventArgs e)
    {
        FocusTitleRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles copy to clipboard requests from the context menu.
    /// </summary>
    private void OnCopyToClipboardRequested(object? sender, TabItemViewModel tabViewModel)
    {
        // Get the content from the tab via TabManager
        var tabWithStats = _tabManager.GetOpenTabs().FirstOrDefault(t => t.Tab.TabId == tabViewModel.TabId);
        if (tabWithStats == null)
        {
            return;
        }

        var content = tabWithStats.Tab.Content;
        if (!string.IsNullOrEmpty(content))
        {
            ClipboardCopyRequested?.Invoke(this, content);
            StatusMessageRequested?.Invoke(this, "Copied to clipboard");
        }
    }

    /// <summary>
    /// Handles Save As requests from the context menu.
    /// </summary>
    private void OnSaveAsRequested(object? sender, TabItemViewModel tabViewModel)
    {
        RequestSaveAs(tabViewModel);
    }

    /// <summary>
    /// Duplicates the currently selected tab.
    /// </summary>
    public async Task DuplicateCurrentTabAsync()
    {
        if (SelectedTab == null)
        {
            return;
        }

        await TabListViewModel.DuplicateTabAsync(SelectedTab);
    }

    /// <summary>
    /// Requests focus on the title editing field.
    /// </summary>
    public void RequestEditTitle()
    {
        TabListViewModel.RequestEditTitle();
    }

    /// <summary>
    /// Event raised when clipboard copy completes to show feedback.
    /// </summary>
    public event EventHandler<string>? StatusMessageRequested;

    /// <summary>
    /// Copies the current tab's content to the clipboard.
    /// </summary>
    /// <returns>True if content was copied, false if no tab is selected.</returns>
    public bool CopyCurrentTabToClipboard()
    {
        if (SelectedTab == null || string.IsNullOrEmpty(EditorViewModel.Content))
        {
            return false;
        }

        // Request the view to handle the clipboard copy
        // We'll raise an event that the view can handle since clipboard access
        // requires the TopLevel (which we don't have in the ViewModel)
        ClipboardCopyRequested?.Invoke(this, EditorViewModel.Content);
        StatusMessageRequested?.Invoke(this, "Copied to clipboard");
        return true;
    }

    /// <summary>
    /// Event raised when clipboard copy is requested.
    /// The view handles this since clipboard access requires TopLevel.
    /// </summary>
    public event EventHandler<string>? ClipboardCopyRequested;

    /// <summary>
    /// Event raised when Save As is requested.
    /// The view handles this since file dialog access requires TopLevel.
    /// </summary>
    public event EventHandler<SaveAsRequestEventArgs>? SaveAsRequested;

    /// <summary>
    /// Requests a Save As dialog for the current tab.
    /// </summary>
    public void RequestSaveAs()
    {
        if (SelectedTab == null)
        {
            return;
        }

        var suggestedName = !string.IsNullOrWhiteSpace(EditorViewModel.Title)
            ? EditorViewModel.Title + ".txt"
            : "untitled.txt";

        SaveAsRequested?.Invoke(this, new SaveAsRequestEventArgs(
            EditorViewModel.Content ?? string.Empty,
            suggestedName));
    }

    /// <summary>
    /// Requests a Save As dialog for a specific tab.
    /// </summary>
    /// <param name="tabViewModel">The tab to save.</param>
    public void RequestSaveAs(TabItemViewModel tabViewModel)
    {
        // Get the content from the tab via TabManager
        var tabWithStats = _tabManager.GetOpenTabs().FirstOrDefault(t => t.Tab.TabId == tabViewModel.TabId);
        if (tabWithStats == null)
        {
            return;
        }

        var content = tabWithStats.Tab.Content;
        var title = tabWithStats.Tab.Document.Title;
        var suggestedName = !string.IsNullOrWhiteSpace(title)
            ? title + ".txt"
            : "untitled.txt";

        SaveAsRequested?.Invoke(this, new SaveAsRequestEventArgs(content, suggestedName));
    }
}

/// <summary>
/// Event arguments for Save As requests.
/// </summary>
public class SaveAsRequestEventArgs : EventArgs
{
    public string Content { get; }
    public string SuggestedFileName { get; }

    public SaveAsRequestEventArgs(string content, string suggestedFileName)
    {
        Content = content;
        SuggestedFileName = suggestedFileName;
    }
}
