using Scrapile.Application.DTOs;
using Scrapile.Application.Services;
using Scrapile.Desktop.ViewModels;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Desktop.Tests;

public class TabListViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTabManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TabListViewModel(null!));
    }

    [Fact]
    public void Constructor_WithValidTabManager_CreatesInstance()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        Assert.NotNull(viewModel);
        Assert.Empty(viewModel.Tabs);
        Assert.Null(viewModel.SelectedTab);
        Assert.False(viewModel.HasTabs);
    }

    #endregion

    #region LoadTabsAsync Tests

    [Fact]
    public async Task LoadTabsAsync_WithNoTabs_LeavesCollectionEmpty()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        await viewModel.LoadTabsAsync();

        Assert.Empty(viewModel.Tabs);
        Assert.Null(viewModel.SelectedTab);
        Assert.False(viewModel.HasTabs);
    }

    [Fact]
    public async Task LoadTabsAsync_WithTabs_PopulatesCollection()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        await tabManager.CreateTabAsync();
        await tabManager.CreateTabAsync();

        var viewModel = new TabListViewModel(tabManager);
        await viewModel.LoadTabsAsync();

        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.True(viewModel.HasTabs);
    }

    [Fact]
    public async Task LoadTabsAsync_SelectsFirstTab_WhenNoActiveTabPersisted()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        var tab = await tabManager.CreateTabAsync();

        var viewModel = new TabListViewModel(tabManager);
        await viewModel.LoadTabsAsync();

        Assert.NotNull(viewModel.SelectedTab);
        Assert.True(viewModel.SelectedTab.IsSelected);
    }

    [Fact]
    public async Task LoadTabsAsync_RestoresActiveTab_WhenPersisted()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        var tab1 = await tabManager.CreateTabAsync();
        var tab2 = await tabManager.CreateTabAsync();

        // Persist second tab as active
        await tabManager.SetActiveTabDocumentIdAsync(tab2.Tab.Document.Id);

        var viewModel = new TabListViewModel(tabManager);
        await viewModel.LoadTabsAsync();

        Assert.NotNull(viewModel.SelectedTab);
        Assert.Equal(tab2.Tab.Document.Id, viewModel.SelectedTab.DocumentId);
    }

    [Fact]
    public async Task LoadTabsAsync_RaisesTabsChangedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var eventRaised = false;
        viewModel.TabsChanged += (_, _) => eventRaised = true;

        await viewModel.LoadTabsAsync();

        Assert.True(eventRaised);
    }

    #endregion

    #region CreateNewTabAsync Tests

    [Fact]
    public async Task CreateNewTabAsync_AddsTabToCollection()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        await viewModel.CreateNewTabAsync();

        Assert.Single(viewModel.Tabs);
        Assert.True(viewModel.HasTabs);
    }

    [Fact]
    public async Task CreateNewTabAsync_SelectsNewTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        await viewModel.CreateNewTabAsync();

        Assert.NotNull(viewModel.SelectedTab);
        Assert.True(viewModel.SelectedTab.IsSelected);
    }

    [Fact]
    public async Task CreateNewTabAsync_RaisesTabSelectedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        TabItemViewModel? selectedTab = null;
        viewModel.TabSelected += (_, tab) => selectedTab = tab;

        await viewModel.CreateNewTabAsync();

        Assert.NotNull(selectedTab);
    }

    [Fact]
    public async Task CreateNewTabAsync_RaisesTabsChangedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var eventRaised = false;
        viewModel.TabsChanged += (_, _) => eventRaised = true;

        await viewModel.CreateNewTabAsync();

        Assert.True(eventRaised);
    }

    [Fact]
    public async Task CreateNewTabAsync_UpdatesHasTabs()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        Assert.False(viewModel.HasTabs);

        await viewModel.CreateNewTabAsync();

        Assert.True(viewModel.HasTabs);
    }

    #endregion

    #region CloseTabAsync Tests

    [Fact]
    public async Task CloseTabAsync_RemovesTabFromCollection()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var tabToClose = viewModel.Tabs[0];

        await viewModel.CloseTabAsync(tabToClose);

        Assert.Empty(viewModel.Tabs);
        Assert.False(viewModel.HasTabs);
    }

    [Fact]
    public async Task CloseTabAsync_WithNull_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();

        await viewModel.CloseTabAsync(null!);

        Assert.Single(viewModel.Tabs);
    }

    [Fact]
    public async Task CloseTabAsync_SelectsAdjacentTab_WhenClosingSelectedTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var firstTab = viewModel.Tabs[0];
        viewModel.SelectTab(firstTab);

        await viewModel.CloseTabAsync(firstTab);

        Assert.NotNull(viewModel.SelectedTab);
        Assert.True(viewModel.SelectedTab.IsSelected);
    }

    [Fact]
    public async Task CloseTabAsync_SelectsLastTab_WhenClosingLastTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var lastTab = viewModel.Tabs[1];
        viewModel.SelectTab(lastTab);

        await viewModel.CloseTabAsync(lastTab);

        Assert.NotNull(viewModel.SelectedTab);
        Assert.Equal(viewModel.Tabs[0].TabId, viewModel.SelectedTab.TabId);
    }

    [Fact]
    public async Task CloseTabAsync_ClearsSelection_WhenClosingOnlyTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var onlyTab = viewModel.Tabs[0];

        await viewModel.CloseTabAsync(onlyTab);

        Assert.Null(viewModel.SelectedTab);
    }

    [Fact]
    public async Task CloseTabAsync_RaisesTabsChangedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var tabToClose = viewModel.Tabs[0];
        var eventRaised = false;
        viewModel.TabsChanged += (_, _) => eventRaised = true;

        await viewModel.CloseTabAsync(tabToClose);

        Assert.True(eventRaised);
    }

    [Fact]
    public async Task CloseTabAsync_RaisesRecentlyClosedChangedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var tabToClose = viewModel.Tabs[0];
        var eventRaised = false;
        viewModel.RecentlyClosedChanged += (_, _) => eventRaised = true;

        await viewModel.CloseTabAsync(tabToClose);

        Assert.True(eventRaised);
    }

    [Fact]
    public async Task CloseTabAsync_SavesDirtyContent_WhenAutoSaveServiceProvided()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        var autoSaveService = new AutoSaveService(mockRepo);
        var viewModel = new TabListViewModel(tabManager, autoSaveService);

        await viewModel.CreateNewTabAsync();
        var tab = viewModel.Tabs[0];

        // Make the tab dirty by updating content
        tabManager.UpdateTabContent(tab.TabId, "dirty content");

        // Verify tab is dirty before close
        var tabWithStats = tabManager.GetTab(tab.TabId);
        Assert.True(tabWithStats?.Tab.IsDirty);

        await viewModel.CloseTabAsync(tab, "dirty content");

        // Verify the tab was closed (removed from tab list)
        Assert.Empty(viewModel.Tabs);
    }

    #endregion

    #region CloseAllTabsAsync Tests

    [Fact]
    public async Task CloseAllTabsAsync_ClosesAllTabs()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();

        await viewModel.CloseAllTabsAsync();

        Assert.Empty(viewModel.Tabs);
        Assert.False(viewModel.HasTabs);
    }

    [Fact]
    public async Task CloseAllTabsAsync_ClearsSelection()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();

        await viewModel.CloseAllTabsAsync();

        Assert.Null(viewModel.SelectedTab);
    }

    #endregion

    #region SelectTab Tests

    [Fact]
    public void SelectTab_SetsSelectedTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tabWithStats = TestHelpers.CreateTabWithStats("content", "title");
        var tabViewModel = new TabItemViewModel(tabWithStats, _ => { });
        viewModel.Tabs.Add(tabViewModel);

        viewModel.SelectTab(tabViewModel);

        Assert.Equal(tabViewModel, viewModel.SelectedTab);
        Assert.True(tabViewModel.IsSelected);
    }

    [Fact]
    public void SelectTab_DeselectsPreviouslySelectedTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);

        viewModel.SelectTab(tab1);
        viewModel.SelectTab(tab2);

        Assert.False(tab1.IsSelected);
        Assert.True(tab2.IsSelected);
    }

    [Fact]
    public void SelectTab_WithNull_ClearsSelection()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        viewModel.Tabs.Add(tab);
        viewModel.SelectTab(tab);

        viewModel.SelectTab(null);

        Assert.Null(viewModel.SelectedTab);
        Assert.False(tab.IsSelected);
    }

    [Fact]
    public void SelectTab_RaisesTabSelectedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        viewModel.Tabs.Add(tab);
        TabItemViewModel? selectedTab = null;
        viewModel.TabSelected += (_, t) => selectedTab = t;

        viewModel.SelectTab(tab);

        Assert.Equal(tab, selectedTab);
    }

    #endregion

    #region SelectNextTab Tests

    [Fact]
    public void SelectNextTab_WithNoTabs_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        viewModel.SelectNextTab();

        Assert.Null(viewModel.SelectedTab);
    }

    [Fact]
    public void SelectNextTab_SelectsNextTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);
        viewModel.SelectTab(tab1);

        viewModel.SelectNextTab();

        Assert.Equal(tab2, viewModel.SelectedTab);
    }

    [Fact]
    public void SelectNextTab_WrapsAround_WhenAtEnd()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);
        viewModel.SelectTab(tab2);

        viewModel.SelectNextTab();

        Assert.Equal(tab1, viewModel.SelectedTab);
    }

    [Fact]
    public void SelectNextTab_WithSingleTab_SelectsSameTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        viewModel.Tabs.Add(tab);
        viewModel.SelectTab(tab);

        viewModel.SelectNextTab();

        Assert.Equal(tab, viewModel.SelectedTab);
    }

    [Fact]
    public void SelectNextTab_WithNoSelection_SelectsFirstTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);

        viewModel.SelectNextTab();

        Assert.Equal(tab1, viewModel.SelectedTab);
    }

    #endregion

    #region SelectPreviousTab Tests

    [Fact]
    public void SelectPreviousTab_WithNoTabs_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        viewModel.SelectPreviousTab();

        Assert.Null(viewModel.SelectedTab);
    }

    [Fact]
    public void SelectPreviousTab_SelectsPreviousTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);
        viewModel.SelectTab(tab2);

        viewModel.SelectPreviousTab();

        Assert.Equal(tab1, viewModel.SelectedTab);
    }

    [Fact]
    public void SelectPreviousTab_WrapsAround_WhenAtBeginning()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab1 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content1"), _ => { });
        var tab2 = new TabItemViewModel(TestHelpers.CreateTabWithStats("content2"), _ => { });
        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);
        viewModel.SelectTab(tab1);

        viewModel.SelectPreviousTab();

        Assert.Equal(tab2, viewModel.SelectedTab);
    }

    [Fact]
    public void SelectPreviousTab_WithSingleTab_SelectsSameTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        viewModel.Tabs.Add(tab);
        viewModel.SelectTab(tab);

        viewModel.SelectPreviousTab();

        Assert.Equal(tab, viewModel.SelectedTab);
    }

    #endregion

    #region DuplicateTabAsync Tests

    [Fact]
    public async Task DuplicateTabAsync_WithNull_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        await viewModel.DuplicateTabAsync(null!);

        Assert.Empty(viewModel.Tabs);
    }

    [Fact]
    public async Task DuplicateTabAsync_CreatesDuplicateTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var originalTab = viewModel.Tabs[0];
        tabManager.UpdateTabContent(originalTab.TabId, "test content");

        await viewModel.DuplicateTabAsync(originalTab);

        Assert.Equal(2, viewModel.Tabs.Count);
    }

    [Fact]
    public async Task DuplicateTabAsync_InsertsAfterOriginal()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var firstTab = viewModel.Tabs[0];
        tabManager.UpdateTabContent(firstTab.TabId, "content");

        await viewModel.DuplicateTabAsync(firstTab);

        // Should have 3 tabs with duplicate at index 1
        Assert.Equal(3, viewModel.Tabs.Count);
    }

    [Fact]
    public async Task DuplicateTabAsync_SelectsDuplicatedTab()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var originalTab = viewModel.Tabs[0];
        tabManager.UpdateTabContent(originalTab.TabId, "content");

        await viewModel.DuplicateTabAsync(originalTab);

        Assert.NotNull(viewModel.SelectedTab);
        Assert.NotEqual(originalTab.TabId, viewModel.SelectedTab.TabId);
    }

    [Fact]
    public async Task DuplicateTabAsync_RaisesDuplicateTabRequestedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var originalTab = viewModel.Tabs[0];
        tabManager.UpdateTabContent(originalTab.TabId, "content");
        TabItemViewModel? duplicatedTab = null;
        viewModel.DuplicateTabRequested += (_, tab) => duplicatedTab = tab;

        await viewModel.DuplicateTabAsync(originalTab);

        Assert.NotNull(duplicatedTab);
    }

    [Fact]
    public async Task DuplicateTabAsync_RaisesTabsChangedEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var originalTab = viewModel.Tabs[0];
        tabManager.UpdateTabContent(originalTab.TabId, "content");
        var eventRaised = false;
        viewModel.TabsChanged += (_, _) => eventRaised = true;

        await viewModel.DuplicateTabAsync(originalTab);

        Assert.True(eventRaised);
    }

    #endregion

    #region OpenDocumentAsync Tests

    [Fact]
    public async Task OpenDocumentAsync_WithNewDocument_AddsTab()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        var document = TestHelpers.CreateDocument("content", "title");
        mockRepo.AddDocument(document);
        var viewModel = new TabListViewModel(tabManager);

        var result = await viewModel.OpenDocumentAsync(document.Id);

        Assert.NotNull(result);
        Assert.Single(viewModel.Tabs);
    }

    [Fact]
    public async Task OpenDocumentAsync_WithExistingTab_SelectsExisting()
    {
        var (tabManager, mockRepo, mockMetadata) = CreateTabManagerWithMocks();
        await tabManager.InitializeAsync();
        var document = TestHelpers.CreateDocument("content", "title");
        mockRepo.AddDocument(document);
        var viewModel = new TabListViewModel(tabManager);

        await viewModel.OpenDocumentAsync(document.Id);
        await viewModel.OpenDocumentAsync(document.Id);

        Assert.Single(viewModel.Tabs);
    }

    [Fact]
    public async Task OpenDocumentAsync_WithMissingDocument_ReturnsNull()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);

        var result = await viewModel.OpenDocumentAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Empty(viewModel.Tabs);
    }

    #endregion

    #region RefreshTabStats Tests

    [Fact]
    public async Task RefreshTabStats_UpdatesTabInPlace()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var tab = viewModel.Tabs[0];
        var originalReference = tab;

        // Update content
        tabManager.UpdateTabContent(tab.TabId, "new content");
        viewModel.RefreshTabStats(tab.TabId);

        // Should be same object (updated in place)
        Assert.Same(originalReference, viewModel.Tabs[0]);
    }

    [Fact]
    public async Task RefreshTabStats_WithInvalidTabId_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();

        // Should not throw
        viewModel.RefreshTabStats(Guid.NewGuid());
    }

    #endregion

    #region RequestEditTitle Tests

    [Fact]
    public async Task RequestEditTitle_WithSelectedTab_RaisesEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        var eventRaised = false;
        viewModel.EditTitleRequested += (_, _) => eventRaised = true;

        viewModel.RequestEditTitle();

        Assert.True(eventRaised);
    }

    [Fact]
    public void RequestEditTitle_WithNoSelectedTab_DoesNotRaiseEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var eventRaised = false;
        viewModel.EditTitleRequested += (_, _) => eventRaised = true;

        viewModel.RequestEditTitle();

        Assert.False(eventRaised);
    }

    #endregion

    #region RequestCopyToClipboard Tests

    [Fact]
    public void RequestCopyToClipboard_WithValidTab_RaisesEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        TabItemViewModel? eventTab = null;
        viewModel.CopyToClipboardRequested += (_, t) => eventTab = t;

        viewModel.RequestCopyToClipboard(tab);

        Assert.Equal(tab, eventTab);
    }

    [Fact]
    public void RequestCopyToClipboard_WithNull_DoesNotRaiseEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var eventRaised = false;
        viewModel.CopyToClipboardRequested += (_, _) => eventRaised = true;

        viewModel.RequestCopyToClipboard(null!);

        Assert.False(eventRaised);
    }

    #endregion

    #region RequestSaveAs Tests

    [Fact]
    public void RequestSaveAs_WithValidTab_RaisesEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var tab = new TabItemViewModel(TestHelpers.CreateTabWithStats("content"), _ => { });
        TabItemViewModel? eventTab = null;
        viewModel.SaveAsRequested += (_, t) => eventTab = t;

        viewModel.RequestSaveAs(tab);

        Assert.Equal(tab, eventTab);
    }

    [Fact]
    public void RequestSaveAs_WithNull_DoesNotRaiseEvent()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        var eventRaised = false;
        viewModel.SaveAsRequested += (_, _) => eventRaised = true;

        viewModel.RequestSaveAs(null!);

        Assert.False(eventRaised);
    }

    #endregion

    #region CloseTabsAbove/Below Tests

    [Fact]
    public async Task CloseTabsAboveAsync_ClosesTabsAboveTarget()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var middleTab = viewModel.Tabs[1];

        await viewModel.CloseTabsAboveAsync(middleTab);

        Assert.Equal(2, viewModel.Tabs.Count);
        Assert.Equal(middleTab.TabId, viewModel.Tabs[0].TabId);
    }

    [Fact]
    public async Task CloseTabsAboveAsync_WithFirstTab_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var firstTab = viewModel.Tabs[0];

        await viewModel.CloseTabsAboveAsync(firstTab);

        Assert.Equal(2, viewModel.Tabs.Count);
    }

    [Fact]
    public async Task CloseTabsBelowAsync_ClosesTabsBelowTarget()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var middleTab = viewModel.Tabs[1];

        await viewModel.CloseTabsBelowAsync(middleTab);

        Assert.Equal(2, viewModel.Tabs.Count);
    }

    [Fact]
    public async Task CloseTabsBelowAsync_WithLastTab_DoesNothing()
    {
        var tabManager = CreateInitializedTabManager();
        var viewModel = new TabListViewModel(tabManager);
        await viewModel.CreateNewTabAsync();
        await viewModel.CreateNewTabAsync();
        var lastTab = viewModel.Tabs[1];

        await viewModel.CloseTabsBelowAsync(lastTab);

        Assert.Equal(2, viewModel.Tabs.Count);
    }

    #endregion

    #region Helper Methods

    private static TabManager CreateInitializedTabManager()
    {
        var mockRepo = new MockDocumentRepository();
        var mockMetadata = new MockMetadataStore();
        var tabManager = new TabManager(mockRepo, mockMetadata);
        tabManager.InitializeAsync().GetAwaiter().GetResult();
        return tabManager;
    }

    private static (TabManager, MockDocumentRepository, MockMetadataStore) CreateTabManagerWithMocks()
    {
        var mockRepo = new MockDocumentRepository();
        var mockMetadata = new MockMetadataStore();
        var tabManager = new TabManager(mockRepo, mockMetadata);
        return (tabManager, mockRepo, mockMetadata);
    }

    #endregion

    #region Mock Classes

    private class MockDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents = new();
        public Dictionary<Guid, string> UpdatedDocuments { get; } = new();

        public void AddDocument(Document document)
        {
            _documents[document.Id] = document;
        }

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

    #endregion
}
