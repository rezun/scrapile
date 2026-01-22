namespace Scrapile.Infrastructure.Tests;

using System.Text.Json;
using Scrapile.Domain.Entities;
using Scrapile.Infrastructure.Storage;

public class JsonMetadataStoreTests : IDisposable
{
    private readonly TestDirectory _testDir;
    private readonly JsonMetadataStore _store;

    public JsonMetadataStoreTests()
    {
        _testDir = new TestDirectory();
        _store = new JsonMetadataStore(_testDir.Path);
    }

    public void Dispose()
    {
        _testDir.Dispose();
    }

    #region LoadAsync / SaveAsync Tests

    [Fact]
    public async Task LoadAsync_NoExistingFile_ReturnsDefaultMetadata()
    {
        var metadata = await _store.LoadAsync();

        Assert.NotNull(metadata);
        Assert.Equal("1.0", metadata.Version);
        Assert.Empty(metadata.OpenTabs);
        Assert.Empty(metadata.RecentlyClosed);
        Assert.Empty(metadata.Documents);
    }

    [Fact]
    public async Task SaveAsync_CreatesMetadataFile()
    {
        var metadata = new Metadata
        {
            Version = "1.0",
            OpenTabs = new List<OpenTabInfo>(),
            RecentlyClosed = new List<RecentlyClosedInfo>(),
            Documents = new Dictionary<Guid, DocumentMetadata>()
        };

        await _store.SaveAsync(metadata);

        var filePath = Path.Combine(_testDir.Path, ".ephemeral_metadata.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task LoadAsync_AfterSave_ReturnsSavedData()
    {
        var docId = Guid.NewGuid();
        var metadata = new Metadata
        {
            Version = "1.0",
            OpenTabs = new List<OpenTabInfo>
            {
                new() { DocumentId = docId, Order = 0 }
            },
            RecentlyClosed = new List<RecentlyClosedInfo>(),
            Documents = new Dictionary<Guid, DocumentMetadata>
            {
                [docId] = new DocumentMetadata { Title = "Test Document" }
            }
        };

        await _store.SaveAsync(metadata);

        // Create new store instance to test persistence
        var newStore = new JsonMetadataStore(_testDir.Path);
        var loaded = await newStore.LoadAsync();

        Assert.Single(loaded.OpenTabs);
        Assert.Equal(docId, loaded.OpenTabs[0].DocumentId);
        Assert.True(loaded.Documents.ContainsKey(docId));
        Assert.Equal("Test Document", loaded.Documents[docId].Title);
    }

    [Fact]
    public async Task LoadAsync_CorruptedFile_ReturnsDefaultMetadata()
    {
        var filePath = Path.Combine(_testDir.Path, ".ephemeral_metadata.json");
        await File.WriteAllTextAsync(filePath, "{ invalid json }}}");

        var metadata = await _store.LoadAsync();

        Assert.NotNull(metadata);
        Assert.Equal("1.0", metadata.Version);
    }

    [Fact]
    public async Task LoadAsync_CorruptedFile_WithValidBackup_RestoresFromBackup()
    {
        // First save valid metadata
        var docId = Guid.NewGuid();
        var validMetadata = new Metadata
        {
            Version = "1.0",
            OpenTabs = new List<OpenTabInfo>(),
            RecentlyClosed = new List<RecentlyClosedInfo>(),
            Documents = new Dictionary<Guid, DocumentMetadata>
            {
                [docId] = new DocumentMetadata { Title = "Backup Document" }
            }
        };
        await _store.SaveAsync(validMetadata);

        // Save again to create a backup of the valid metadata
        // (backup is only created when file already exists)
        await _store.SaveAsync(validMetadata);

        // Manually corrupt the main file but leave backup intact
        var filePath = Path.Combine(_testDir.Path, ".ephemeral_metadata.json");
        await File.WriteAllTextAsync(filePath, "{ corrupted }");

        // Create new store and load - should recover from backup
        var newStore = new JsonMetadataStore(_testDir.Path);
        var loaded = await newStore.LoadAsync();

        Assert.True(loaded.Documents.ContainsKey(docId));
        Assert.Equal("Backup Document", loaded.Documents[docId].Title);
    }

    [Fact]
    public async Task LoadAsync_ReturnsCachedData()
    {
        var metadata = await _store.LoadAsync();
        var metadata2 = await _store.LoadAsync();

        // Both should return the same cached instance
        Assert.Same(metadata, metadata2);
    }

    #endregion

    #region Document Management Tests

    [Fact]
    public async Task AddDocumentAsync_AddsToDocuments()
    {
        var docId = Guid.NewGuid();

        await _store.AddDocumentAsync(docId, "Test Title");

        var metadata = await _store.LoadAsync();
        Assert.True(metadata.Documents.ContainsKey(docId));
        Assert.Equal("Test Title", metadata.Documents[docId].Title);
    }

    [Fact]
    public async Task AddDocumentAsync_WithNullTitle_SetsNullTitle()
    {
        var docId = Guid.NewGuid();

        await _store.AddDocumentAsync(docId, null);

        var metadata = await _store.LoadAsync();
        Assert.True(metadata.Documents.ContainsKey(docId));
        Assert.Null(metadata.Documents[docId].Title);
    }

    [Fact]
    public async Task UpdateDocumentTitleAsync_UpdatesExistingDocument()
    {
        var docId = Guid.NewGuid();
        await _store.AddDocumentAsync(docId, "Original");

        await _store.UpdateDocumentTitleAsync(docId, "Updated");

        var title = await _store.GetDocumentTitleAsync(docId);
        Assert.Equal("Updated", title);
    }

    [Fact]
    public async Task UpdateDocumentTitleAsync_CreatesIfNotExists()
    {
        var docId = Guid.NewGuid();

        await _store.UpdateDocumentTitleAsync(docId, "New Title");

        var title = await _store.GetDocumentTitleAsync(docId);
        Assert.Equal("New Title", title);
    }

    [Fact]
    public async Task RemoveDocumentAsync_RemovesFromAllLists()
    {
        var docId = Guid.NewGuid();
        await _store.AddDocumentAsync(docId, "Title");
        await _store.AddOpenTabAsync(docId, 0);
        await _store.AddRecentlyClosedAsync(docId, DateTime.UtcNow);

        await _store.RemoveDocumentAsync(docId);

        var metadata = await _store.LoadAsync();
        Assert.False(metadata.Documents.ContainsKey(docId));
        Assert.DoesNotContain(metadata.OpenTabs, t => t.DocumentId == docId);
        Assert.DoesNotContain(metadata.RecentlyClosed, r => r.DocumentId == docId);
    }

    [Fact]
    public async Task GetDocumentTitleAsync_ReturnsTitle()
    {
        var docId = Guid.NewGuid();
        await _store.AddDocumentAsync(docId, "My Title");

        var title = await _store.GetDocumentTitleAsync(docId);

        Assert.Equal("My Title", title);
    }

    [Fact]
    public async Task GetDocumentTitleAsync_NonExistingDocument_ReturnsNull()
    {
        var title = await _store.GetDocumentTitleAsync(Guid.NewGuid());

        Assert.Null(title);
    }

    #endregion

    #region Open Tabs Tests

    [Fact]
    public async Task AddOpenTabAsync_AddsTabWithOrder()
    {
        var docId = Guid.NewGuid();

        await _store.AddOpenTabAsync(docId, 0);

        var tabs = await _store.GetOpenTabsAsync();
        Assert.Single(tabs);
        Assert.Equal(docId, tabs[0]);
    }

    [Fact]
    public async Task AddOpenTabAsync_MultipleTabs_MaintainsOrder()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var doc3 = Guid.NewGuid();

        await _store.AddOpenTabAsync(doc1, 0);
        await _store.AddOpenTabAsync(doc2, 1);
        await _store.AddOpenTabAsync(doc3, 2);

        var tabs = await _store.GetOpenTabsAsync();
        Assert.Equal(3, tabs.Count);
        Assert.Equal(doc1, tabs[0]);
        Assert.Equal(doc2, tabs[1]);
        Assert.Equal(doc3, tabs[2]);
    }

    [Fact]
    public async Task AddOpenTabAsync_DuplicateDocument_UpdatesPosition()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        await _store.AddOpenTabAsync(doc1, 0);
        await _store.AddOpenTabAsync(doc2, 1);
        // Move doc1 to position 2
        await _store.AddOpenTabAsync(doc1, 2);

        var tabs = await _store.GetOpenTabsAsync();
        Assert.Equal(2, tabs.Count);
        Assert.Equal(doc2, tabs[0]);
        Assert.Equal(doc1, tabs[1]);
    }

    [Fact]
    public async Task RemoveOpenTabAsync_RemovesTab()
    {
        var docId = Guid.NewGuid();
        await _store.AddOpenTabAsync(docId, 0);

        await _store.RemoveOpenTabAsync(docId);

        var tabs = await _store.GetOpenTabsAsync();
        Assert.Empty(tabs);
    }

    [Fact]
    public async Task RemoveOpenTabAsync_ReassignsOrders()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var doc3 = Guid.NewGuid();
        await _store.AddOpenTabAsync(doc1, 0);
        await _store.AddOpenTabAsync(doc2, 1);
        await _store.AddOpenTabAsync(doc3, 2);

        await _store.RemoveOpenTabAsync(doc2);

        var metadata = await _store.LoadAsync();
        Assert.Equal(2, metadata.OpenTabs.Count);
        Assert.Equal(0, metadata.OpenTabs.First(t => t.DocumentId == doc1).Order);
        Assert.Equal(1, metadata.OpenTabs.First(t => t.DocumentId == doc3).Order);
    }

    [Fact]
    public async Task UpdateOpenTabsOrderAsync_ReordersAll()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var doc3 = Guid.NewGuid();
        await _store.AddOpenTabAsync(doc1, 0);
        await _store.AddOpenTabAsync(doc2, 1);
        await _store.AddOpenTabAsync(doc3, 2);

        // Reverse order
        await _store.UpdateOpenTabsOrderAsync(new[] { doc3, doc2, doc1 });

        var tabs = await _store.GetOpenTabsAsync();
        Assert.Equal(doc3, tabs[0]);
        Assert.Equal(doc2, tabs[1]);
        Assert.Equal(doc1, tabs[2]);
    }

    [Fact]
    public async Task GetOpenTabsAsync_ReturnsInOrder()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        // Add in reverse order
        await _store.AddOpenTabAsync(doc2, 1);
        await _store.AddOpenTabAsync(doc1, 0);

        var tabs = await _store.GetOpenTabsAsync();

        Assert.Equal(doc1, tabs[0]);
        Assert.Equal(doc2, tabs[1]);
    }

    #endregion

    #region Recently Closed Tests

    [Fact]
    public async Task AddRecentlyClosedAsync_AddsToList()
    {
        var docId = Guid.NewGuid();
        var closedAt = DateTime.UtcNow;

        await _store.AddRecentlyClosedAsync(docId, closedAt);

        var recentlyClosed = await _store.GetRecentlyClosedAsync();
        Assert.Single(recentlyClosed);
        Assert.Equal(docId, recentlyClosed[0].DocumentId);
        Assert.Equal(closedAt, recentlyClosed[0].ClosedAt);
    }

    [Fact]
    public async Task AddRecentlyClosedAsync_MostRecentFirst()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        await _store.AddRecentlyClosedAsync(doc1, DateTime.UtcNow);
        await _store.AddRecentlyClosedAsync(doc2, DateTime.UtcNow);

        var recentlyClosed = await _store.GetRecentlyClosedAsync();
        Assert.Equal(doc2, recentlyClosed[0].DocumentId);
        Assert.Equal(doc1, recentlyClosed[1].DocumentId);
    }

    [Fact]
    public async Task AddRecentlyClosedAsync_Limit50Items()
    {
        // Add 60 items
        for (int i = 0; i < 60; i++)
        {
            await _store.AddRecentlyClosedAsync(Guid.NewGuid(), DateTime.UtcNow);
        }

        var recentlyClosed = await _store.GetRecentlyClosedAsync();

        Assert.Equal(50, recentlyClosed.Count);
    }

    [Fact]
    public async Task AddRecentlyClosedAsync_DuplicateDocument_MovesToFront()
    {
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        await _store.AddRecentlyClosedAsync(doc1, DateTime.UtcNow);
        await _store.AddRecentlyClosedAsync(doc2, DateTime.UtcNow);
        await _store.AddRecentlyClosedAsync(doc1, DateTime.UtcNow); // Re-add doc1

        var recentlyClosed = await _store.GetRecentlyClosedAsync();
        Assert.Equal(2, recentlyClosed.Count);
        Assert.Equal(doc1, recentlyClosed[0].DocumentId);
        Assert.Equal(doc2, recentlyClosed[1].DocumentId);
    }

    [Fact]
    public async Task RemoveRecentlyClosedAsync_RemovesFromList()
    {
        var docId = Guid.NewGuid();
        await _store.AddRecentlyClosedAsync(docId, DateTime.UtcNow);

        await _store.RemoveRecentlyClosedAsync(docId);

        var recentlyClosed = await _store.GetRecentlyClosedAsync();
        Assert.Empty(recentlyClosed);
    }

    [Fact]
    public async Task RemoveRecentlyClosedAsync_NonExisting_DoesNotThrow()
    {
        await _store.RemoveRecentlyClosedAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GetRecentlyClosedAsync_ReturnsListCopy()
    {
        var docId = Guid.NewGuid();
        await _store.AddRecentlyClosedAsync(docId, DateTime.UtcNow);

        var list1 = await _store.GetRecentlyClosedAsync();
        var list2 = await _store.GetRecentlyClosedAsync();

        // Should return copies, not the same instance
        Assert.NotSame(list1, list2);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task MetadataStore_PersistsAcrossInstances()
    {
        var docId = Guid.NewGuid();
        await _store.AddDocumentAsync(docId, "Persistent Title");
        await _store.AddOpenTabAsync(docId, 0);

        // Create new instance
        var newStore = new JsonMetadataStore(_testDir.Path);
        var metadata = await newStore.LoadAsync();

        Assert.True(metadata.Documents.ContainsKey(docId));
        Assert.Single(metadata.OpenTabs);
    }

    [Fact]
    public async Task MetadataStore_JsonStructureIsReadable()
    {
        var docId = Guid.NewGuid();
        await _store.AddDocumentAsync(docId, "Test");

        var filePath = Path.Combine(_testDir.Path, ".ephemeral_metadata.json");
        var json = await File.ReadAllTextAsync(filePath);

        // Verify JSON is properly formatted
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);

        // Verify structure uses camelCase
        Assert.Contains("\"version\"", json);
        Assert.Contains("\"openTabs\"", json);
        Assert.Contains("\"recentlyClosed\"", json);
        Assert.Contains("\"documents\"", json);
    }

    [Fact]
    public async Task MetadataStore_CreatesBackupOnSave()
    {
        await _store.AddDocumentAsync(Guid.NewGuid(), "First");
        await _store.AddDocumentAsync(Guid.NewGuid(), "Second");

        var backupPath = Path.Combine(_testDir.Path, ".ephemeral_metadata.json.backup");
        Assert.True(File.Exists(backupPath));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task MetadataStore_ConcurrentOperations_DoNotCorrupt()
    {
        var tasks = new List<Task>();

        // Concurrent writes
        for (int i = 0; i < 20; i++)
        {
            var docId = Guid.NewGuid();
            tasks.Add(_store.AddDocumentAsync(docId, $"Document {i}"));
        }

        await Task.WhenAll(tasks);

        var metadata = await _store.LoadAsync();
        Assert.Equal(20, metadata.Documents.Count);
    }

    [Fact]
    public async Task MetadataStore_ConcurrentReadsAndWrites_DoNotDeadlock()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var tasks = new List<Task>();

        // Mix of reads and writes
        for (int i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
            {
                tasks.Add(_store.AddDocumentAsync(Guid.NewGuid(), $"Doc {i}"));
            }
            else
            {
                tasks.Add(_store.GetOpenTabsAsync());
            }
        }

        await Task.WhenAll(tasks);
        // If we get here without timeout, no deadlock occurred
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task MetadataStore_HandlesUnicodeInTitles()
    {
        var docId = Guid.NewGuid();
        var unicodeTitle = "文档 📄 Document éàü";

        await _store.AddDocumentAsync(docId, unicodeTitle);

        // Verify persistence
        var newStore = new JsonMetadataStore(_testDir.Path);
        var title = await newStore.GetDocumentTitleAsync(docId);
        Assert.Equal(unicodeTitle, title);
    }

    [Fact]
    public void Constructor_NullDirectory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonMetadataStore(null!));
    }

    [Fact]
    public void Constructor_EmptyDirectory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonMetadataStore(""));
    }

    [Fact]
    public void Constructor_WhitespaceDirectory_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JsonMetadataStore("   "));
    }

    #endregion
}
