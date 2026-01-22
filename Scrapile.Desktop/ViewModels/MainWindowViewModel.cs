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

        IsInitialized = true;
    }
}
