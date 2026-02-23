using System.Globalization;
using Scrapile.Application.Services;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Application.Tests;

public class DocumentServiceTests
{
    private readonly MockDocumentRepository _mockRepository;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _mockRepository = new MockDocumentRepository();
        _service = new DocumentService(_mockRepository);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentService(null!));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithContent_CreatesDocumentWithStats()
    {
        // Arrange
        var content = "Hello World Test";

        // Act
        var result = await _service.CreateAsync(content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.Document.Content);
        Assert.Equal(3, result.WordCount);
        Assert.Equal(16, result.CharacterCount);
        Assert.Equal("3 words", result.FormattedWordCount);
        Assert.Equal("16 chars", result.FormattedCharacterCount);
    }

    [Fact]
    public async Task CreateAsync_WithTitle_IncludesTitle()
    {
        // Arrange
        var content = "Test content";
        var title = "My Document";

        // Act
        var result = await _service.CreateAsync(content, title);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Document.Title);
        Assert.True(result.Document.HasTitle);
    }

    [Fact]
    public async Task CreateAsync_WithoutTitle_HasNoTitle()
    {
        // Arrange
        var content = "Test content";

        // Act
        var result = await _service.CreateAsync(content);

        // Assert
        Assert.Null(result.Document.Title);
        Assert.False(result.Document.HasTitle);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyContent_CalculatesZeroStats()
    {
        // Act
        var result = await _service.CreateAsync(string.Empty);

        // Assert
        Assert.Equal(0, result.WordCount);
        Assert.Equal(0, result.CharacterCount);
        Assert.Equal("0 words", result.FormattedWordCount);
    }

    [Fact]
    public async Task CreateAsync_GeneratesContentPreview()
    {
        // Arrange
        var content = "This is a test document with some content";

        // Act
        var result = await _service.CreateAsync(content);

        // Assert
        Assert.NotEmpty(result.ContentPreview);
        Assert.True(result.ContentPreview.Length <= 43); // 40 + "..."
    }

    [Fact]
    public async Task CreateAsync_DelegatesToRepository()
    {
        // Arrange
        var content = "Test content";
        var title = "Test title";

        // Act
        await _service.CreateAsync(content, title);

        // Assert
        Assert.Single(_mockRepository.CreateCalls);
        Assert.Equal(content, _mockRepository.CreateCalls[0].Content);
        Assert.Equal(title, _mockRepository.CreateCalls[0].Title);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingDocument_ReturnsDocumentWithStats()
    {
        // Arrange
        var doc = CreateDocument("Test content", "Test Title");
        _mockRepository.AddDocument(doc);

        // Act
        var result = await _service.GetByIdAsync(doc.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(doc.Id, result.Document.Id);
        Assert.Equal(doc.Title, result.Document.Title);
        Assert.Equal(2, result.WordCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentDocument_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_CalculatesCorrectStats()
    {
        // Arrange
        var doc = CreateDocument("One two three four five");
        _mockRepository.AddDocument(doc);

        // Act
        var result = await _service.GetByIdAsync(doc.Id);

        // Assert
        Assert.Equal(5, result!.WordCount);
        Assert.Equal(23, result.CharacterCount);
        Assert.Equal("5 words", result.FormattedWordCount);
        Assert.Equal("23 chars", result.FormattedCharacterCount);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoDocuments_ReturnsEmptyList()
    {
        // Act
        var results = await _service.GetAllAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllAsync_WithDocuments_ReturnsAllWithStats()
    {
        // Arrange
        var doc1 = CreateDocument("First document");
        var doc2 = CreateDocument("Second document content");
        var doc3 = CreateDocument("Third");
        _mockRepository.AddDocument(doc1);
        _mockRepository.AddDocument(doc2);
        _mockRepository.AddDocument(doc3);

        // Act
        var results = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r =>
        {
            Assert.NotNull(r.Document);
            Assert.True(r.WordCount >= 0);
            Assert.True(r.CharacterCount >= 0);
        });
    }

    [Fact]
    public async Task GetAllAsync_CalculatesStatsForEachDocument()
    {
        // Arrange
        var doc1 = CreateDocument("One word");
        var doc2 = CreateDocument("Two words here");
        _mockRepository.AddDocument(doc1);
        _mockRepository.AddDocument(doc2);

        // Act
        var results = (await _service.GetAllAsync()).ToList();

        // Assert
        var firstDoc = results.First(r => r.Document.Id == doc1.Id);
        var secondDoc = results.First(r => r.Document.Id == doc2.Id);
        Assert.Equal(2, firstDoc.WordCount);
        Assert.Equal(3, secondDoc.WordCount);
    }

    #endregion

    #region UpdateContentAsync Tests

    [Fact]
    public async Task UpdateContentAsync_DelegatesToRepository()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var newContent = "Updated content";

        // Act
        await _service.UpdateContentAsync(docId, newContent);

        // Assert
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal(docId, _mockRepository.UpdateContentCalls[0].Id);
        Assert.Equal(newContent, _mockRepository.UpdateContentCalls[0].Content);
    }

    [Fact]
    public async Task UpdateContentAsync_WithEmptyContent_StillDelegates()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        await _service.UpdateContentAsync(docId, string.Empty);

        // Assert
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal(string.Empty, _mockRepository.UpdateContentCalls[0].Content);
    }

    #endregion

    #region UpdateTitleAsync Tests

    [Fact]
    public async Task UpdateTitleAsync_WithValidTitle_DelegatesToRepository()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var newTitle = "New Title";

        // Act
        await _service.UpdateTitleAsync(docId, newTitle);

        // Assert
        Assert.Single(_mockRepository.UpdateTitleCalls);
        Assert.Equal(docId, _mockRepository.UpdateTitleCalls[0].Id);
        Assert.Equal(newTitle, _mockRepository.UpdateTitleCalls[0].Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_WithNullTitle_PassesNull()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        await _service.UpdateTitleAsync(docId, null);

        // Assert
        Assert.Single(_mockRepository.UpdateTitleCalls);
        Assert.Null(_mockRepository.UpdateTitleCalls[0].Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_WithEmptyString_NormalizesToNull()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        await _service.UpdateTitleAsync(docId, "");

        // Assert
        Assert.Single(_mockRepository.UpdateTitleCalls);
        Assert.Null(_mockRepository.UpdateTitleCalls[0].Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_WithWhitespaceOnly_NormalizesToNull()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        await _service.UpdateTitleAsync(docId, "   \t\n  ");

        // Assert
        Assert.Single(_mockRepository.UpdateTitleCalls);
        Assert.Null(_mockRepository.UpdateTitleCalls[0].Title);
    }

    [Fact]
    public async Task UpdateTitleAsync_WithTitleContainingSpaces_PreservesTitle()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var title = "  My Title  ";

        // Act
        await _service.UpdateTitleAsync(docId, title);

        // Assert
        Assert.Single(_mockRepository.UpdateTitleCalls);
        Assert.Equal(title, _mockRepository.UpdateTitleCalls[0].Title);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        // Arrange
        var docId = Guid.NewGuid();

        // Act
        await _service.DeleteAsync(docId);

        // Assert
        Assert.Single(_mockRepository.DeleteCalls);
        Assert.Equal(docId, _mockRepository.DeleteCalls[0]);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNoResults_ReturnsEmptyList()
    {
        // Act
        var results = await _service.SearchAsync("nonexistent");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithMatches_ReturnsDocumentsWithStats()
    {
        // Arrange
        var doc1 = CreateDocument("Test document one", "Test Title");
        var doc2 = CreateDocument("Another test document");
        _mockRepository.AddDocument(doc1);
        _mockRepository.AddDocument(doc2);

        // Act
        var results = (await _service.SearchAsync("test")).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.NotNull(r.Document);
            Assert.True(r.WordCount > 0);
        });
    }

    [Fact]
    public async Task SearchAsync_DelegatesToRepository()
    {
        // Arrange
        var query = "search query";

        // Act
        await _service.SearchAsync(query);

        // Assert
        Assert.Single(_mockRepository.SearchCalls);
        Assert.Equal(query, _mockRepository.SearchCalls[0]);
    }

    [Fact]
    public async Task SearchAsync_CalculatesStatsForResults()
    {
        // Arrange
        var doc = CreateDocument("This is a test with five words");
        _mockRepository.AddDocument(doc);

        // Act
        var results = (await _service.SearchAsync("test")).ToList();

        // Assert
        var result = results.First();
        Assert.Equal(7, result.WordCount);
        Assert.Equal(30, result.CharacterCount);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWorkflow_CreateGetUpdateDelete()
    {
        // Create
        var created = await _service.CreateAsync("Initial content", "My Doc");
        Assert.NotNull(created);
        Assert.Equal("My Doc", created.Document.Title);

        // Get
        _mockRepository.AddDocument(created.Document);
        var retrieved = await _service.GetByIdAsync(created.Document.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Document.Id, retrieved.Document.Id);

        // Update Content
        await _service.UpdateContentAsync(created.Document.Id, "Updated content");
        Assert.Single(_mockRepository.UpdateContentCalls);

        // Update Title
        await _service.UpdateTitleAsync(created.Document.Id, "New Title");
        Assert.Single(_mockRepository.UpdateTitleCalls);

        // Delete
        await _service.DeleteAsync(created.Document.Id);
        Assert.Single(_mockRepository.DeleteCalls);
    }

    [Fact]
    public async Task LargeDocument_StatsCalculatedCorrectly()
    {
        // Arrange
        var words = Enumerable.Range(0, 1500).Select(i => "word").ToArray();
        var content = string.Join(" ", words);
        var doc = CreateDocument(content);
        _mockRepository.AddDocument(doc);

        // Act
        var result = await _service.GetByIdAsync(doc.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1500, result.WordCount);
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        Assert.Equal($"1{sep}5k words", result.FormattedWordCount);
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

    #region Mock Repository

    private class MockDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents = new();

        public List<(string Content, string? Title)> CreateCalls { get; } = new();
        public List<(Guid Id, string Content)> UpdateContentCalls { get; } = new();
        public List<(Guid Id, string? Title)> UpdateTitleCalls { get; } = new();
        public List<Guid> DeleteCalls { get; } = new();
        public List<string> SearchCalls { get; } = new();

        public void AddDocument(Document document)
        {
            _documents[document.Id] = document;
        }

        public Task<Document> CreateAsync(string content, string? title = null)
        {
            CreateCalls.Add((content, title));
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                Filename = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.txt",
                Title = title,
                Content = content,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
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
            UpdateContentCalls.Add((id, content));
            if (_documents.TryGetValue(id, out var doc))
            {
                doc.Content = content;
                doc.LastModified = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task UpdateTitleAsync(Guid id, string? title)
        {
            UpdateTitleCalls.Add((id, title));
            if (_documents.TryGetValue(id, out var doc))
            {
                doc.Title = title;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            DeleteCalls.Add(id);
            _documents.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Document>> SearchAsync(string query)
        {
            SearchCalls.Add(query);
            return Task.FromResult(_documents.Values.Where(d =>
                (d.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }
    }

    #endregion
}
