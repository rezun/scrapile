using Scrapile.Application.Services;
using Scrapile.Infrastructure.Repositories;
using Scrapile.Infrastructure.Storage;

namespace Scrapile.Application.Tests;

/// <summary>
/// Integration tests for application services using real infrastructure implementations.
/// These tests verify the complete document lifecycle and service interactions.
/// </summary>
public class ServicesIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileSystemDocumentRepository _repository;
    private readonly JsonMetadataStore _metadataStore;
    private readonly DocumentService _documentService;
    private readonly TabManager _tabManager;
    private readonly AutoSaveService _autoSaveService;

    public ServicesIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ScrapileIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        _metadataStore = new JsonMetadataStore(_testDirectory);
        _repository = new FileSystemDocumentRepository(_testDirectory, _metadataStore);
        _documentService = new DocumentService(_repository);
        _tabManager = new TabManager(_repository, _metadataStore);
        _autoSaveService = new AutoSaveService(_repository, TimeSpan.FromMilliseconds(100));
    }

    public void Dispose()
    {
        _autoSaveService.Dispose();

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region Full Document Lifecycle Tests

    [Fact]
    public async Task FullDocumentLifecycle_CreateUpdateCloseReopenDelete()
    {
        // 1. Initialize TabManager
        await _tabManager.InitializeAsync();

        // 2. Create document via TabManager (simulates "New Tab")
        var tab = await _tabManager.CreateTabAsync();
        Assert.NotNull(tab);
        Assert.Equal(0, tab.WordCount);
        var documentId = tab.Tab.Document.Id;

        // 3. Update content
        const string content = "Hello world this is test content for the document";
        _tabManager.UpdateTabContent(tab.Tab.TabId, content);

        // Verify dirty state
        Assert.True(_tabManager.HasDirtyTabs);
        var dirtyTabs = _tabManager.GetDirtyTabs();
        Assert.Single(dirtyTabs);

        // 4. Save content via DocumentService
        await _documentService.UpdateContentAsync(documentId, content);
        _tabManager.MarkTabAsSaved(tab.Tab.TabId);
        Assert.False(_tabManager.HasDirtyTabs);

        // 5. Update title
        const string title = "My Test Document";
        await _documentService.UpdateTitleAsync(documentId, title);

        // Verify document has stats
        var doc = await _documentService.GetByIdAsync(documentId);
        Assert.NotNull(doc);
        Assert.Equal(title, doc.Document.Title);
        Assert.Equal(9, doc.WordCount); // "Hello world this is test content for the document"
        Assert.Equal(content, doc.Document.Content);

        // 6. Close tab
        await _tabManager.CloseTabAsync(tab.Tab.TabId);
        Assert.Equal(0, _tabManager.TabCount);

        // 7. Verify document in recently closed
        var recentlyClosed = await _tabManager.GetRecentlyClosedAsync();
        Assert.Single(recentlyClosed);
        Assert.Equal(documentId, recentlyClosed[0].DocumentId);
        Assert.Equal(title, recentlyClosed[0].Title);
        Assert.False(recentlyClosed[0].IsDeleted);

        // 8. Reopen via TabManager
        var reopenedTab = await _tabManager.ReopenLastClosedAsync();
        Assert.NotNull(reopenedTab);
        Assert.Equal(documentId, reopenedTab.Tab.Document.Id);
        Assert.Equal(content, reopenedTab.Tab.Content);
        Assert.Equal(1, _tabManager.TabCount);

        // Verify removed from recently closed
        recentlyClosed = await _tabManager.GetRecentlyClosedAsync();
        Assert.Empty(recentlyClosed);

        // 9. Delete document
        await _documentService.DeleteAsync(documentId);

        // Verify document is gone
        var deletedDoc = await _documentService.GetByIdAsync(documentId);
        Assert.Null(deletedDoc);
    }

    [Fact]
    public async Task DocumentService_CreatesDocumentWithStats()
    {
        // Act
        var doc = await _documentService.CreateAsync("Hello world test content", "Test Title");

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("Test Title", doc.Document.Title);
        Assert.Equal(4, doc.WordCount);
        Assert.Equal(24, doc.CharacterCount);
        Assert.Equal("Hello world test content", doc.ContentPreview);
        Assert.Equal("4 words", doc.FormattedWordCount);
    }

    [Fact]
    public async Task DocumentService_SearchReturnsDocumentsWithStats()
    {
        // Arrange
        await _documentService.CreateAsync("The quick brown fox", "Fox Document");
        await _documentService.CreateAsync("Lazy dog sleeping", "Dog Document");
        await _documentService.CreateAsync("The quick rabbit", null);

        // Act
        var results = await _documentService.SearchAsync("quick");

        // Assert
        var resultList = results.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, doc => Assert.True(doc.WordCount > 0));
    }

    [Fact]
    public async Task DocumentService_UpdateTitleNormalizesEmptyToNull()
    {
        // Arrange
        var doc = await _documentService.CreateAsync("Content", "Initial Title");

        // Act
        await _documentService.UpdateTitleAsync(doc.Document.Id, "");

        // Assert
        var updated = await _documentService.GetByIdAsync(doc.Document.Id);
        Assert.Null(updated!.Document.Title);
    }

    #endregion

    #region Auto-Save Debouncing Tests

    [Fact]
    public async Task AutoSave_DebouncingPreventsMultipleSaves()
    {
        // Arrange
        var doc = await _documentService.CreateAsync("Initial", null);

        // Act - simulate rapid typing
        for (int i = 0; i < 10; i++)
        {
            await _autoSaveService.ScheduleSaveAsync(doc.Document.Id, $"Content {i}");
            await Task.Delay(10); // Much shorter than debounce delay
        }

        // Wait for debounce to complete
        await Task.Delay(200);

        // Assert - only the last content should be saved
        var saved = await _documentService.GetByIdAsync(doc.Document.Id);
        Assert.Equal("Content 9", saved!.Document.Content);
    }

    [Fact]
    public async Task AutoSave_ImmediateSaveCancelsPending()
    {
        // Arrange
        var doc = await _documentService.CreateAsync("Initial", null);

        // Schedule a debounced save
        await _autoSaveService.ScheduleSaveAsync(doc.Document.Id, "Debounced content");
        Assert.True(_autoSaveService.HasPendingSave(doc.Document.Id));

        // Act - save immediately with different content
        await _autoSaveService.SaveImmediatelyAsync(doc.Document.Id, "Immediate content");

        // Assert
        Assert.False(_autoSaveService.HasPendingSave(doc.Document.Id));
        var saved = await _documentService.GetByIdAsync(doc.Document.Id);
        Assert.Equal("Immediate content", saved!.Document.Content);
    }

    [Fact]
    public async Task AutoSave_MultipleDocumentsTrackedIndependently()
    {
        // Arrange
        var doc1 = await _documentService.CreateAsync("Content 1", null);
        var doc2 = await _documentService.CreateAsync("Content 2", null);

        // Act
        await _autoSaveService.ScheduleSaveAsync(doc1.Document.Id, "Updated 1");
        await _autoSaveService.ScheduleSaveAsync(doc2.Document.Id, "Updated 2");

        // Both should have pending saves
        Assert.Equal(2, _autoSaveService.PendingSaveCount);

        // Wait for saves to complete
        await Task.Delay(200);

        // Assert
        Assert.Equal(0, _autoSaveService.PendingSaveCount);
        var saved1 = await _documentService.GetByIdAsync(doc1.Document.Id);
        var saved2 = await _documentService.GetByIdAsync(doc2.Document.Id);
        Assert.Equal("Updated 1", saved1!.Document.Content);
        Assert.Equal("Updated 2", saved2!.Document.Content);
    }

    #endregion

    #region TabManager State Persistence Tests

    [Fact]
    public async Task TabManager_StatePersistsAcrossRestarts()
    {
        // Arrange - Create tabs in first "session"
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Update content
        _tabManager.UpdateTabContent(tab1.Tab.TabId, "Content 1");
        _tabManager.UpdateTabContent(tab2.Tab.TabId, "Content 2");
        _tabManager.UpdateTabContent(tab3.Tab.TabId, "Content 3");

        // Save content
        await _documentService.UpdateContentAsync(tab1.Tab.Document.Id, "Content 1");
        await _documentService.UpdateContentAsync(tab2.Tab.Document.Id, "Content 2");
        await _documentService.UpdateContentAsync(tab3.Tab.Document.Id, "Content 3");

        // Reorder tabs
        await _tabManager.ReorderTabsAsync(new[] { tab3.Tab.TabId, tab1.Tab.TabId, tab2.Tab.TabId });

        var doc1Id = tab1.Tab.Document.Id;
        var doc2Id = tab2.Tab.Document.Id;
        var doc3Id = tab3.Tab.Document.Id;

        // Simulate restart - create new TabManager instance
        var newMetadataStore = new JsonMetadataStore(_testDirectory);
        var newRepository = new FileSystemDocumentRepository(_testDirectory, newMetadataStore);
        var newTabManager = new TabManager(newRepository, newMetadataStore);

        // Act - Initialize new session
        var restoredTabs = await newTabManager.InitializeAsync();

        // Assert - tabs restored in correct order
        Assert.Equal(3, restoredTabs.Count);
        Assert.Equal(doc3Id, restoredTabs[0].Tab.Document.Id);
        Assert.Equal(doc1Id, restoredTabs[1].Tab.Document.Id);
        Assert.Equal(doc2Id, restoredTabs[2].Tab.Document.Id);

        // Verify content
        Assert.Equal("Content 3", restoredTabs[0].Tab.Content);
        Assert.Equal("Content 1", restoredTabs[1].Tab.Content);
        Assert.Equal("Content 2", restoredTabs[2].Tab.Content);
    }

    [Fact]
    public async Task TabManager_HandlesDeletedDocumentsOnRestore()
    {
        // Arrange - Create tabs
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var doc2Id = tab2.Tab.Document.Id;

        // Delete doc2's file directly (simulate external deletion)
        var files = Directory.GetFiles(_testDirectory, "*.txt");
        var doc2File = files.First(f => f.Contains(doc2Id.ToString("N").Substring(0, 8)));
        File.Delete(doc2File);

        // Simulate restart
        var newMetadataStore = new JsonMetadataStore(_testDirectory);
        var newRepository = new FileSystemDocumentRepository(_testDirectory, newMetadataStore);
        var newTabManager = new TabManager(newRepository, newMetadataStore);

        // Act
        var restoredTabs = await newTabManager.InitializeAsync();

        // Assert - only doc1 should be restored
        Assert.Single(restoredTabs);
        Assert.Equal(tab1.Tab.Document.Id, restoredTabs[0].Tab.Document.Id);
    }

    #endregion

    #region Recently Closed Service Tests

    [Fact]
    public async Task RecentlyClosed_ClosingTabAddsToList()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        await _documentService.UpdateTitleAsync(tab.Tab.Document.Id, "Test Title");
        _tabManager.UpdateTabContent(tab.Tab.TabId, "Test content");
        await _documentService.UpdateContentAsync(tab.Tab.Document.Id, "Test content");

        // Act
        await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Assert
        var recentlyClosed = await _tabManager.GetRecentlyClosedAsync();
        Assert.Single(recentlyClosed);
        Assert.Equal(tab.Tab.Document.Id, recentlyClosed[0].DocumentId);
        Assert.Equal("Test Title", recentlyClosed[0].Title);
        Assert.Equal("Test content", recentlyClosed[0].ContentPreview);
        Assert.False(recentlyClosed[0].IsDeleted);
        Assert.NotEmpty(recentlyClosed[0].FormattedClosedTime);
    }

    [Fact]
    public async Task RecentlyClosed_ReopenRemovesFromList()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        var docId = tab.Tab.Document.Id;
        await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Act
        await _tabManager.ReopenLastClosedAsync();

        // Assert
        var recentlyClosed = await _tabManager.GetRecentlyClosedAsync();
        Assert.Empty(recentlyClosed);
        Assert.Equal(1, _tabManager.TabCount);
    }

    [Fact]
    public async Task RecentlyClosed_MultipleTabsStackCorrectly()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        var doc1Id = tab1.Tab.Document.Id;
        var doc2Id = tab2.Tab.Document.Id;
        var doc3Id = tab3.Tab.Document.Id;

        // Close in order: 1, 2, 3
        await _tabManager.CloseTabAsync(tab1.Tab.TabId);
        await _tabManager.CloseTabAsync(tab2.Tab.TabId);
        await _tabManager.CloseTabAsync(tab3.Tab.TabId);

        // Act - reopen should return in reverse order (LIFO)
        var reopened3 = await _tabManager.ReopenLastClosedAsync();
        var reopened2 = await _tabManager.ReopenLastClosedAsync();
        var reopened1 = await _tabManager.ReopenLastClosedAsync();

        // Assert
        Assert.Equal(doc3Id, reopened3!.Tab.Document.Id);
        Assert.Equal(doc2Id, reopened2!.Tab.Document.Id);
        Assert.Equal(doc1Id, reopened1!.Tab.Document.Id);
    }

    [Fact]
    public async Task RecentlyClosed_HandlesMissingDocumentsGracefully()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();

        var doc1Id = tab1.Tab.Document.Id;
        var doc2Id = tab2.Tab.Document.Id;

        // Close both
        await _tabManager.CloseTabAsync(tab1.Tab.TabId);
        await _tabManager.CloseTabAsync(tab2.Tab.TabId);

        // Delete doc2's file
        await _documentService.DeleteAsync(doc2Id);

        // Act - should skip deleted doc2 and return doc1
        var reopened = await _tabManager.ReopenLastClosedAsync();

        // Assert
        Assert.NotNull(reopened);
        Assert.Equal(doc1Id, reopened.Tab.Document.Id);
    }

    [Fact]
    public async Task RecentlyClosed_ReopenSpecificDocument()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        var doc2Id = tab2.Tab.Document.Id;

        // Close all
        await _tabManager.CloseTabAsync(tab1.Tab.TabId);
        await _tabManager.CloseTabAsync(tab2.Tab.TabId);
        await _tabManager.CloseTabAsync(tab3.Tab.TabId);

        // Act - reopen specific document (not the most recent)
        var reopened = await _tabManager.ReopenDocumentFromRecentlyClosedAsync(doc2Id);

        // Assert
        Assert.NotNull(reopened);
        Assert.Equal(doc2Id, reopened.Tab.Document.Id);

        // doc2 should be removed from recently closed
        var recentlyClosed = await _tabManager.GetRecentlyClosedAsync();
        Assert.Equal(2, recentlyClosed.Count);
        Assert.DoesNotContain(recentlyClosed, r => r.DocumentId == doc2Id);
    }

    [Fact]
    public async Task RecentlyClosed_PersistsAcrossRestart()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab = await _tabManager.CreateTabAsync();
        await _documentService.UpdateTitleAsync(tab.Tab.Document.Id, "Persisted Title");
        _tabManager.UpdateTabContent(tab.Tab.TabId, "Persisted content");
        await _documentService.UpdateContentAsync(tab.Tab.Document.Id, "Persisted content");
        var docId = tab.Tab.Document.Id;

        await _tabManager.CloseTabAsync(tab.Tab.TabId);

        // Simulate restart
        var newMetadataStore = new JsonMetadataStore(_testDirectory);
        var newRepository = new FileSystemDocumentRepository(_testDirectory, newMetadataStore);
        var newTabManager = new TabManager(newRepository, newMetadataStore);
        await newTabManager.InitializeAsync();

        // Act
        var recentlyClosed = await newTabManager.GetRecentlyClosedAsync();

        // Assert
        Assert.Single(recentlyClosed);
        Assert.Equal(docId, recentlyClosed[0].DocumentId);
        Assert.Equal("Persisted Title", recentlyClosed[0].Title);
    }

    #endregion

    #region Duplicate Tab Tests

    [Fact]
    public async Task DuplicateTab_CreatesNewDocumentWithCopiedContent()
    {
        // Arrange - Create document with title via DocumentService first
        await _tabManager.InitializeAsync();
        var docWithTitle = await _documentService.CreateAsync("Original content to copy", "Original Title");

        // Open the document in a tab (this loads the document with its title from repository)
        var originalTab = await _tabManager.OpenDocumentInTabAsync(docWithTitle.Document.Id);
        Assert.NotNull(originalTab);

        // Act
        var duplicateTab = await _tabManager.DuplicateTabAsync(originalTab.Tab.TabId);

        // Assert
        Assert.NotNull(duplicateTab);
        Assert.NotEqual(originalTab.Tab.Document.Id, duplicateTab.Tab.Document.Id);
        Assert.Equal("Original content to copy", duplicateTab.Tab.Content);
        Assert.Equal("Original Title - Copy", duplicateTab.Tab.Document.Title);
        Assert.Equal(2, _tabManager.TabCount);

        // Verify files exist
        var docs = await _documentService.GetAllAsync();
        Assert.Equal(2, docs.Count());
    }

    [Fact]
    public async Task DuplicateTab_InsertedAfterOriginal()
    {
        // Arrange
        await _tabManager.InitializeAsync();
        var tab1 = await _tabManager.CreateTabAsync();
        var tab2 = await _tabManager.CreateTabAsync();
        var tab3 = await _tabManager.CreateTabAsync();

        // Act - duplicate middle tab
        var duplicate = await _tabManager.DuplicateTabAsync(tab2.Tab.TabId);

        // Assert - order should be: tab1, tab2, duplicate, tab3
        var tabs = _tabManager.GetOpenTabs();
        Assert.Equal(4, tabs.Count);
        Assert.Equal(tab1.Tab.TabId, tabs[0].Tab.TabId);
        Assert.Equal(tab2.Tab.TabId, tabs[1].Tab.TabId);
        Assert.Equal(duplicate!.Tab.TabId, tabs[2].Tab.TabId);
        Assert.Equal(tab3.Tab.TabId, tabs[3].Tab.TabId);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task EmptyTabManager_ReopenLastClosed_ReturnsNull()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act
        var result = await _tabManager.ReopenLastClosedAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LargeDocument_StatsCalculatedCorrectly()
    {
        // Arrange - Create a document with ~1500 words
        var words = Enumerable.Range(1, 1500).Select(i => $"word{i}");
        var content = string.Join(" ", words);

        // Act
        var doc = await _documentService.CreateAsync(content, "Large Document");

        // Assert
        Assert.Equal(1500, doc.WordCount);
        Assert.Equal("1.5k words", doc.FormattedWordCount);
    }

    [Fact]
    public async Task ConcurrentTabOperations_NoDeadlock()
    {
        // Arrange
        await _tabManager.InitializeAsync();

        // Act - create multiple tabs concurrently
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var tab = await _tabManager.CreateTabAsync();
            _tabManager.UpdateTabContent(tab.Tab.TabId, $"Content {i}");
            return tab;
        });

        var tabs = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, _tabManager.TabCount);
        Assert.All(tabs, t => Assert.NotNull(t));
    }

    #endregion
}
