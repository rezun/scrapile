namespace Scrapile.Domain.Interfaces;

using Scrapile.Domain.Entities;

/// <summary>
/// Repository interface for document storage operations.
/// Implementations handle the actual persistence mechanism (file system, database, etc.).
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Creates a new document with the given content.
    /// </summary>
    /// <param name="content">The document content.</param>
    /// <param name="title">Optional user-provided title.</param>
    /// <returns>The created document with generated ID and filename.</returns>
    Task<Document> CreateAsync(string content, string? title = null);

    /// <summary>
    /// Retrieves a document by its ID.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <returns>The document, or null if not found.</returns>
    Task<Document?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all documents.
    /// </summary>
    /// <returns>All documents in storage.</returns>
    Task<IEnumerable<Document>> GetAllAsync();

    /// <summary>
    /// Updates the content of an existing document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="content">The new content.</param>
    Task UpdateContentAsync(Guid id, string content);

    /// <summary>
    /// Updates the title of a document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="title">The new title, or null to remove the title.</param>
    Task UpdateTitleAsync(Guid id, string? title);

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Searches documents by title and content.
    /// </summary>
    /// <param name="query">The search query (case-insensitive substring match).</param>
    /// <returns>Matching documents sorted by relevance (title matches first, then by last modified).</returns>
    Task<IEnumerable<Document>> SearchAsync(string query);
}
