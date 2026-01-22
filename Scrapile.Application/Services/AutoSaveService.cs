namespace Scrapile.Application.Services;

using Scrapile.Domain.Interfaces;

/// <summary>
/// Service for debounced auto-saving of document content.
/// Each keystroke resets the debounce timer, resulting in a single save
/// after the user stops typing.
/// </summary>
public class AutoSaveService : IDisposable
{
    private readonly IDocumentRepository _repository;
    private readonly TimeSpan _debounceDelay;
    private readonly Dictionary<Guid, CancellationTokenSource> _pendingSaves = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a debounced save operation completes successfully.
    /// </summary>
    public event EventHandler<SaveCompletedEventArgs>? SaveCompleted;

    /// <summary>
    /// Creates a new AutoSaveService with the default debounce delay (500ms).
    /// </summary>
    /// <param name="repository">The document repository for storage operations.</param>
    public AutoSaveService(IDocumentRepository repository)
        : this(repository, TimeSpan.FromMilliseconds(500))
    {
    }

    /// <summary>
    /// Creates a new AutoSaveService with a custom debounce delay.
    /// </summary>
    /// <param name="repository">The document repository for storage operations.</param>
    /// <param name="debounceDelay">The delay before saving after the last content change.</param>
    public AutoSaveService(IDocumentRepository repository, TimeSpan debounceDelay)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        if (debounceDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(debounceDelay), "Debounce delay cannot be negative.");
        }

        _debounceDelay = debounceDelay;
    }

    /// <summary>
    /// Gets the configured debounce delay.
    /// </summary>
    public TimeSpan DebounceDelay => _debounceDelay;

    /// <summary>
    /// Schedules a save operation after the debounce delay.
    /// If a save is already pending for this document, it is cancelled and replaced.
    /// </summary>
    /// <param name="documentId">The document ID to save.</param>
    /// <param name="content">The content to save.</param>
    /// <returns>A task that completes when the save operation is scheduled (not when it completes).</returns>
    public Task ScheduleSaveAsync(Guid documentId, string content)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationTokenSource cts;
        CancellationTokenSource? existingCts = null;

        lock (_lock)
        {
            // Cancel any existing pending save for this document
            if (_pendingSaves.TryGetValue(documentId, out existingCts))
            {
                _pendingSaves.Remove(documentId);
            }

            // Create new cancellation token source for this save
            cts = new CancellationTokenSource();
            _pendingSaves[documentId] = cts;
        }

        // Cancel the existing CTS outside the lock to avoid potential deadlocks
        existingCts?.Cancel();
        existingCts?.Dispose();

        // Start the debounced save task (fire and forget)
        _ = ExecuteDebouncedSaveAsync(documentId, content, cts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the document immediately, cancelling any pending debounced save.
    /// Use this when the user closes a tab to ensure content is saved.
    /// </summary>
    /// <param name="documentId">The document ID to save.</param>
    /// <param name="content">The content to save.</param>
    /// <returns>A task that completes when the save operation finishes.</returns>
    public async Task SaveImmediatelyAsync(Guid documentId, string content)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationTokenSource? existingCts = null;

        lock (_lock)
        {
            // Cancel and remove any pending debounced save
            if (_pendingSaves.TryGetValue(documentId, out existingCts))
            {
                _pendingSaves.Remove(documentId);
            }
        }

        // Cancel the existing CTS outside the lock
        existingCts?.Cancel();
        existingCts?.Dispose();

        // Save immediately
        await _repository.UpdateContentAsync(documentId, content);
    }

    /// <summary>
    /// Checks if there is a pending save for the specified document.
    /// </summary>
    /// <param name="documentId">The document ID to check.</param>
    /// <returns>True if a save is pending, false otherwise.</returns>
    public bool HasPendingSave(Guid documentId)
    {
        lock (_lock)
        {
            return _pendingSaves.ContainsKey(documentId);
        }
    }

    /// <summary>
    /// Cancels a pending save for the specified document without saving.
    /// </summary>
    /// <param name="documentId">The document ID to cancel.</param>
    public void CancelPendingSave(Guid documentId)
    {
        CancellationTokenSource? cts = null;

        lock (_lock)
        {
            if (_pendingSaves.TryGetValue(documentId, out cts))
            {
                _pendingSaves.Remove(documentId);
            }
        }

        cts?.Cancel();
        cts?.Dispose();
    }

    /// <summary>
    /// Gets the number of currently pending saves.
    /// </summary>
    public int PendingSaveCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingSaves.Count;
            }
        }
    }

    /// <summary>
    /// Executes the debounced save after waiting for the delay period.
    /// </summary>
    private async Task ExecuteDebouncedSaveAsync(Guid documentId, string content, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for the debounce period
            await Task.Delay(_debounceDelay, cancellationToken);

            // Check if still not cancelled
            cancellationToken.ThrowIfCancellationRequested();

            // Perform the save
            await _repository.UpdateContentAsync(documentId, content);

            // Clean up the pending save entry
            lock (_lock)
            {
                // Only remove if it's still our CTS (hasn't been replaced)
                if (_pendingSaves.TryGetValue(documentId, out var currentCts) &&
                    currentCts.Token == cancellationToken)
                {
                    _pendingSaves.Remove(documentId);
                }
            }

            // Raise event to notify that save completed
            SaveCompleted?.Invoke(this, new SaveCompletedEventArgs(documentId));
        }
        catch (OperationCanceledException)
        {
            // Expected when user continues typing or closes tab
            // No action needed - the save was intentionally cancelled
        }
    }

    /// <summary>
    /// Disposes of the service and cancels all pending saves.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        List<CancellationTokenSource> allCts;
        lock (_lock)
        {
            allCts = new List<CancellationTokenSource>(_pendingSaves.Values);
            _pendingSaves.Clear();
        }

        foreach (var cts in allCts)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}

/// <summary>
/// Event args for save completed events.
/// </summary>
public class SaveCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the document that was saved.
    /// </summary>
    public Guid DocumentId { get; }

    /// <summary>
    /// Creates a new SaveCompletedEventArgs.
    /// </summary>
    /// <param name="documentId">The ID of the document that was saved.</param>
    public SaveCompletedEventArgs(Guid documentId)
    {
        DocumentId = documentId;
    }
}
