using Avalonia.Media;
using Scrapile.Application.DTOs;
using Scrapile.Application.Services;
using Scrapile.Desktop.ViewModels;
using Scrapile.Domain.Constants;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Desktop.Tests;

public class EditorViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTabManager_ThrowsArgumentNullException()
    {
        var (_, docService, autoSave, settings, metadata) = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new EditorViewModel(null!, docService, autoSave, settings, metadata));
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ThrowsArgumentNullException()
    {
        var (tabManager, _, autoSave, settings, metadata) = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new EditorViewModel(tabManager, null!, autoSave, settings, metadata));
    }

    [Fact]
    public void Constructor_WithNullAutoSaveService_ThrowsArgumentNullException()
    {
        var (tabManager, docService, _, settings, metadata) = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new EditorViewModel(tabManager, docService, null!, settings, metadata));
    }

    [Fact]
    public void Constructor_WithNullSettingsService_ThrowsArgumentNullException()
    {
        var (tabManager, docService, autoSave, _, metadata) = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new EditorViewModel(tabManager, docService, autoSave, null!, metadata));
    }

    [Fact]
    public void Constructor_WithNullMetadataStore_ThrowsArgumentNullException()
    {
        var (tabManager, docService, autoSave, settings, _) = CreateServices();
        Assert.Throws<ArgumentNullException>(() =>
            new EditorViewModel(tabManager, docService, autoSave, settings, null!));
    }

    [Fact]
    public void Constructor_WithValidServices_InitializesDefaults()
    {
        var viewModel = CreateEditorViewModel();

        Assert.Equal(string.Empty, viewModel.Content);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.False(viewModel.HasTab);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(string.Empty, viewModel.SaveStatus);
    }

    #endregion

    #region CurrentTab Property Tests

    [Fact]
    public void CurrentTab_SetToNull_ClearsContent()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("test content", "Test Title");
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;
        viewModel.CurrentTab = null;

        Assert.Equal(string.Empty, viewModel.Content);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.False(viewModel.HasTab);
    }

    [Fact]
    public void CurrentTab_SetToTab_LoadsContent()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("test content", "Test Title");
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;

        Assert.Equal("test content", viewModel.Content);
        Assert.Equal("Test Title", viewModel.Title);
        Assert.True(viewModel.HasTab);
    }

    [Fact]
    public void CurrentTab_SetToTab_SetsHasTabTrue()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("content");
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;

        Assert.True(viewModel.HasTab);
    }

    [Fact]
    public void CurrentTab_SetToTabWithNullTitle_SetsEmptyTitle()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("content", null);
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;

        Assert.Equal(string.Empty, viewModel.Title);
    }

    [Fact]
    public void CurrentTab_SetToSameTab_DoesNotReload()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("original content");
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;
        viewModel.Content = "modified";
        viewModel.CurrentTab = tabViewModel;

        // Content should still be modified (not reloaded)
        Assert.Equal("modified", viewModel.Content);
    }

    [Fact]
    public void CurrentTab_SetToDirtyTab_SetsIsDirtyTrue()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("content", isDirty: true);
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;

        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void CurrentTab_SetToCleanTab_SetsIsDirtyFalse()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("content", isDirty: false);
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CurrentTab = tabViewModel;

        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void CurrentTab_Changed_ResetsCaretAndSelection()
    {
        var viewModel = CreateEditorViewModel();
        var tab1 = TestHelpers.CreateTabWithStats("content1");
        var tab2 = TestHelpers.CreateTabWithStats("content2");

        viewModel.CurrentTab = new TabItemViewModel(tab1, _ => { });
        viewModel.CaretIndex = 5;
        viewModel.SelectionStart = 0;
        viewModel.SelectionEnd = 3;

        viewModel.CurrentTab = new TabItemViewModel(tab2, _ => { });

        Assert.Equal(0, viewModel.CaretIndex);
        Assert.Equal(0, viewModel.SelectionStart);
        Assert.Equal(0, viewModel.SelectionEnd);
    }

    #endregion

    #region Content Change Tests

    [Fact]
    public void Content_Changed_UpdatesTabManager()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;

        viewModel.Content = "new content";

        var updatedTab = tabManager.GetTab(tab.Tab.TabId);
        Assert.Equal("new content", updatedTab?.Tab.Content);
    }

    [Fact]
    public void Content_Changed_SetsIsDirtyTrue()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;

        viewModel.Content = "new content";

        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void Content_Changed_RaisesContentChangedEvent()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;
        ContentChangedEventArgs? eventArgs = null;
        viewModel.ContentChanged += (_, args) => eventArgs = args;

        viewModel.Content = "new content";

        Assert.NotNull(eventArgs);
        Assert.Equal(tab.Tab.Document.Id, eventArgs.DocumentId);
        Assert.Equal("new content", eventArgs.Content);
    }

    [Fact]
    public void Content_Changed_WithNoTab_DoesNotRaiseEvent()
    {
        var viewModel = CreateEditorViewModel();
        var eventRaised = false;
        viewModel.ContentChanged += (_, _) => eventRaised = true;

        viewModel.Content = "new content";

        Assert.False(eventRaised);
    }

    [Fact]
    public void Content_Changed_UpdatesStatusBar()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;

        viewModel.Content = "hello world";

        Assert.Contains("2 words", viewModel.StatusText);
        Assert.Contains("11 chars", viewModel.StatusText);
    }

    #endregion

    #region Title Change Tests

    [Fact]
    public void Title_Changed_RaisesTitleChangedEvent()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;
        TitleChangedEventArgs? eventArgs = null;
        viewModel.TitleChanged += (_, args) => eventArgs = args;

        viewModel.Title = "New Title";

        Assert.NotNull(eventArgs);
        Assert.Equal(tab.Tab.Document.Id, eventArgs.DocumentId);
        Assert.Equal("New Title", eventArgs.Title);
    }

    [Fact]
    public void Title_Changed_ToEmpty_NormalizesToNull()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;

        // First set a title, then change to empty
        viewModel.Title = "Initial Title";

        TitleChangedEventArgs? eventArgs = null;
        viewModel.TitleChanged += (_, args) => eventArgs = args;

        viewModel.Title = "";

        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.Title);
    }

    [Fact]
    public void Title_Changed_ToWhitespace_NormalizesToNull()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;
        TitleChangedEventArgs? eventArgs = null;
        viewModel.TitleChanged += (_, args) => eventArgs = args;

        viewModel.Title = "   ";

        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.Title);
    }

    [Fact]
    public void Title_Changed_WithNoTab_DoesNotRaiseEvent()
    {
        var viewModel = CreateEditorViewModel();
        var eventRaised = false;
        viewModel.TitleChanged += (_, _) => eventRaised = true;

        viewModel.Title = "New Title";

        Assert.False(eventRaised);
    }

    #endregion

    #region Status Bar Tests

    [Fact]
    public void StatusText_WithNoTab_IsEmpty()
    {
        var viewModel = CreateEditorViewModel();

        Assert.Equal(string.Empty, viewModel.StatusText);
    }

    [Fact]
    public void StatusText_ShowsWordCharLineCount()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("Hello World\nLine Two");
        viewModel.CurrentTab = new TabItemViewModel(tabWithStats, _ => { });

        Assert.Contains("4 words", viewModel.StatusText);
        Assert.Contains("20 chars", viewModel.StatusText);  // "Hello World\nLine Two" = 20 chars
        Assert.Contains("2 lines", viewModel.StatusText);
    }

    [Fact]
    public void CursorPositionText_ShowsLineAndColumn()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("Hello\nWorld");
        viewModel.CurrentTab = new TabItemViewModel(tabWithStats, _ => { });

        // Position at start
        viewModel.CaretIndex = 0;
        Assert.Equal("Ln 1, Col 1", viewModel.CursorPositionText);

        // Position at second line
        viewModel.CaretIndex = 8; // "Hello\nWo"
        Assert.Equal("Ln 2, Col 3", viewModel.CursorPositionText);
    }

    [Fact]
    public void SelectionText_WithSelection_ShowsStats()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("Hello World Test");
        viewModel.CurrentTab = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.SelectionStart = 0;
        viewModel.SelectionEnd = 11; // "Hello World"

        Assert.True(viewModel.HasSelection);
        Assert.Contains("2 words", viewModel.SelectionText);
        Assert.Contains("11 chars", viewModel.SelectionText);
    }

    [Fact]
    public void HasSelection_WithNoSelection_IsFalse()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("content");
        viewModel.CurrentTab = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.SelectionStart = 0;
        viewModel.SelectionEnd = 0;

        Assert.False(viewModel.HasSelection);
        Assert.Equal(string.Empty, viewModel.SelectionText);
    }

    [Fact]
    public void CaretIndex_Changed_UpdatesCursorPosition()
    {
        var viewModel = CreateEditorViewModel();
        var tabWithStats = TestHelpers.CreateTabWithStats("Hello World");
        viewModel.CurrentTab = new TabItemViewModel(tabWithStats, _ => { });

        viewModel.CaretIndex = 5;

        Assert.Equal("Ln 1, Col 6", viewModel.CursorPositionText);
    }

    #endregion

    #region Word Wrap Tests

    [Fact]
    public void SelectedWordWrap_InitiallyDefault()
    {
        var viewModel = CreateEditorViewModel();

        Assert.Equal("Default", viewModel.SelectedWordWrap);
    }

    [Fact]
    public void WordWrapDisplayText_InitiallyWrapDefault()
    {
        var viewModel = CreateEditorViewModel();

        Assert.Equal("Wrap: Default", viewModel.WordWrapDisplayText);
    }

    [Fact]
    public async Task SetDocumentWordWrapAsync_UpdatesSelectedWordWrap()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        viewModel.CurrentTab = new TabItemViewModel(tab, _ => { });

        await viewModel.SetDocumentWordWrapAsync("No Wrap");

        Assert.Equal("No Wrap", viewModel.SelectedWordWrap);
        Assert.Equal(TextWrapping.NoWrap, viewModel.TextWrapping);
    }

    [Fact]
    public async Task SetDocumentWordWrapAsync_UpdatesDisplayText()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        viewModel.CurrentTab = new TabItemViewModel(tab, _ => { });

        await viewModel.SetDocumentWordWrapAsync("No Wrap");

        Assert.Equal("No Wrap", viewModel.WordWrapDisplayText);
    }

    [Fact]
    public async Task CycleWordWrapAsync_CyclesThroughOptions()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        viewModel.CurrentTab = new TabItemViewModel(tab, _ => { });

        Assert.Equal("Default", viewModel.SelectedWordWrap);

        await viewModel.CycleWordWrapAsync();
        Assert.Equal("Wrap", viewModel.SelectedWordWrap);

        await viewModel.CycleWordWrapAsync();
        Assert.Equal("No Wrap", viewModel.SelectedWordWrap);

        await viewModel.CycleWordWrapAsync();
        Assert.Equal("Default", viewModel.SelectedWordWrap);
    }

    [Fact]
    public async Task SetDocumentWordWrapAsync_WithNoTab_DoesNothing()
    {
        var viewModel = CreateEditorViewModel();

        // Should not throw
        await viewModel.SetDocumentWordWrapAsync("Wrap");

        Assert.Equal("Default", viewModel.SelectedWordWrap);
    }

    #endregion

    #region SetDirty Tests

    [Fact]
    public void SetDirty_UpdatesIsDirty()
    {
        var viewModel = CreateEditorViewModel();

        viewModel.SetDirty(true);
        Assert.True(viewModel.IsDirty);

        viewModel.SetDirty(false);
        Assert.False(viewModel.IsDirty);
    }

    #endregion

    #region RefreshFromTab Tests

    [Fact]
    public void RefreshFromTab_ReloadsContent()
    {
        var (viewModel, tabManager, _, _) = CreateEditorViewModelWithServices();
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        var tab = tabManager.CreateTabAsync().GetAwaiter().GetResult();
        var tabViewModel = new TabItemViewModel(tab, _ => { });
        viewModel.CurrentTab = tabViewModel;
        viewModel.Content = "modified";

        // Update tab content externally
        tabManager.UpdateTabContent(tab.Tab.TabId, "external update");

        // Refresh should reload
        viewModel.RefreshFromTab();

        Assert.Equal("external update", viewModel.Content);
    }

    #endregion

    #region Font Settings Tests

    [Fact]
    public void EditorFontSize_DefaultsTo14()
    {
        var viewModel = CreateEditorViewModel();

        Assert.Equal(14, viewModel.EditorFontSize);
    }

    [Fact]
    public void EditorFontFamily_DefaultsToMonospace()
    {
        var viewModel = CreateEditorViewModel();

        Assert.NotNull(viewModel.EditorFontFamily);
    }

    #endregion

    #region Helper Methods

    private static EditorViewModel CreateEditorViewModel()
    {
        var (tabManager, docService, autoSave, settings, metadata) = CreateServices();
        return new EditorViewModel(tabManager, docService, autoSave, settings, metadata);
    }

    private static (EditorViewModel, TabManager, MockDocumentRepository, MockMetadataStore) CreateEditorViewModelWithServices()
    {
        var mockRepo = new MockDocumentRepository();
        var mockMetadata = new MockMetadataStore();
        var tabManager = new TabManager(mockRepo, mockMetadata);
        var docService = new DocumentService(mockRepo);
        var autoSave = new AutoSaveService(mockRepo);
        var settingsStore = new MockSettingsStore();
        var settings = new SettingsService(settingsStore);
        var viewModel = new EditorViewModel(tabManager, docService, autoSave, settings, mockMetadata);
        return (viewModel, tabManager, mockRepo, mockMetadata);
    }

    private static (TabManager, DocumentService, AutoSaveService, SettingsService, IMetadataStore) CreateServices()
    {
        var mockRepo = new MockDocumentRepository();
        var mockMetadata = new MockMetadataStore();
        var tabManager = new TabManager(mockRepo, mockMetadata);
        var docService = new DocumentService(mockRepo);
        var autoSave = new AutoSaveService(mockRepo);
        var settingsStore = new MockSettingsStore();
        var settings = new SettingsService(settingsStore);
        return (tabManager, docService, autoSave, settings, mockMetadata);
    }

    #endregion

    #region Mock Classes

    private class MockDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents = new();

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

        public Task<Metadata> LoadAsync() => Task.FromResult(new Metadata());

        public Task SaveAsync(Metadata metadata) => Task.CompletedTask;

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
        public string SettingsFilePath => "/mock/settings.json";

        public Task<AppSettings> LoadAsync() => Task.FromResult(AppSettings.CreateDefault());

        public Task SaveAsync(AppSettings settings) => Task.CompletedTask;

        public bool SettingsFileExists() => true;
    }

    #endregion
}
