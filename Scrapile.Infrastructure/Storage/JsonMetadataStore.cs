namespace Scrapile.Infrastructure.Storage;

using System.Text.Json;
using System.Text.Json.Serialization;
using Scrapile.Domain.Constants;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// JSON file-based implementation of IMetadataStore.
/// Metadata is stored in .ephemeral_metadata.json in the storage directory.
/// </summary>
public class JsonMetadataStore : IMetadataStore, IDisposable
{
    private bool _disposed;
    private const string MetadataFilename = ".ephemeral_metadata.json";
    private const string BackupExtension = ".backup";

    private readonly string _storageDirectory;
    private readonly string _metadataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // In-memory cache of metadata to reduce disk I/O
    private Metadata? _cachedMetadata;

    /// <summary>
    /// Creates a new JsonMetadataStore.
    /// </summary>
    /// <param name="storageDirectory">Directory where the metadata file will be stored.</param>
    public JsonMetadataStore(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory cannot be null or empty.", nameof(storageDirectory));
        }

        _storageDirectory = storageDirectory;
        _metadataFilePath = Path.Combine(_storageDirectory, MetadataFilename);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirectory);
    }

    /// <inheritdoc />
    public async Task<Metadata> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Return cached if available
            if (_cachedMetadata != null)
            {
                return _cachedMetadata;
            }

            _cachedMetadata = await LoadFromFileAsync();
            return _cachedMetadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(Metadata metadata)
    {
        await _lock.WaitAsync();
        try
        {
            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddDocumentAsync(Guid documentId, string? title)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            metadata.Documents[documentId] = new DocumentMetadata
            {
                Title = string.IsNullOrWhiteSpace(title) ? null : title
            };

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateDocumentTitleAsync(Guid documentId, string? title)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (!metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                docMeta = new DocumentMetadata();
                metadata.Documents[documentId] = docMeta;
            }

            docMeta.Title = string.IsNullOrWhiteSpace(title) ? null : title;

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveDocumentAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            metadata.Documents.Remove(documentId);

            // Also remove from open tabs if present
            metadata.OpenTabs.RemoveAll(t => t.DocumentId == documentId);

            // Also remove from recently closed if present
            metadata.RecentlyClosed.RemoveAll(r => r.DocumentId == documentId);

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddOpenTabAsync(Guid documentId, int order)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            // Remove existing entry if present (to avoid duplicates)
            metadata.OpenTabs.RemoveAll(t => t.DocumentId == documentId);

            metadata.OpenTabs.Add(new OpenTabInfo
            {
                DocumentId = documentId,
                Order = order
            });

            // Re-sort by order
            metadata.OpenTabs = metadata.OpenTabs.OrderBy(t => t.Order).ToList();

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveOpenTabAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            metadata.OpenTabs.RemoveAll(t => t.DocumentId == documentId);

            // Re-assign orders to be contiguous
            for (int i = 0; i < metadata.OpenTabs.Count; i++)
            {
                metadata.OpenTabs[i].Order = i;
            }

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateOpenTabsOrderAsync(IEnumerable<Guid> orderedDocumentIds)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            var newOpenTabs = new List<OpenTabInfo>();
            var order = 0;

            foreach (var docId in orderedDocumentIds)
            {
                newOpenTabs.Add(new OpenTabInfo
                {
                    DocumentId = docId,
                    Order = order++
                });
            }

            metadata.OpenTabs = newOpenTabs;

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddRecentlyClosedAsync(Guid documentId, DateTime closedAt)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            // Remove existing entry if present (to avoid duplicates)
            metadata.RecentlyClosed.RemoveAll(r => r.DocumentId == documentId);

            // Add at the beginning (most recent first)
            metadata.RecentlyClosed.Insert(0, new RecentlyClosedInfo
            {
                DocumentId = documentId,
                ClosedAt = closedAt
            });

            // Enforce max limit (LRU eviction - remove oldest entries)
            while (metadata.RecentlyClosed.Count > MetadataLimits.MaxRecentlyClosedItems)
            {
                metadata.RecentlyClosed.RemoveAt(metadata.RecentlyClosed.Count - 1);
            }

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveRecentlyClosedAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            metadata.RecentlyClosed.RemoveAll(r => r.DocumentId == documentId);

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetOpenTabsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();
            return metadata.OpenTabs
                .OrderBy(t => t.Order)
                .Select(t => t.DocumentId)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<RecentlyClosedInfo>> GetRecentlyClosedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();
            return metadata.RecentlyClosed.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetDocumentTitleAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                return docMeta.Title;
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Guid?> GetActiveTabDocumentIdAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();
            return metadata.ActiveTabDocumentId;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetActiveTabDocumentIdAsync(Guid? documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();
            metadata.ActiveTabDocumentId = documentId;
            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetDocumentWordWrapAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                return docMeta.WordWrap;
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateDocumentWordWrapAsync(Guid documentId, string? wordWrap)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (!metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                docMeta = new DocumentMetadata();
                metadata.Documents[documentId] = docMeta;
            }

            // Normalize "Default" to null
            docMeta.WordWrap = (string.IsNullOrWhiteSpace(wordWrap) || wordWrap == "Default") ? null : wordWrap;

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetDocumentSyntaxLanguageAsync(Guid documentId)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                return docMeta.SyntaxLanguage;
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateDocumentSyntaxLanguageAsync(Guid documentId, string? syntaxLanguage)
    {
        await _lock.WaitAsync();
        try
        {
            var metadata = await LoadFromFileOrCacheAsync();

            if (!metadata.Documents.TryGetValue(documentId, out var docMeta))
            {
                docMeta = new DocumentMetadata();
                metadata.Documents[documentId] = docMeta;
            }

            // Normalize "PlainText" to null
            docMeta.SyntaxLanguage = (string.IsNullOrWhiteSpace(syntaxLanguage) || syntaxLanguage == "PlainText") ? null : syntaxLanguage;

            await SaveToFileAsync(metadata);
            _cachedMetadata = metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads metadata from cache if available, otherwise from file.
    /// Must be called within the lock.
    /// </summary>
    private async Task<Metadata> LoadFromFileOrCacheAsync()
    {
        if (_cachedMetadata != null)
        {
            return _cachedMetadata;
        }

        _cachedMetadata = await LoadFromFileAsync();
        return _cachedMetadata;
    }

    /// <summary>
    /// Loads metadata from the JSON file.
    /// Returns default metadata if file doesn't exist or is corrupted.
    /// </summary>
    private async Task<Metadata> LoadFromFileAsync()
    {
        if (!File.Exists(_metadataFilePath))
        {
            return CreateDefaultMetadata();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_metadataFilePath);
            var metadata = JsonSerializer.Deserialize<Metadata>(json, _jsonOptions);

            if (metadata != null)
            {
                return metadata;
            }
        }
        catch (JsonException)
        {
            // JSON is corrupted, try to recover from backup
            var backupPath = _metadataFilePath + BackupExtension;
            if (File.Exists(backupPath))
            {
                try
                {
                    var backupJson = await File.ReadAllTextAsync(backupPath);
                    var backupMetadata = JsonSerializer.Deserialize<Metadata>(backupJson, _jsonOptions);

                    if (backupMetadata != null)
                    {
                        // Restore from backup
                        await SaveToFileAsync(backupMetadata);
                        return backupMetadata;
                    }
                }
                catch
                {
                    // Backup also corrupted, fall through to default
                }
            }
        }
        catch (IOException)
        {
            // File may be inaccessible, return default
        }

        return CreateDefaultMetadata();
    }

    /// <summary>
    /// Saves metadata to the JSON file using atomic write.
    /// Creates a backup before writing.
    /// </summary>
    private async Task SaveToFileAsync(Metadata metadata)
    {
        var json = JsonSerializer.Serialize(metadata, _jsonOptions);

        // Create backup of existing file if it exists
        if (File.Exists(_metadataFilePath))
        {
            var backupPath = _metadataFilePath + BackupExtension;
            try
            {
                File.Copy(_metadataFilePath, backupPath, overwrite: true);
            }
            catch
            {
                // Best effort backup, continue even if backup fails
            }
        }

        // Atomic write: write to temp file, then rename
        var tempPath = Path.Combine(_storageDirectory, $".tmp_{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _metadataFilePath, overwrite: true);
        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }

    /// <summary>
    /// Creates a default metadata object.
    /// </summary>
    private static Metadata CreateDefaultMetadata()
    {
        return new Metadata
        {
            Version = "1.0",
            OpenTabs = new List<OpenTabInfo>(),
            RecentlyClosed = new List<RecentlyClosedInfo>(),
            Documents = new Dictionary<Guid, DocumentMetadata>()
        };
    }

    /// <summary>
    /// Disposes resources used by the metadata store.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _lock.Dispose();
        }

        _disposed = true;
    }
}
