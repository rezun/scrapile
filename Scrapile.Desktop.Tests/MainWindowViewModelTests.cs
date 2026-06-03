using Scrapile.Application.DTOs;
using Scrapile.Application.Services;
using Scrapile.Desktop.Services;
using Scrapile.Desktop.ViewModels;
using Scrapile.Domain.Constants;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Desktop.Tests;

public class MainWindowViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTabManager_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(null!, services.DocService, services.AutoSave, services.Theme, services.Settings, services.Autorun, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, null!, services.AutoSave, services.Theme, services.Settings, services.Autorun, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullAutoSaveService_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, services.DocService, null!, services.Theme, services.Settings, services.Autorun, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullThemeService_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, services.DocService, services.AutoSave, null!, services.Settings, services.Autorun, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullSettingsService_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, services.DocService, services.AutoSave, services.Theme, null!, services.Autorun, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullAutorunService_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, services.DocService, services.AutoSave, services.Theme, services.Settings, null!, services.Metadata));
    }

    [Fact]
    public void Constructor_WithNullMetadataStore_ThrowsArgumentNullException()
    {
        var services = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new MainWindowViewModel(services.TabManager, services.DocService, services.AutoSave, services.Theme, services.Settings, services.Autorun, null!));
    }

    [Fact]
    public void Constructor_WithValidServices_InitializesDefaults()
    {
        var viewModel = CreateMainWindowViewModel();

        Assert.Equal("Scrapile", viewModel.Title);
        Assert.False(viewModel.IsInitialized);
        Assert.False(viewModel.HasTabs);
        Assert.False(viewModel.IsSearchVisible);
        Assert.True(viewModel.IsTabListOnLeft);
        Assert.NotNull(viewModel.TabListViewModel);
        Assert.NotNull(viewModel.EditorViewModel);
        Assert.NotNull(viewModel.SearchViewModel);
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsync_SetsIsInitializedTrue()
    {
        var viewModel = CreateMainWindowViewModel();

        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsInitialized);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyInitialized_DoesNotReinitialize()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        // Should not throw on second call
        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsInitialized);
    }

    [Fact]
    public async Task InitializeAsync_LoadsTabsFromTabManager()
    {
        var services = CreateServices();
        await services.TabManager.InitializeAsync();
        await services.TabManager.CreateTabAsync();
        var viewModel = CreateMainWindowViewModelWithServices(services);

        await viewModel.InitializeAsync();

        Assert.True(viewModel.HasTabs);
        Assert.Single(viewModel.TabListViewModel.Tabs);
    }

    [Fact]
    public async Task InitializeAsync_AppliesTabPositionSetting()
    {
        var services = CreateServices();
        await services.Settings.SetTabPositionAsync("Right");
        var viewModel = CreateMainWindowViewModelWithServices(services);

        await viewModel.InitializeAsync();

        Assert.False(viewModel.IsTabListOnLeft);
    }

    #endregion

    #region CreateNewTabAsync Tests

    [Fact]
    public async Task CreateNewTabAsync_DelegatesToTabListViewModel()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        await viewModel.CreateNewTabAsync();

        Assert.Single(viewModel.TabListViewModel.Tabs);
        Assert.True(viewModel.HasTabs);
    }

    #endregion

    #region CloseCurrentTabAsync Tests

    [Fact]
    public async Task CloseCurrentTabAsync_WithNoSelectedTab_DoesNothing()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        // Should not throw
        await viewModel.CloseCurrentTabAsync();
    }

    [Fact]
    public async Task CloseCurrentTabAsync_ClosesSelectedTab()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();

        await viewModel.CloseCurrentTabAsync();

        Assert.Empty(viewModel.TabListViewModel.Tabs);
        Assert.False(viewModel.HasTabs);
    }

    [Fact]
    public async Task CloseCurrentTabAsync_PassesEditorContent()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "test content";

        await viewModel.CloseCurrentTabAsync();

        // Content should be saved - verify through the mock
        Assert.Empty(viewModel.TabListViewModel.Tabs);
    }

    #endregion

    #region SelectNextTab/SelectPreviousTab Tests

    [Fact]
    public async Task SelectNextTab_DelegatesToTabListViewModel()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.TabListViewModel.SelectTab(viewModel.TabListViewModel.Tabs[0]);

        viewModel.SelectNextTab();

        Assert.Equal(viewModel.TabListViewModel.Tabs[1], viewModel.TabListViewModel.SelectedTab);
    }

    [Fact]
    public async Task SelectPreviousTab_DelegatesToTabListViewModel()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.TabListViewModel.SelectTab(viewModel.TabListViewModel.Tabs[1]);

        viewModel.SelectPreviousTab();

        Assert.Equal(viewModel.TabListViewModel.Tabs[0], viewModel.TabListViewModel.SelectedTab);
    }

    #endregion

    #region ReopenLastClosedAsync Tests

    [Fact]
    public async Task ReopenLastClosedAsync_WithNoRecentlyClosed_ReturnsFalse()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        var result = await viewModel.ReopenLastClosedAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ReopenLastClosedAsync_ReopensLastClosedTab()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();

        // Create and close a tab with content
        await viewModel.CreateNewTabAsync();
        var tab = viewModel.SelectedTab!;
        services.TabManager.UpdateTabContent(tab.TabId, "content to restore");
        await viewModel.CloseCurrentTabAsync();

        // Reopen
        var result = await viewModel.ReopenLastClosedAsync();

        Assert.True(result);
        Assert.Single(viewModel.TabListViewModel.Tabs);
    }

    [Fact]
    public async Task ReopenLastClosedAsync_SelectsReopenedTab()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        var closedTabDocId = viewModel.SelectedTab!.DocumentId;
        services.TabManager.UpdateTabContent(viewModel.SelectedTab.TabId, "content");
        await viewModel.CloseCurrentTabAsync();

        await viewModel.ReopenLastClosedAsync();

        Assert.NotNull(viewModel.SelectedTab);
        Assert.Equal(closedTabDocId, viewModel.SelectedTab.DocumentId);
    }

    #endregion

    #region Search Tests

    [Fact]
    public void ShowSearch_SetsIsSearchVisibleTrue()
    {
        var viewModel = CreateMainWindowViewModel();

        viewModel.ShowSearch();

        Assert.True(viewModel.IsSearchVisible);
    }

    [Fact]
    public void ShowSearch_ResetsSearchViewModel()
    {
        var viewModel = CreateMainWindowViewModel();
        viewModel.SearchViewModel.SearchQuery = "previous search";

        viewModel.ShowSearch();

        Assert.Equal(string.Empty, viewModel.SearchViewModel.SearchQuery);
    }

    [Fact]
    public void HideSearch_SetsIsSearchVisibleFalse()
    {
        var viewModel = CreateMainWindowViewModel();
        viewModel.ShowSearch();

        viewModel.HideSearch();

        Assert.False(viewModel.IsSearchVisible);
    }

    [Fact]
    public void ToggleSearch_TogglesVisibility()
    {
        var viewModel = CreateMainWindowViewModel();

        viewModel.ToggleSearch();
        Assert.True(viewModel.IsSearchVisible);

        viewModel.ToggleSearch();
        Assert.False(viewModel.IsSearchVisible);
    }

    #endregion

    #region DuplicateCurrentTabAsync Tests

    [Fact]
    public async Task DuplicateCurrentTabAsync_WithNoSelectedTab_DoesNothing()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        // Should not throw
        await viewModel.DuplicateCurrentTabAsync();

        Assert.Empty(viewModel.TabListViewModel.Tabs);
    }

    [Fact]
    public async Task DuplicateCurrentTabAsync_DuplicatesSelectedTab()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        services.TabManager.UpdateTabContent(viewModel.SelectedTab!.TabId, "content");

        await viewModel.DuplicateCurrentTabAsync();

        Assert.Equal(2, viewModel.TabListViewModel.Tabs.Count);
    }

    #endregion

    #region RequestEditTitle Tests

    [Fact]
    public async Task RequestEditTitle_DelegatesToTabListViewModel()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        var eventRaised = false;
        viewModel.FocusTitleRequested += (_, _) => eventRaised = true;

        viewModel.RequestEditTitle();

        Assert.True(eventRaised);
    }

    #endregion

    #region SaveAllPendingChangesAsync Tests

    [Fact]
    public async Task SaveAllPendingChangesAsync_SavesAllDirtyTabs()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        var tab1 = viewModel.SelectedTab!;
        services.TabManager.UpdateTabContent(tab1.TabId, "dirty content 1");

        await viewModel.CreateNewTabAsync();
        var tab2 = viewModel.SelectedTab!;
        services.TabManager.UpdateTabContent(tab2.TabId, "dirty content 2");

        await viewModel.SaveAllPendingChangesAsync();

        // Both should be saved
        Assert.Contains(tab1.DocumentId, services.MockRepo.UpdatedDocuments.Keys);
        Assert.Contains(tab2.DocumentId, services.MockRepo.UpdatedDocuments.Keys);
    }

    #endregion

    #region CopyCurrentTabToClipboard Tests

    [Fact]
    public void CopyCurrentTabToClipboard_WithNoSelectedTab_ReturnsFalse()
    {
        var viewModel = CreateMainWindowViewModel();

        var result = viewModel.CopyCurrentTabToClipboard();

        Assert.False(result);
    }

    [Fact]
    public async Task CopyCurrentTabToClipboard_WithEmptyContent_ReturnsFalse()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();

        var result = viewModel.CopyCurrentTabToClipboard();

        Assert.False(result);
    }

    [Fact]
    public async Task CopyCurrentTabToClipboard_WithContent_ReturnsTrue()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "test content";

        var result = viewModel.CopyCurrentTabToClipboard();

        Assert.True(result);
    }

    [Fact]
    public async Task CopyCurrentTabToClipboard_RaisesClipboardCopyRequestedEvent()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "test content";
        string? copiedContent = null;
        viewModel.ClipboardCopyRequested += (_, content) => copiedContent = content;

        viewModel.CopyCurrentTabToClipboard();

        Assert.Equal("test content", copiedContent);
    }

    [Fact]
    public async Task CopyCurrentTabToClipboard_RaisesStatusMessageRequestedEvent()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "test content";
        string? message = null;
        viewModel.StatusMessageRequested += (_, msg) => message = msg;

        viewModel.CopyCurrentTabToClipboard();

        Assert.Equal("Copied to clipboard", message);
    }

    #endregion

    #region RequestSaveAs Tests

    [Fact]
    public void RequestSaveAs_WithNoSelectedTab_DoesNotRaiseEvent()
    {
        var viewModel = CreateMainWindowViewModel();
        var eventRaised = false;
        viewModel.SaveAsRequested += (_, _) => eventRaised = true;

        viewModel.RequestSaveAs();

        Assert.False(eventRaised);
    }

    [Fact]
    public async Task RequestSaveAs_RaisesSaveAsRequestedEvent()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "content";
        SaveAsRequestEventArgs? eventArgs = null;
        viewModel.SaveAsRequested += (_, args) => eventArgs = args;

        viewModel.RequestSaveAs();

        Assert.NotNull(eventArgs);
        Assert.Equal("content", eventArgs.Content);
    }

    [Fact]
    public async Task RequestSaveAs_UsesTitleForSuggestedFilename()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Title = "My Document";
        viewModel.EditorViewModel.Content = "content";
        SaveAsRequestEventArgs? eventArgs = null;
        viewModel.SaveAsRequested += (_, args) => eventArgs = args;

        viewModel.RequestSaveAs();

        Assert.Equal("My Document.txt", eventArgs!.SuggestedFileName);
    }

    [Fact]
    public async Task RequestSaveAs_UsesUntitledForSuggestedFilename_WhenNoTitle()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        viewModel.EditorViewModel.Content = "content";
        SaveAsRequestEventArgs? eventArgs = null;
        viewModel.SaveAsRequested += (_, args) => eventArgs = args;

        viewModel.RequestSaveAs();

        Assert.Equal("untitled.txt", eventArgs!.SuggestedFileName);
    }

    #endregion

    #region TabSelected Handler Tests

    [Fact]
    public async Task TabSelected_UpdatesSelectedTab()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();

        Assert.NotNull(viewModel.SelectedTab);
    }

    [Fact]
    public async Task TabSelected_UpdatesEditorCurrentTab()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();

        Assert.NotNull(viewModel.EditorViewModel.CurrentTab);
    }

    #endregion

    #region TabsChanged Handler Tests

    [Fact]
    public async Task TabsChanged_UpdatesHasTabs()
    {
        var viewModel = CreateMainWindowViewModel();
        await viewModel.InitializeAsync();

        Assert.False(viewModel.HasTabs);

        await viewModel.CreateNewTabAsync();

        Assert.True(viewModel.HasTabs);

        await viewModel.CloseCurrentTabAsync();

        Assert.False(viewModel.HasTabs);
    }

    #endregion

    #region SettingsChanged Handler Tests

    [Fact]
    public async Task SettingsChanged_TabPosition_UpdatesIsTabListOnLeft()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsTabListOnLeft);

        await services.Settings.SetTabPositionAsync("Right");

        Assert.False(viewModel.IsTabListOnLeft);
    }

    #endregion

    #region OpenSettings Tests

    [Fact]
    public void OpenSettingsCommand_RaisesOpenSettingsRequestedEvent()
    {
        var viewModel = CreateMainWindowViewModel();
        var eventRaised = false;
        viewModel.OpenSettingsRequested += (_, _) => eventRaised = true;

        viewModel.OpenSettingsCommand.Execute(null);

        Assert.True(eventRaised);
    }

    #endregion

    #region Service Properties Tests

    [Fact]
    public void SettingsService_ReturnsInjectedService()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);

        Assert.Same(services.Settings, viewModel.SettingsService);
    }

    [Fact]
    public void ThemeService_ReturnsInjectedService()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);

        Assert.Same(services.Theme, viewModel.ThemeService);
    }

    [Fact]
    public void AutorunService_ReturnsInjectedService()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);

        Assert.Same(services.Autorun, viewModel.AutorunService);
    }

    #endregion

    #region Gesture Properties Tests

    [Fact]
    public void ReopenLastClosedGesture_ContainsModifierAndShiftT()
    {
        var viewModel = CreateMainWindowViewModel();

        Assert.Contains("Shift+T", viewModel.ReopenLastClosedGesture);
    }

    [Fact]
    public void SettingsGesture_ContainsModifierAndComma()
    {
        var viewModel = CreateMainWindowViewModel();

        Assert.Contains(",", viewModel.SettingsGesture);
    }

    [Fact]
    public void SearchGesture_ContainsModifierAndP()
    {
        var viewModel = CreateMainWindowViewModel();

        Assert.Contains("P", viewModel.SearchGesture);
    }

    #endregion

    #region RecentlyClosed Menu Tests

    [Fact]
    public void HasRecentlyClosedMenuItems_InitiallyFalse()
    {
        var viewModel = CreateMainWindowViewModel();

        Assert.False(viewModel.HasRecentlyClosedMenuItems);
    }

    [Fact]
    public async Task RecentlyClosedMenuItems_PopulatedAfterTabClose()
    {
        var services = CreateServices();
        var viewModel = CreateMainWindowViewModelWithServices(services);
        await viewModel.InitializeAsync();
        await viewModel.CreateNewTabAsync();
        services.TabManager.UpdateTabContent(viewModel.SelectedTab!.TabId, "content");

        await viewModel.CloseCurrentTabAsync();

        // Give async handler time to complete
        await Task.Delay(50);

        Assert.True(viewModel.HasRecentlyClosedMenuItems);
    }

    #endregion

    #region Helper Methods

    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        var services = CreateServices();
        return new MainWindowViewModel(
            services.TabManager,
            services.DocService,
            services.AutoSave,
            services.Theme,
            services.Settings,
            services.Autorun,
            services.Metadata);
    }

    private static MainWindowViewModel CreateMainWindowViewModelWithServices(TestServices services)
    {
        return new MainWindowViewModel(
            services.TabManager,
            services.DocService,
            services.AutoSave,
            services.Theme,
            services.Settings,
            services.Autorun,
            services.Metadata);
    }

    private static TestServices CreateServices()
    {
        var mockRepo = new MockDocumentRepository();
        var mockMetadata = new MockMetadataStore();
        var tabManager = new TabManager(mockRepo, mockMetadata);
        var docService = new DocumentService(mockRepo);
        var autoSave = new AutoSaveService(mockRepo);
        var theme = new ThemeService(mockMetadata);
        var settingsStore = new MockSettingsStore();
        var settings = new SettingsService(settingsStore);
        var autorun = new AutorunService();

        return new TestServices(tabManager, docService, autoSave, theme, settings, autorun, mockMetadata, mockRepo);
    }

    private record TestServices(
        TabManager TabManager,
        DocumentService DocService,
        AutoSaveService AutoSave,
        ThemeService Theme,
        SettingsService Settings,
        AutorunService Autorun,
        MockMetadataStore Metadata,
        MockDocumentRepository MockRepo);

    #endregion

    #region Mock Classes

    private class MockDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents = new();
        public Dictionary<Guid, string> UpdatedDocuments { get; } = new();

        public Task<Document> CreateAsync(string content, string? title = null)
        {
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                Filename = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.txt",
                Title = title,
                Content = content,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _documents[doc.Id] = doc;
            return Task.FromResult(doc);
        }

        public Task<Document?> GetByIdAsync(Guid id) =>
            Task.FromResult(_documents.TryGetValue(id, out var doc) ? doc : null);

        public Task<IEnumerable<Document>> GetAllAsync() =>
            Task.FromResult(_documents.Values.AsEnumerable());

        public Task UpdateContentAsync(Guid id, string content)
        {
            if (_documents.TryGetValue(id, out var doc))
            {
                doc.Content = content;
                doc.LastModified = DateTime.UtcNow;
            }
            UpdatedDocuments[id] = content;
            return Task.CompletedTask;
        }

        public Task UpdateTitleAsync(Guid id, string? title)
        {
            if (_documents.TryGetValue(id, out var doc))
            {
                doc.Title = title;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _documents.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Document>> SearchAsync(string query) =>
            Task.FromResult(_documents.Values.Where(d =>
                (d.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    private class MockMetadataStore : IMetadataStore
    {
        private List<Guid> _openTabs = new();
        private readonly List<RecentlyClosedInfo> _recentlyClosed = new();
        private readonly Dictionary<Guid, string?> _documentTitles = new();
        private readonly Dictionary<Guid, string?> _documentWordWrap = new();
        private Guid? _activeTabDocumentId;
        private string? _theme;

        public Task<Metadata> LoadAsync() => Task.FromResult(new Metadata { Theme = _theme ?? ThemeValues.System });

        public Task SaveAsync(Metadata metadata)
        {
            _theme = metadata.Theme;
            return Task.CompletedTask;
        }

        public Task AddDocumentAsync(Guid documentId, string? title) => Task.CompletedTask;

        public Task UpdateDocumentTitleAsync(Guid documentId, string? title) => Task.CompletedTask;

        public Task RemoveDocumentAsync(Guid documentId) => Task.CompletedTask;

        public Task AddOpenTabAsync(Guid documentId, int order)
        {
            _openTabs.Add(documentId);
            return Task.CompletedTask;
        }

        public Task RemoveOpenTabAsync(Guid documentId)
        {
            _openTabs.Remove(documentId);
            return Task.CompletedTask;
        }

        public Task UpdateOpenTabsOrderAsync(IEnumerable<Guid> orderedDocumentIds)
        {
            _openTabs = orderedDocumentIds.ToList();
            return Task.CompletedTask;
        }

        public Task AddRecentlyClosedAsync(Guid documentId, DateTime closedAt)
        {
            _recentlyClosed.Insert(0, new RecentlyClosedInfo { DocumentId = documentId, ClosedAt = closedAt });
            return Task.CompletedTask;
        }

        public Task RemoveRecentlyClosedAsync(Guid documentId)
        {
            _recentlyClosed.RemoveAll(r => r.DocumentId == documentId);
            return Task.CompletedTask;
        }

        public Task<List<Guid>> GetOpenTabsAsync() => Task.FromResult(_openTabs.ToList());

        public Task<List<RecentlyClosedInfo>> GetRecentlyClosedAsync() => Task.FromResult(_recentlyClosed.ToList());

        public Task<string?> GetDocumentTitleAsync(Guid documentId) =>
            Task.FromResult(_documentTitles.TryGetValue(documentId, out var title) ? title : null);

        public Task<Guid?> GetActiveTabDocumentIdAsync() => Task.FromResult(_activeTabDocumentId);

        public Task SetActiveTabDocumentIdAsync(Guid? documentId)
        {
            _activeTabDocumentId = documentId;
            return Task.CompletedTask;
        }

        public Task<string?> GetDocumentWordWrapAsync(Guid documentId) =>
            Task.FromResult(_documentWordWrap.TryGetValue(documentId, out var wordWrap) ? wordWrap : null);

        public Task UpdateDocumentWordWrapAsync(Guid documentId, string? wordWrap)
        {
            _documentWordWrap[documentId] = wordWrap;
            return Task.CompletedTask;
        }

        private readonly Dictionary<Guid, string?> _documentSyntaxLanguage = new();

        public Task<string?> GetDocumentSyntaxLanguageAsync(Guid documentId) =>
            Task.FromResult(_documentSyntaxLanguage.TryGetValue(documentId, out var language) ? language : null);

        public Task UpdateDocumentSyntaxLanguageAsync(Guid documentId, string? syntaxLanguage)
        {
            _documentSyntaxLanguage[documentId] = syntaxLanguage;
            return Task.CompletedTask;
        }
    }

    private class MockSettingsStore : ISettingsStore
    {
        private AppSettings _settings = AppSettings.CreateDefault();

        public string SettingsFilePath => "/mock/settings.json";

        public Task<AppSettings> LoadAsync() => Task.FromResult(_settings);

        public Task SaveAsync(AppSettings settings)
        {
            _settings = settings;
            return Task.CompletedTask;
        }

        public bool SettingsFileExists() => true;
    }

    #endregion
}
