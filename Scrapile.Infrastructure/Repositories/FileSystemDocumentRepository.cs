namespace Scrapile.Infrastructure.Repositories;

using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// File system-based implementation of IDocumentRepository.
/// Documents are stored as plain text files with naming convention: {timestamp}_{guid}.txt
/// </summary>
public class FileSystemDocumentRepository : IDocumentRepository
{
    private readonly string _storageDirectory;
    private readonly IMetadataStore? _metadataStore;

    /// <summary>
    /// Creates a new FileSystemDocumentRepository.
    /// </summary>
    /// <param name="storageDirectory">Directory where documents will be stored.</param>
    /// <param name="metadataStore">Optional metadata store for title management.</param>
    public FileSystemDocumentRepository(string storageDirectory, IMetadataStore? metadataStore = null)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory cannot be null or empty.", nameof(storageDirectory));
        }

        _storageDirectory = storageDirectory;
        _metadataStore = metadataStore;

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirectory);
    }

    /// <inheritdoc />
    public async Task<Document> CreateAsync(string content, string? title = null)
    {
        var id = Guid.NewGuid();
        var filename = GenerateFilename(id);
        var filePath = Path.Combine(_storageDirectory, filename);

        // Atomic write: write to temp file, then rename
        await AtomicWriteAsync(filePath, content);

        var fileInfo = new FileInfo(filePath);
        var document = new Document
        {
            Id = id,
            Filename = filename,
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Content = content,
            Created = fileInfo.CreationTimeUtc,
            LastModified = fileInfo.LastWriteTimeUtc
        };

        // Store document in metadata if metadata store is available
        if (_metadataStore != null)
        {
            await _metadataStore.AddDocumentAsync(id, title);
        }

        return document;
    }

    /// <inheritdoc />
    public async Task<Document?> GetByIdAsync(Guid id)
    {
        var (filePath, filename) = await FindFileByIdAsync(id);
        if (filePath == null || filename == null)
        {
            return null;
        }

        return await LoadDocumentFromFileAsync(filePath, id, filename);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        var documents = new List<Document>();

        if (!Directory.Exists(_storageDirectory))
        {
            return documents;
        }

        var files = Directory.GetFiles(_storageDirectory, "*.txt");

        foreach (var filePath in files)
        {
            var filename = Path.GetFileName(filePath);
            var id = ExtractIdFromFilename(filename);
            if (id.HasValue)
            {
                var document = await LoadDocumentFromFileAsync(filePath, id.Value, filename);
                if (document != null)
                {
                    documents.Add(document);
                }
            }
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task UpdateContentAsync(Guid id, string content)
    {
        var (filePath, _) = await FindFileByIdAsync(id);
        if (filePath == null)
        {
            throw new FileNotFoundException($"Document with ID {id} not found.");
        }

        // Atomic write: write to temp file, then rename
        await AtomicWriteAsync(filePath, content);
    }

    /// <inheritdoc />
    public async Task UpdateTitleAsync(Guid id, string? title)
    {
        // Titles are stored in metadata, not in the file itself.
        // Delegate to metadata store if available.
        if (_metadataStore != null)
        {
            await _metadataStore.UpdateDocumentTitleAsync(id, title);
        }
        else
        {
            // Without metadata store, title updates are not persisted.
            // This is expected during early development phases.
            await Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var (filePath, _) = await FindFileByIdAsync(id);
        if (filePath == null)
        {
            // File already doesn't exist, consider deletion successful
            return;
        }

        await Task.Run(() => File.Delete(filePath));

        // Remove document from metadata if available
        if (_metadataStore != null)
        {
            await _metadataStore.RemoveDocumentAsync(id);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<Document>> SearchAsync(string query)
    {
        // Search implementation is Task 2.3
        throw new NotImplementedException("Search functionality will be implemented in Task 2.3.");
    }

    /// <summary>
    /// Generates a filename for a document: {timestamp}_{guid}.txt
    /// Example: 20250122143022_a3f5b2e1c4d34e5f8a9b1c2d3e4f5a6b.txt
    /// </summary>
    private static string GenerateFilename(Guid id)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guidString = id.ToString("N"); // 32 hex chars, no hyphens
        return $"{timestamp}_{guidString}.txt";
    }

    /// <summary>
    /// Extracts the GUID from a filename.
    /// </summary>
    private static Guid? ExtractIdFromFilename(string filename)
    {
        // Filename format: {timestamp}_{guid}.txt
        // Example: 20250122143022_a3f5b2e1c4d34e5f8a9b1c2d3e4f5a6b.txt

        if (string.IsNullOrEmpty(filename))
        {
            return null;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var underscoreIndex = nameWithoutExtension.IndexOf('_');

        if (underscoreIndex < 0 || underscoreIndex >= nameWithoutExtension.Length - 1)
        {
            return null;
        }

        var guidPart = nameWithoutExtension[(underscoreIndex + 1)..];

        if (Guid.TryParse(guidPart, out var id))
        {
            return id;
        }

        return null;
    }

    /// <summary>
    /// Finds a file by document ID.
    /// </summary>
    private Task<(string? FilePath, string? Filename)> FindFileByIdAsync(Guid id)
    {
        if (!Directory.Exists(_storageDirectory))
        {
            return Task.FromResult<(string?, string?)>((null, null));
        }

        var guidString = id.ToString("N");
        var files = Directory.GetFiles(_storageDirectory, $"*_{guidString}.txt");

        if (files.Length > 0)
        {
            var filePath = files[0];
            var filename = Path.GetFileName(filePath);
            return Task.FromResult<(string?, string?)>((filePath, filename));
        }

        return Task.FromResult<(string?, string?)>((null, null));
    }

    /// <summary>
    /// Loads a document from a file.
    /// </summary>
    private async Task<Document?> LoadDocumentFromFileAsync(string filePath, Guid id, string filename)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var fileInfo = new FileInfo(filePath);

            // Get title from metadata if available
            string? title = null;
            if (_metadataStore != null)
            {
                title = await _metadataStore.GetDocumentTitleAsync(id);
            }

            return new Document
            {
                Id = id,
                Filename = filename,
                Title = title,
                Content = content,
                Created = fileInfo.CreationTimeUtc,
                LastModified = fileInfo.LastWriteTimeUtc
            };
        }
        catch (IOException)
        {
            // File may have been deleted or is inaccessible
            return null;
        }
    }

    /// <summary>
    /// Performs an atomic write by writing to a temp file and then renaming.
    /// This prevents data corruption if the application crashes during write.
    /// </summary>
    private static async Task AtomicWriteAsync(string targetPath, string content)
    {
        var directory = Path.GetDirectoryName(targetPath) ?? ".";
        var tempPath = Path.Combine(directory, $".tmp_{Guid.NewGuid():N}.txt");

        try
        {
            // Write to temp file
            await File.WriteAllTextAsync(tempPath, content);

            // Rename (atomic on most file systems)
            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            // Clean up temp file if it still exists (in case of error before move)
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
}
