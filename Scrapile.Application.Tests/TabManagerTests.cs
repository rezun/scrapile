using Scrapile.Application.Services;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Application.Tests;

public class TabManagerTests
{
    private readonly MockDocumentRepository _mockRepository;
    private readonly MockMetadataStore _mockMetadataStore;
    private readonly TabManager _tabManager;

    public TabManagerTests()
    {
        _mockRepository = new MockDocumentRepository();
        _mockMetadataStore = new MockMetadataStore();
        _tabManager = new TabManager(_mockRepository, _mockMetadataStore);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TabManager(null!, _mockMetadataStore));
    }

    [Fact]
    public void Constructor_WithNullMetadataStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TabManager(_mockRepository, null!));
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsync_WithNoOpenTabs_ReturnsEmptyList()
    {
        // Act
        var tabs = await _tabManager.InitializeAsync();

        // Assert
        Assert.Empty(tabs);
        Assert.Equal(0, _tabManager.TabCount);
    }

    [Fact]
    public async Task InitializeAsync_WithOpenTabs_RestoresTabs()
    {
        // Arrange
        var doc1 = CreateDocument("Content 1", "Title 1");
        var doc2 = CreateDocument("Content 2", null);
        _mockRepository.AddDocument(doc1);
        _mockRepository.AddDocument(doc2);
        _mockMetadataStore.SetOpenTabs(new List<Guid> { doc1.Id, doc2.Id });

        // Act
        var tabs = await _tabManager.InitializeAsync();

        // Assert
        Assert.Equal(2, tabs.Count);
        Assert.Equal(doc1.Id, tabs[0].Tab.Document.Id);
        Assert.Equal(doc2.Id, tabs[1].Tab.Document.Id);
        Assert.Equal(0, tabs[0].Tab.Order);
        Assert.Equal(1, tabs[1].Tab.Order);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingDocument_SkipsAndRemovesFromMetadata()
    {
        // Arrange
        var existingDoc = CreateDocument("Content", "Title");
        var missingDocId = Guid.NewGuid();
        _mockRepository.AddDocument(existingDoc);
        _mockMetadataStore.SetOpenTabs(new List<Guid> { missingDocId, existingDoc.Id });

        // Act
        var tabs = await _tabManager.InitializeAsync();

        // Assert
        Assert.Single(tabs);
        Assert.Equal(existingDoc.Id, tabs[0].Tab.Document.Id);
        Assert.Contains(missingDocId, _mockMetadataStore.RemovedOpenTabs);
    }

    [Fact]
    public async Task InitializeAsync_PreservesTabOrder()
    {
        // Arrange
        var doc1 = CreateDocument("Content 1");
        var doc2 = CreateDocument("Content 2");
        var doc3 = CreateDocument("Content 3");
        _mockRepository.AddDocument(doc1);
        _mockRepository.AddDocument(doc2);
        _mockRepository.AddDocument(doc3);
        _mockMetadataStore.SetOpenTabs(new List<Guid> { doc3.Id, doc1.Id, doc2.Id });

        // Act
        var tabs = await _tabManager.InitializeAsync();

        // Assert
        Assert.Equal(doc3.Id, tabs[0].Tab.Document.Id);
        Assert.Equal(doc1.Id, tabs[1].Tab.Document.Id);
        Assert.Equal(doc2.Id, tabs[2].Tab.Document.Id);
    }

    #endregion

    #region CreateTabAsync Tests

    [Fact]
    public async Task CreateTabAsync_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _tabManager.CreateTabAsync());
    }

    [Fact]
    public async Task CreateTabAsync_CreatesNewDocumentAndTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var tab = await _tabManager.CreateTabAsync();

        // Assert
        Assert.NotNull(tab);
        Assert.Equal(1, _tabManager.TabCount);
        Assert.Equal(0, tab.Tab.Order);
        Assert.False(tab.Tab.IsDirty);
        Assert.Single(_mockRepository.CreatedDocuments);
    }

    [Fact]
    public async Task CreateTabAsync_AddsTabAtEnd()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        await _tabManager.CreateTabAsync();
        await _tabManager.CreateTabAsync();

        // Act
        var tab = await _tabManager.CreateTabAsync();

        // Assert
        Assert.Equal(3, _tabManager.TabCount);
        Assert.Equal(2, tab.Tab.Order);
    }

    [Fact]
    public async Task CreateTabAsync_PersistsToMetadata()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var tab = await _tabManager.CreateTabAsync();

        // Assert
        Assert.Contains(tab.Tab.Document.Id, _mockMetadataStore.AddedOpenTabs.Select(t => t.DocumentId));
    }

    #endregion

    #region OpenDocumentInTabAsync Tests

    [Fact]
    public async Task OpenDocumentInTabAsync_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tabManager.OpenDocumentInTabAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task OpenDocumentInTabAsync_WithValidDocument_OpensTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var doc = CreateDocument("Test content", "Test Title");
        _mockRepository.AddDocument(doc);

        // Act
        var tab = await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Assert
        Assert.NotNull(tab);
        Assert.Equal(doc.Id, tab.Tab.Document.Id);
        Assert.Equal(doc.Content, tab.Tab.Content);
        Assert.Equal("Test Title", tab.Tab.Document.Title);
    }

    [Fact]
    public async Task OpenDocumentInTabAsync_WithMissingDocument_ReturnsNull()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var tab = await _tabManager.OpenDocumentInTabAsync(Guid.NewGuid());

        // Assert
        Assert.Null(tab);
    }

    [Fact]
    public async Task OpenDocumentInTabAsync_AlreadyOpen_ReturnsExistingTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var doc = CreateDocument("Test content");
        _mockRepository.AddDocument(doc);
        var firstTab = await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Act
        var secondTab = await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Assert
        Assert.Equal(firstTab!.Tab.TabId, secondTab!.Tab.TabId);
        Assert.Equal(1, _tabManager.TabCount);
    }

    [Fact]
    public async Task OpenDocumentInTabAsync_RemovesFromRecentlyClosed()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var doc = CreateDocument("Test content");
        _mockRepository.AddDocument(doc);

        // Act
        await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Assert
        Assert.Contains(doc.Id, _mockMetadataStore.RemovedFromRecentlyClosed);
    }

    #endregion

    #region CloseTabAsync Tests

    [Fact]
    public async Task CloseTabAsync_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tabManager.CloseTabAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseTabAsync_WithValidTab_RemovesTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Assert
        Assert.True(result);
        Assert.Equal(0, _tabManager.TabCount);
    }

    [Fact]
    public async Task CloseTabAsync_WithInvalidTabId_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var result = await _tabManager.CloseTabAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CloseTabAsync_AddsToRecentlyClosed()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Assert
        Assert.Contains(tab.Tab.Document.Id, _mockMetadataStore.AddedToRecentlyClosed.Select(r => r.DocumentId));
    }

    [Fact]
    public async Task CloseTabAsync_RemovesFromOpenTabs()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Assert
        Assert.Contains(tab.Tab.Document.Id, _mockMetadataStore.RemovedOpenTabs);
    }

    [Fact]
    public async Task CloseTabAsync_UpdatesRemainingTabOrders()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act - close the middle tab
        await _tabManager.CloseTabAsync(tab2.Tab.TabId);

        // Assert
        var tabs = _tabManager.GetOpenTabs();
        Assert.Equal(2, tabs.Count);
        Assert.Equal(0, tabs[0].Tab.Order);
        Assert.Equal(1, tabs[1].Tab.Order);
    }

    #endregion

    #region DuplicateTabAsync Tests

    [Fact]
    public async Task DuplicateTabAsync_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tabManager.DuplicateTabAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DuplicateTabAsync_WithValidTab_CreatesDuplicate()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var originalTab = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(originalTab.Tab.TabId, "Original content");

        // Act
        var duplicateTab = await _tabManager.DuplicateTabAsync(originalTab.Tab.TabId);

        // Assert
        Assert.NotNull(duplicateTab);
        Assert.Equal(2, _tabManager.TabCount);
        Assert.NotEqual(originalTab.Tab.TabId, duplicateTab.Tab.TabId);
        Assert.NotEqual(originalTab.Tab.Document.Id, duplicateTab.Tab.Document.Id);
        Assert.Equal(originalTab.Tab.Content, duplicateTab.Tab.Content);
    }

    [Fact]
    public async Task DuplicateTabAsync_WithTitle_AppendsCopy()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var doc = CreateDocument("Test content", "Original Title");
        _mockRepository.AddDocument(doc);
        var originalTab = await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Act
        var duplicateTab = await _tabManager.DuplicateTabAsync(originalTab!.Tab.TabId);

        // Assert
        var createdDoc = _mockRepository.CreatedDocuments.Last();
        Assert.Equal("Original Title - Copy", createdDoc.Title);
    }

    [Fact]
    public async Task DuplicateTabAsync_WithoutTitle_NoTitleOnCopy()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        var duplicateTab = await _tabManager.DuplicateTabAsync(tab.Tab.TabId);

        // Assert
        Assert.Null(duplicateTab!.Tab.Document.Title);
    }

    [Fact]
    public async Task DuplicateTabAsync_InsertsAfterSource()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act - duplicate the middle tab
        var duplicateTab = await _tabManager.DuplicateTabAsync(tab2.Tab.TabId);

        // Assert
        var tabs = _tabManager.GetOpenTabs();
        Assert.Equal(4, tabs.Count);
        Assert.Equal(tab1.Tab.TabId, tabs[0].Tab.TabId);
        Assert.Equal(tab2.Tab.TabId, tabs[1].Tab.TabId);
        Assert.Equal(duplicateTab!.Tab.TabId, tabs[2].Tab.TabId);
        Assert.Equal(tab3.Tab.TabId, tabs[3].Tab.TabId);
    }

    [Fact]
    public async Task DuplicateTabAsync_WithInvalidTabId_ReturnsNull()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var result = await _tabManager.DuplicateTabAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ReorderTabAsync Tests

    [Fact]
    public async Task ReorderTabAsync_MovesTabToNewPosition()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act - move first tab to last position
        var result = await _tabManager.ReorderTabAsync(tab1.Tab.TabId, 2);

        // Assert
        Assert.True(result);
        var tabs = _tabManager.GetOpenTabs();
        Assert.Equal(tab2.Tab.TabId, tabs[0].Tab.TabId);
        Assert.Equal(tab3.Tab.TabId, tabs[1].Tab.TabId);
        Assert.Equal(tab1.Tab.TabId, tabs[2].Tab.TabId);
    }

    [Fact]
    public async Task ReorderTabAsync_WithInvalidTabId_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.ReorderTabAsync(Guid.NewGuid(), 0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReorderTabAsync_WithInvalidPosition_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.ReorderTabAsync(tab.Tab.TabId, 5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReorderTabAsync_ToSamePosition_ReturnsTrue()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.ReorderTabAsync(tab.Tab.TabId, 0);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReorderTabAsync_PersistsToMetadata()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        _mockMetadataStore.UpdatedTabOrders.Clear();

        // Act
        await _tabManager.ReorderTabAsync(tab1.Tab.TabId, 1);

        // Assert
        Assert.NotEmpty(_mockMetadataStore.UpdatedTabOrders);
    }

    #endregion

    #region ReorderTabsAsync Tests

    [Fact]
    public async Task ReorderTabsAsync_WithValidOrder_ReordersAll()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act - reverse the order
        var result = await _tabManager.ReorderTabsAsync(new[] { tab3.Tab.TabId, tab2.Tab.TabId, tab1.Tab.TabId });

        // Assert
        Assert.True(result);
        var tabs = _tabManager.GetOpenTabs();
        Assert.Equal(tab3.Tab.TabId, tabs[0].Tab.TabId);
        Assert.Equal(tab2.Tab.TabId, tabs[1].Tab.TabId);
        Assert.Equal(tab1.Tab.TabId, tabs[2].Tab.TabId);
    }

    [Fact]
    public async Task ReorderTabsAsync_WithMissingTabId_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.ReorderTabsAsync(new[] { tab1.Tab.TabId, Guid.NewGuid() });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReorderTabsAsync_WithWrongCount_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        await _tabManager.CreateTabAsync();

        // Act
        var result = await _tabManager.ReorderTabsAsync(new[] { tab1.Tab.TabId });

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetOpenTabs Tests

    [Fact]
    public async Task GetOpenTabs_ReturnsTabsInOrder()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act
        var tabs = _tabManager.GetOpenTabs();

        // Assert
        Assert.Equal(3, tabs.Count);
        Assert.Equal(tab1.Tab.TabId, tabs[0].Tab.TabId);
        Assert.Equal(tab2.Tab.TabId, tabs[1].Tab.TabId);
        Assert.Equal(tab3.Tab.TabId, tabs[2].Tab.TabId);
    }

    [Fact]
    public async Task GetOpenTabs_ReturnsTabsWithStats()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(tab.Tab.TabId, "Hello world test");

        // Act
        var tabs = _tabManager.GetOpenTabs();

        // Assert
        Assert.Equal(3, tabs[0].WordCount);
        Assert.Equal(16, tabs[0].CharacterCount);
        Assert.Equal("3 words", tabs[0].FormattedWordCount);
    }

    #endregion

    #region UpdateTabContent Tests

    [Fact]
    public async Task UpdateTabContent_UpdatesContent()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        var result = _tabManager.UpdateTabContent(tab.Tab.TabId, "New content");

        // Assert
        Assert.True(result);
        var updatedTab = _tabManager.GetTab(tab.Tab.TabId);
        Assert.Equal("New content", updatedTab!.Tab.Content);
    }

    [Fact]
    public async Task UpdateTabContent_SetsIsDirty()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();

        // Act
        _tabManager.UpdateTabContent(tab.Tab.TabId, "New content");

        // Assert
        var updatedTab = _tabManager.GetTab(tab.Tab.TabId);
        Assert.True(updatedTab!.Tab.IsDirty);
    }

    [Fact]
    public async Task UpdateTabContent_WithInvalidTabId_ReturnsFalse()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var result = _tabManager.UpdateTabContent(Guid.NewGuid(), "Content");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region MarkTabAsSaved Tests

    [Fact]
    public async Task MarkTabAsSaved_ClearsDirtyFlag()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(tab.Tab.TabId, "Modified content");

        // Act
        var result = _tabManager.MarkTabAsSaved(tab.Tab.TabId);

        // Assert
        Assert.True(result);
        var updatedTab = _tabManager.GetTab(tab.Tab.TabId);
        Assert.False(updatedTab!.Tab.IsDirty);
    }

    [Fact]
    public async Task MarkTabAsSaved_UpdatesDocumentContent()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(tab.Tab.TabId, "New content");

        // Act
        _tabManager.MarkTabAsSaved(tab.Tab.TabId);

        // Assert
        var updatedTab = _tabManager.GetTab(tab.Tab.TabId);
        Assert.Equal("New content", updatedTab!.Tab.Document.Content);
    }

    #endregion

    #region GetDirtyTabs Tests

    [Fact]
    public async Task GetDirtyTabs_ReturnsOnlyDirtyTabs()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(tab1.Tab.TabId, "Modified 1");
        _tabManager.UpdateTabContent(tab3.Tab.TabId, "Modified 3");

        // Act
        var dirtyTabs = _tabManager.GetDirtyTabs();

        // Assert
        Assert.Equal(2, dirtyTabs.Count);
        Assert.Contains(dirtyTabs, t => t.Tab.TabId == tab1.Tab.TabId);
        Assert.Contains(dirtyTabs, t => t.Tab.TabId == tab3.Tab.TabId);
    }

    [Fact]
    public async Task HasDirtyTabs_ReturnsTrueWhenDirty()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        _tabManager.UpdateTabContent(tab.Tab.TabId, "Modified");

        // Assert
        Assert.True(_tabManager.HasDirtyTabs);
    }

    [Fact]
    public async Task HasDirtyTabs_ReturnsFalseWhenClean()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        await _tabManager.CreateTabAsync();

        // Assert
        Assert.False(_tabManager.HasDirtyTabs);
    }

    #endregion

    #region GetTab Tests

    [Fact]
    public async Task GetTab_WithValidId_ReturnsTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var createdTab = await _tabManager.CreateTabAsync();

        // Act
        var tab = _tabManager.GetTab(createdTab.Tab.TabId);

        // Assert
        Assert.NotNull(tab);
        Assert.Equal(createdTab.Tab.TabId, tab.Tab.TabId);
    }

    [Fact]
    public async Task GetTab_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var tab = _tabManager.GetTab(Guid.NewGuid());

        // Assert
        Assert.Null(tab);
    }

    #endregion

    #region GetTabByDocumentId Tests

    [Fact]
    public async Task GetTabByDocumentId_WithValidId_ReturnsTab()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var doc = CreateDocument("Test content");
        _mockRepository.AddDocument(doc);
        await _tabManager.OpenDocumentInTabAsync(doc.Id);

        // Act
        var tab = _tabManager.GetTabByDocumentId(doc.Id);

        // Assert
        Assert.NotNull(tab);
        Assert.Equal(doc.Id, tab.Tab.Document.Id);
    }

    [Fact]
    public async Task GetTabByDocumentId_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var tab = _tabManager.GetTabByDocumentId(Guid.NewGuid());

        // Assert
        Assert.Null(tab);
    }

    #endregion

    #region Helper Methods

    private static Document CreateDocument(string content, string? title = null)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            Filename = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.txt",
            Title = title,
            Content = content,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }

    #endregion

    #region Mock Classes

    private class MockDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents = new();
        public List<Document> CreatedDocuments { get; } = new();

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
            CreatedDocuments.Add(doc);
            return Task.FromResult(doc);
        }

        public Task<Document?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_documents.TryGetValue(id, out var doc) ? doc : null);
        }

        public Task<IEnumerable<Document>> GetAllAsync()
        {
            return Task.FromResult(_documents.Values.AsEnumerable());
        }

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

        public Task<IEnumerable<Document>> SearchAsync(string query)
        {
            return Task.FromResult(_documents.Values.Where(d =>
                (d.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private class MockMetadataStore : IMetadataStore
    {
        private List<Guid> _openTabs = new();
        public List<(Guid DocumentId, int Order)> AddedOpenTabs { get; } = new();
        public List<Guid> RemovedOpenTabs { get; } = new();
        public List<(Guid DocumentId, DateTime ClosedAt)> AddedToRecentlyClosed { get; } = new();
        public List<Guid> RemovedFromRecentlyClosed { get; } = new();
        public List<List<Guid>> UpdatedTabOrders { get; } = new();

        public void SetOpenTabs(List<Guid> openTabs)
        {
            _openTabs = openTabs;
        }

        public Task<Metadata> LoadAsync()
        {
            return Task.FromResult(new Metadata());
        }

        public Task SaveAsync(Metadata metadata)
        {
            return Task.CompletedTask;
        }

        public Task AddDocumentAsync(Guid documentId, string? title)
        {
            return Task.CompletedTask;
        }

        public Task UpdateDocumentTitleAsync(Guid documentId, string? title)
        {
            return Task.CompletedTask;
        }

        public Task RemoveDocumentAsync(Guid documentId)
        {
            return Task.CompletedTask;
        }

        public Task AddOpenTabAsync(Guid documentId, int order)
        {
            AddedOpenTabs.Add((documentId, order));
            _openTabs.Add(documentId);
            return Task.CompletedTask;
        }

        public Task RemoveOpenTabAsync(Guid documentId)
        {
            RemovedOpenTabs.Add(documentId);
            _openTabs.Remove(documentId);
            return Task.CompletedTask;
        }

        public Task UpdateOpenTabsOrderAsync(IEnumerable<Guid> orderedDocumentIds)
        {
            var order = orderedDocumentIds.ToList();
            UpdatedTabOrders.Add(order);
            _openTabs = order;
            return Task.CompletedTask;
        }

        public Task AddRecentlyClosedAsync(Guid documentId, DateTime closedAt)
        {
            AddedToRecentlyClosed.Add((documentId, closedAt));
            return Task.CompletedTask;
        }

        public Task RemoveRecentlyClosedAsync(Guid documentId)
        {
            RemovedFromRecentlyClosed.Add(documentId);
            return Task.CompletedTask;
        }

        public Task<List<Guid>> GetOpenTabsAsync()
        {
            return Task.FromResult(_openTabs.ToList());
        }

        public Task<List<RecentlyClosedInfo>> GetRecentlyClosedAsync()
        {
            return Task.FromResult(new List<RecentlyClosedInfo>());
        }

        public Task<string?> GetDocumentTitleAsync(Guid documentId)
        {
            return Task.FromResult<string?>(null);
        }
    }

    #endregion
}
