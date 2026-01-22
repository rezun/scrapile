namespace Scrapile.Desktop.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Scrapile.Application.Services;

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
        _tabListViewModel = new TabListViewModel(_tabManager);
        _tabListViewModel.TabSelected += OnTabSelected;
        _tabListViewModel.TabsChanged += OnTabsChanged;
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
    }

    /// <summary>
    /// Handles changes to the tab collection.
    /// </summary>
    private void OnTabsChanged(object? sender, EventArgs e)
    {
        UpdateHasTabs();
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
    /// </summary>
    public async Task CloseCurrentTabAsync()
    {
        if (SelectedTab != null)
        {
            await TabListViewModel.CloseTabAsync(SelectedTab);
        }
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
