namespace Scrapile.Infrastructure.Tests;

using Scrapile.Infrastructure.Repositories;
using Scrapile.Infrastructure.Storage;

public class FileSystemDocumentRepositoryTests : IDisposable
{
    private readonly TestDirectory _testDir;
    private readonly JsonMetadataStore _metadataStore;
    private readonly FileSystemDocumentRepository _repository;

    public FileSystemDocumentRepositoryTests()
    {
        _testDir = new TestDirectory();
        _metadataStore = new JsonMetadataStore(_testDir.Path);
        _repository = new FileSystemDocumentRepository(_testDir.Path, _metadataStore);
    }

    public void Dispose()
    {
        _testDir.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithContent_CreatesDocumentWithGeneratedId()
    {
        var content = "Hello, World!";

        var document = await _repository.CreateAsync(content);

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(content, document.Content);
        Assert.Null(document.Title);
        Assert.NotNull(document.Filename);
        Assert.EndsWith(".txt", document.Filename);
    }

    [Fact]
    public async Task CreateAsync_WithTitle_SetsTitle()
    {
        var content = "Document content";
        var title = "My Document";

        var document = await _repository.CreateAsync(content, title);

        Assert.Equal(title, document.Title);
        Assert.Equal(content, document.Content);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_TreatsAsNull()
    {
        var content = "Content";

        var document = await _repository.CreateAsync(content, "   ");

        Assert.Null(document.Title);
    }

    [Fact]
    public async Task CreateAsync_FilenameFollowsConvention()
    {
        var document = await _repository.CreateAsync("test");

        // Filename should be: {timestamp}_{guid}.txt
        // Example: 20250122143022_a3f5b2e1c4d34e5f8a9b1c2d3e4f5a6b.txt
        var filename = document.Filename;
        var parts = Path.GetFileNameWithoutExtension(filename).Split('_');

        Assert.Equal(2, parts.Length);
        Assert.Equal(14, parts[0].Length); // Timestamp: yyyyMMddHHmmss
        Assert.Equal(32, parts[1].Length); // GUID without hyphens
        Assert.True(Guid.TryParse(parts[1], out _));
    }

    [Fact]
    public async Task CreateAsync_PersistsFileToDisk()
    {
        var content = "Persisted content";

        var document = await _repository.CreateAsync(content);

        var filePath = Path.Combine(_testDir.Path, document.Filename);
        Assert.True(File.Exists(filePath));
        Assert.Equal(content, await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAndLastModifiedDates()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var document = await _repository.CreateAsync("content");

        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(document.Created >= before && document.Created <= after);
        Assert.True(document.LastModified >= before && document.LastModified <= after);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyContent_CreatesEmptyFile()
    {
        var document = await _repository.CreateAsync("");

        var filePath = Path.Combine(_testDir.Path, document.Filename);
        Assert.True(File.Exists(filePath));
        Assert.Equal("", await File.ReadAllTextAsync(filePath));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingDocument_ReturnsDocument()
    {
        var content = "Find me";
        var title = "Findable";
        var created = await _repository.CreateAsync(content, title);

        var retrieved = await _repository.GetByIdAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(content, retrieved.Content);
        Assert.Equal(title, retrieved.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingDocument_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AfterFileDeleted_ReturnsNull()
    {
        var document = await _repository.CreateAsync("content");
        var filePath = Path.Combine(_testDir.Path, document.Filename);
        File.Delete(filePath);

        var result = await _repository.GetByIdAsync(document.Id);

        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var documents = await _repository.GetAllAsync();

        Assert.Empty(documents);
    }

    [Fact]
    public async Task GetAllAsync_MultipleDocuments_ReturnsAll()
    {
        await _repository.CreateAsync("First");
        await _repository.CreateAsync("Second");
        await _repository.CreateAsync("Third");

        var documents = (await _repository.GetAllAsync()).ToList();

        Assert.Equal(3, documents.Count);
        Assert.Contains(documents, d => d.Content == "First");
        Assert.Contains(documents, d => d.Content == "Second");
        Assert.Contains(documents, d => d.Content == "Third");
    }

    [Fact]
    public async Task GetAllAsync_IgnoresNonMatchingFiles()
    {
        await _repository.CreateAsync("Valid document");

        // Create a non-matching txt file
        await File.WriteAllTextAsync(
            Path.Combine(_testDir.Path, "invalid_file.txt"),
            "Not a scrapile document");

        // Create a non-txt file
        await File.WriteAllTextAsync(
            Path.Combine(_testDir.Path, "document.md"),
            "Markdown file");

        var documents = (await _repository.GetAllAsync()).ToList();

        Assert.Single(documents);
        Assert.Equal("Valid document", documents[0].Content);
    }

    #endregion

    #region UpdateContentAsync Tests

    [Fact]
    public async Task UpdateContentAsync_ExistingDocument_UpdatesContent()
    {
        var document = await _repository.CreateAsync("Original");

        await _repository.UpdateContentAsync(document.Id, "Updated");

        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated", retrieved.Content);
    }

    [Fact]
    public async Task UpdateContentAsync_NonExistingDocument_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _repository.UpdateContentAsync(Guid.NewGuid(), "content"));
    }

    [Fact]
    public async Task UpdateContentAsync_UpdatesLastModifiedDate()
    {
        var document = await _repository.CreateAsync("Original");
        var originalModified = document.LastModified;

        // Small delay to ensure different timestamp
        await Task.Delay(50);

        await _repository.UpdateContentAsync(document.Id, "Updated");

        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.LastModified >= originalModified);
    }

    [Fact]
    public async Task UpdateContentAsync_PreservesFilename()
    {
        var document = await _repository.CreateAsync("Original");
        var originalFilename = document.Filename;

        await _repository.UpdateContentAsync(document.Id, "Updated");

        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(originalFilename, retrieved.Filename);
    }

    #endregion

    #region UpdateTitleAsync Tests

    [Fact]
    public async Task UpdateTitleAsync_SetsNewTitle()
    {
        var document = await _repository.CreateAsync("Content");

        await _repository.UpdateTitleAsync(document.Id, "New Title");

        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("New Title", retrieved.Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_WithNull_ClearsTitle()
    {
        var document = await _repository.CreateAsync("Content", "Original Title");

        await _repository.UpdateTitleAsync(document.Id, null);

        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_DoesNotModifyFileContent()
    {
        var originalContent = "Original content";
        var document = await _repository.CreateAsync(originalContent);

        await _repository.UpdateTitleAsync(document.Id, "New Title");

        var filePath = Path.Combine(_testDir.Path, document.Filename);
        var fileContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(originalContent, fileContent);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingDocument_RemovesFile()
    {
        var document = await _repository.CreateAsync("To be deleted");
        var filePath = Path.Combine(_testDir.Path, document.Filename);
        Assert.True(File.Exists(filePath));

        await _repository.DeleteAsync(document.Id);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_NonExistingDocument_DoesNotThrow()
    {
        // Should not throw for non-existing document
        await _repository.DeleteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedFile_DoesNotThrow()
    {
        var document = await _repository.CreateAsync("content");
        var filePath = Path.Combine(_testDir.Path, document.Filename);
        File.Delete(filePath);

        // Should not throw even if file is already deleted
        await _repository.DeleteAsync(document.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFromGetAll()
    {
        var doc1 = await _repository.CreateAsync("First");
        var doc2 = await _repository.CreateAsync("Second");

        await _repository.DeleteAsync(doc1.Id);

        var documents = (await _repository.GetAllAsync()).ToList();
        Assert.Single(documents);
        Assert.Equal(doc2.Id, documents[0].Id);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        await _repository.CreateAsync("content");

        var results = await _repository.SearchAsync("");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmpty()
    {
        await _repository.CreateAsync("content");

        var results = await _repository.SearchAsync("   ");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MatchesTitleSubstring()
    {
        await _repository.CreateAsync("content", "Meeting Notes");

        var results = (await _repository.SearchAsync("Meet")).ToList();

        Assert.Single(results);
        Assert.Equal("Meeting Notes", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_MatchesContentSubstring()
    {
        await _repository.CreateAsync("This is a budget report for Q1");

        var results = (await _repository.SearchAsync("budget")).ToList();

        Assert.Single(results);
        Assert.Contains("budget", results[0].Content);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive()
    {
        await _repository.CreateAsync("content", "UPPERCASE TITLE");
        await _repository.CreateAsync("LOWERCASE CONTENT");

        var titleResults = (await _repository.SearchAsync("uppercase")).ToList();
        var contentResults = (await _repository.SearchAsync("lowercase")).ToList();

        Assert.Single(titleResults);
        Assert.Single(contentResults);
    }

    [Fact]
    public async Task SearchAsync_TitleMatchesFirst()
    {
        // Create content match first
        var contentDoc = await _repository.CreateAsync("This document has the word project in it");
        await Task.Delay(50);
        // Create title match second
        var titleDoc = await _repository.CreateAsync("Other content", "Project Plan");

        var results = (await _repository.SearchAsync("project")).ToList();

        Assert.Equal(2, results.Count);
        // Title match should be first
        Assert.Equal(titleDoc.Id, results[0].Id);
        Assert.Equal(contentDoc.Id, results[1].Id);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmpty()
    {
        await _repository.CreateAsync("Hello world", "Greeting");

        var results = await _repository.SearchAsync("xyznonexistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MultipleMatches_SortedByLastModified()
    {
        var doc1 = await _repository.CreateAsync("First content match");
        await Task.Delay(50);
        var doc2 = await _repository.CreateAsync("Second content match");
        await Task.Delay(50);
        var doc3 = await _repository.CreateAsync("Third content match");

        var results = (await _repository.SearchAsync("content")).ToList();

        Assert.Equal(3, results.Count);
        // Most recently modified first
        Assert.Equal(doc3.Id, results[0].Id);
        Assert.Equal(doc2.Id, results[1].Id);
        Assert.Equal(doc1.Id, results[2].Id);
    }

    [Fact]
    public async Task SearchAsync_WithManyDocuments_PerformsAcceptably()
    {
        // Create 100 documents
        for (int i = 0; i < 100; i++)
        {
            await _repository.CreateAsync($"Document content {i}", $"Document {i}");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = (await _repository.SearchAsync("Document")).ToList();
        stopwatch.Stop();

        Assert.Equal(100, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Search took {stopwatch.ElapsedMilliseconds}ms, expected <500ms");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Repository_HandlesSpecialCharactersInContent()
    {
        var content = "Special chars: éàü, emoji: 🎉, newlines:\nand\ttabs";

        var document = await _repository.CreateAsync(content);
        var retrieved = await _repository.GetByIdAsync(document.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(content, retrieved.Content);
    }

    [Fact]
    public async Task Repository_HandlesLargeContent()
    {
        var content = new string('x', 100_000); // 100KB

        var document = await _repository.CreateAsync(content);
        var retrieved = await _repository.GetByIdAsync(document.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(content, retrieved.Content);
    }

    [Fact]
    public async Task Repository_WithoutMetadataStore_StillWorks()
    {
        var repoWithoutMetadata = new FileSystemDocumentRepository(_testDir.Path, metadataStore: null);

        var document = await repoWithoutMetadata.CreateAsync("content", "title");
        var retrieved = await repoWithoutMetadata.GetByIdAsync(document.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("content", retrieved.Content);
        // Title won't be persisted without metadata store
        Assert.Null(retrieved.Title);
    }

    [Fact]
    public async Task Repository_MultipleDocumentsSameTimestamp_HasUniqueFilenames()
    {
        // Create multiple documents rapidly
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _repository.CreateAsync("content"))
            .ToList();

        var documents = await Task.WhenAll(tasks);

        var filenames = documents.Select(d => d.Filename).ToList();
        Assert.Equal(10, filenames.Distinct().Count());
    }

    #endregion
}
