using Scrapile.Application.Services;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

namespace Scrapile.Application.Tests;

public class AutoSaveServiceTests : IDisposable
{
    private readonly MockDocumentRepository _mockRepository;
    private AutoSaveService? _service;

    public AutoSaveServiceTests()
    {
        _mockRepository = new MockDocumentRepository();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AutoSaveService(null!));
    }

    [Fact]
    public void Constructor_WithNegativeDelay_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void Constructor_WithZeroDelay_DoesNotThrow()
    {
        _service = new AutoSaveService(_mockRepository, TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, _service.DebounceDelay);
    }

    [Fact]
    public void Constructor_DefaultDelay_Is500ms()
    {
        _service = new AutoSaveService(_mockRepository);
        Assert.Equal(TimeSpan.FromMilliseconds(500), _service.DebounceDelay);
    }

    [Fact]
    public void Constructor_CustomDelay_IsUsed()
    {
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(1000));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), _service.DebounceDelay);
    }

    #endregion

    #region ScheduleSaveAsync Tests

    [Fact]
    public async Task ScheduleSaveAsync_AfterDelay_SavesContent()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentId = Guid.NewGuid();
        var content = "Test content";

        // Act
        await _service.ScheduleSaveAsync(documentId, content);

        // Wait for the debounce delay plus some buffer
        await Task.Delay(100);

        // Assert
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal(documentId, _mockRepository.UpdateContentCalls[0].Id);
        Assert.Equal(content, _mockRepository.UpdateContentCalls[0].Content);
    }

    [Fact]
    public async Task ScheduleSaveAsync_BeforeDelay_DoesNotSave()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(200));
        var documentId = Guid.NewGuid();

        // Act
        await _service.ScheduleSaveAsync(documentId, "Test content");

        // Wait less than the debounce delay
        await Task.Delay(50);

        // Assert
        Assert.Empty(_mockRepository.UpdateContentCalls);
    }

    [Fact]
    public async Task ScheduleSaveAsync_RapidCalls_OnlyLastSaveExecuted()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentId = Guid.NewGuid();

        // Act - simulate rapid typing
        await _service.ScheduleSaveAsync(documentId, "Content 1");
        await Task.Delay(10);
        await _service.ScheduleSaveAsync(documentId, "Content 2");
        await Task.Delay(10);
        await _service.ScheduleSaveAsync(documentId, "Content 3");

        // Wait for the debounce delay
        await Task.Delay(100);

        // Assert - only the last content should be saved
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal("Content 3", _mockRepository.UpdateContentCalls[0].Content);
    }

    [Fact]
    public async Task ScheduleSaveAsync_DifferentDocuments_AllSaved()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var doc3 = Guid.NewGuid();

        // Act
        await _service.ScheduleSaveAsync(doc1, "Content 1");
        await _service.ScheduleSaveAsync(doc2, "Content 2");
        await _service.ScheduleSaveAsync(doc3, "Content 3");

        // Wait for all debounce delays
        await Task.Delay(100);

        // Assert - all documents should be saved
        Assert.Equal(3, _mockRepository.UpdateContentCalls.Count);
    }

    [Fact]
    public async Task ScheduleSaveAsync_SetsPendingSave()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(100));
        var documentId = Guid.NewGuid();

        // Act
        await _service.ScheduleSaveAsync(documentId, "Test content");

        // Assert
        Assert.True(_service.HasPendingSave(documentId));
    }

    [Fact]
    public async Task ScheduleSaveAsync_AfterSaveCompletes_NoPendingSave()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentId = Guid.NewGuid();

        // Act
        await _service.ScheduleSaveAsync(documentId, "Test content");
        await Task.Delay(100);

        // Assert
        Assert.False(_service.HasPendingSave(documentId));
    }

    [Fact]
    public async Task ScheduleSaveAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        _service.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _service.ScheduleSaveAsync(Guid.NewGuid(), "Test"));
    }

    #endregion

    #region SaveImmediatelyAsync Tests

    [Fact]
    public async Task SaveImmediatelyAsync_SavesImmediately()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(1000));
        var documentId = Guid.NewGuid();
        var content = "Test content";

        // Act
        await _service.SaveImmediatelyAsync(documentId, content);

        // Assert - should save immediately without waiting
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal(documentId, _mockRepository.UpdateContentCalls[0].Id);
        Assert.Equal(content, _mockRepository.UpdateContentCalls[0].Content);
    }

    [Fact]
    public async Task SaveImmediatelyAsync_CancelsPendingSave()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(200));
        var documentId = Guid.NewGuid();

        // Act - schedule a debounced save, then save immediately
        await _service.ScheduleSaveAsync(documentId, "Debounced content");
        Assert.True(_service.HasPendingSave(documentId));

        await _service.SaveImmediatelyAsync(documentId, "Immediate content");

        // Wait for what would have been the debounce delay
        await Task.Delay(250);

        // Assert - only the immediate save should have occurred
        Assert.Single(_mockRepository.UpdateContentCalls);
        Assert.Equal("Immediate content", _mockRepository.UpdateContentCalls[0].Content);
        Assert.False(_service.HasPendingSave(documentId));
    }

    [Fact]
    public async Task SaveImmediatelyAsync_NoPendingSave_StillSaves()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentId = Guid.NewGuid();

        // Act
        await _service.SaveImmediatelyAsync(documentId, "Test content");

        // Assert
        Assert.Single(_mockRepository.UpdateContentCalls);
    }

    [Fact]
    public async Task SaveImmediatelyAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        _service.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _service.SaveImmediatelyAsync(Guid.NewGuid(), "Test"));
    }

    #endregion

    #region HasPendingSave Tests

    [Fact]
    public void HasPendingSave_NoPendingSaves_ReturnsFalse()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository);

        // Assert
        Assert.False(_service.HasPendingSave(Guid.NewGuid()));
    }

    #endregion

    #region CancelPendingSave Tests

    [Fact]
    public async Task CancelPendingSave_CancelsPendingSaveWithoutSaving()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(100));
        var documentId = Guid.NewGuid();

        // Act
        await _service.ScheduleSaveAsync(documentId, "Test content");
        Assert.True(_service.HasPendingSave(documentId));

        _service.CancelPendingSave(documentId);

        // Wait for what would have been the debounce delay
        await Task.Delay(150);

        // Assert
        Assert.False(_service.HasPendingSave(documentId));
        Assert.Empty(_mockRepository.UpdateContentCalls);
    }

    [Fact]
    public void CancelPendingSave_NoPendingSave_DoesNotThrow()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository);

        // Act & Assert - should not throw
        _service.CancelPendingSave(Guid.NewGuid());
    }

    #endregion

    #region PendingSaveCount Tests

    [Fact]
    public void PendingSaveCount_NoPendingSaves_ReturnsZero()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository);

        // Assert
        Assert.Equal(0, _service.PendingSaveCount);
    }

    [Fact]
    public async Task PendingSaveCount_WithPendingSaves_ReturnsCorrectCount()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(200));

        // Act
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 1");
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 2");
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 3");

        // Assert
        Assert.Equal(3, _service.PendingSaveCount);
    }

    [Fact]
    public async Task PendingSaveCount_AfterSavesComplete_ReturnsZero()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));

        // Act
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 1");
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 2");
        await Task.Delay(100);

        // Assert
        Assert.Equal(0, _service.PendingSaveCount);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_CancelsAllPendingSaves()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(200));
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 1");
        await _service.ScheduleSaveAsync(Guid.NewGuid(), "Content 2");

        // Act
        _service.Dispose();

        // Wait for what would have been the debounce delay
        await Task.Delay(250);

        // Assert - no saves should have occurred
        Assert.Empty(_mockRepository.UpdateContentCalls);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository);

        // Act & Assert - should not throw
        _service.Dispose();
        _service.Dispose();
        _service.Dispose();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentScheduleSaves_HandledSafely()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        // Act - schedule many saves concurrently
        var tasks = documentIds.Select(id => _service.ScheduleSaveAsync(id, $"Content for {id}"));
        await Task.WhenAll(tasks);

        // Wait for all debounce delays
        await Task.Delay(150);

        // Assert - all documents should be saved
        Assert.Equal(10, _mockRepository.UpdateContentCalls.Count);
    }

    [Fact]
    public async Task ConcurrentRapidUpdates_OnlyLastSaved()
    {
        // Arrange
        _service = new AutoSaveService(_mockRepository, TimeSpan.FromMilliseconds(50));
        var documentId = Guid.NewGuid();

        // Act - simulate concurrent rapid updates to the same document
        var tasks = Enumerable.Range(0, 20)
            .Select(i => _service.ScheduleSaveAsync(documentId, $"Content {i}"));
        await Task.WhenAll(tasks);

        // Wait for the debounce delay
        await Task.Delay(100);

        // Assert - only one save should have occurred (the last one)
        Assert.Single(_mockRepository.UpdateContentCalls);
    }

    #endregion

    #region Mock Repository

    private class MockDocumentRepository : IDocumentRepository
    {
        public List<(Guid Id, string Content)> UpdateContentCalls { get; } = new();
        private readonly object _lock = new();

        public Task<Document> CreateAsync(string content, string? title = null)
        {
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                Filename = "test.txt",
                Title = title,
                Content = content,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            return Task.FromResult(doc);
        }

        public Task<Document?> GetByIdAsync(Guid id)
        {
            return Task.FromResult<Document?>(null);
        }

        public Task<IEnumerable<Document>> GetAllAsync()
        {
            return Task.FromResult(Enumerable.Empty<Document>());
        }

        public Task UpdateContentAsync(Guid id, string content)
        {
            lock (_lock)
            {
                UpdateContentCalls.Add((id, content));
            }
            return Task.CompletedTask;
        }

        public Task UpdateTitleAsync(Guid id, string? title)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Document>> SearchAsync(string query)
        {
            return Task.FromResult(Enumerable.Empty<Document>());
        }
    }

    #endregion
}
