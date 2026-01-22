namespace Scrapile.Application.Services;

using Scrapile.Application.DTOs;
using Scrapile.Application.Helpers;
using Scrapile.Domain.Entities;
using Scrapile.Domain.Interfaces;

/// <summary>
/// Service for document operations that coordinates between the repository and metadata store.
/// Provides document retrieval with calculated statistics.
/// </summary>
public class DocumentService
{
    private readonly IDocumentRepository _repository;

    /// <summary>
    /// Creates a new DocumentService.
    /// </summary>
    /// <param name="repository">The document repository for storage operations.</param>
    public DocumentService(IDocumentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Creates a new document with optional title.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <param name="title">Optional title for the document.</param>
    /// <returns>The created document with calculated stats.</returns>
    public async Task<DocumentWithStats> CreateAsync(string content, string? title = null)
    {
        var document = await _repository.CreateAsync(content, title);
        return EnrichWithStats(document);
    }

    /// <summary>
    /// Gets a document by ID with calculated stats.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <returns>The document with stats, or null if not found.</returns>
    public async Task<DocumentWithStats?> GetByIdAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);
        return document != null ? EnrichWithStats(document) : null;
    }

    /// <summary>
    /// Gets all documents with calculated stats.
    /// </summary>
    /// <returns>All documents with their stats.</returns>
    public async Task<IEnumerable<DocumentWithStats>> GetAllAsync()
    {
        var documents = await _repository.GetAllAsync();
        return documents.Select(EnrichWithStats);
    }

    /// <summary>
    /// Updates the content of a document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="content">The new content.</param>
    public async Task UpdateContentAsync(Guid id, string content)
    {
        await _repository.UpdateContentAsync(id, content);
    }

    /// <summary>
    /// Updates the title of a document.
    /// Title is stored in metadata only, not in the file content.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="title">The new title, or null to remove the title.</param>
    public async Task UpdateTitleAsync(Guid id, string? title)
    {
        // Normalize empty string to null (no title)
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : title;
        await _repository.UpdateTitleAsync(id, normalizedTitle);
    }

    /// <summary>
    /// Deletes a document.
    /// Removes both the file and metadata entry.
    /// </summary>
    /// <param name="id">The document ID.</param>
    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// Searches documents by title and content.
    /// </summary>
    /// <param name="query">The search query (case-insensitive substring match).</param>
    /// <returns>Matching documents with stats, sorted by relevance.</returns>
    public async Task<IEnumerable<DocumentWithStats>> SearchAsync(string query)
    {
        var documents = await _repository.SearchAsync(query);
        return documents.Select(EnrichWithStats);
    }

    /// <summary>
    /// Enriches a document with calculated statistics.
    /// </summary>
    /// <param name="document">The document to enrich.</param>
    /// <returns>Document with calculated stats.</returns>
    private static DocumentWithStats EnrichWithStats(Document document)
    {
        var wordCount = ContentHelper.CountWords(document.Content);
        var charCount = ContentHelper.CountCharacters(document.Content);
        var preview = ContentHelper.GetContentPreview(document.Content);

        return new DocumentWithStats
        {
            Document = document,
            WordCount = wordCount,
            CharacterCount = charCount,
            ContentPreview = preview,
            FormattedWordCount = $"{ContentHelper.FormatCount(wordCount)} words",
            FormattedCharacterCount = $"{ContentHelper.FormatCount(charCount)} chars"
        };
    }
}
