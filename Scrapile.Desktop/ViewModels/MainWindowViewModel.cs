using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Scrapile.Application.Services;

namespace Scrapile.Desktop.ViewModels;

/// <summary>
/// Main window view model that coordinates the application's primary UI.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TabManager _tabManager;
    private readonly DocumentService _documentService;
    private readonly AutoSaveService _autoSaveService;

    [ObservableProperty]
    private string _title = "Scrapile";

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _hasTabs;

    [ObservableProperty]
    private TabListViewModel _tabListViewModel;

    [ObservableProperty]
    private EditorViewModel _editorViewModel;

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    /// <summary>
    /// Creates a new MainWindowViewModel with injected services.
    /// </summary>
    /// <param name="tabManager">The tab manager service.</param>
    /// <param name="documentService">The document service.</param>
    /// <param name="autoSaveService">The auto-save service.</param>
    public MainWindowViewModel(
        TabManager tabManager,
        DocumentService documentService,
        AutoSaveService autoSaveService)
    {
        _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _autoSaveService = autoSaveService ?? throw new ArgumentNullException(nameof(autoSaveService));

        // Create the tab list view model
        _tabListViewModel = new TabListViewModel(_tabManager, _autoSaveService);
        _tabListViewModel.TabSelected += OnTabSelected;
        _tabListViewModel.TabsChanged += OnTabsChanged;

        // Create the editor view model
        _editorViewModel = new EditorViewModel(_tabManager, _documentService, _autoSaveService);
        _editorViewModel.ContentChanged += OnEditorContentChanged;
        _editorViewModel.TitleChanged += OnEditorTitleChanged;
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

        // Initialize the tab manager to restore previous session
        await _tabManager.InitializeAsync();

        // Load tabs into the TabListViewModel
        TabListViewModel.LoadTabs();

        // Update HasTabs based on restored tabs
        UpdateHasTabs();

        IsInitialized = true;
    }

    /// <summary>
    /// Handles tab selection events from the tab list.
    /// </summary>
    private void OnTabSelected(object? sender, TabItemViewModel? tabViewModel)
    {
        SelectedTab = tabViewModel;
        EditorViewModel.CurrentTab = tabViewModel;
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
}
