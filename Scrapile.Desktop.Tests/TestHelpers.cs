using Scrapile.Application.DTOs;
using Scrapile.Application.Helpers;
using Scrapile.Domain.Entities;

namespace Scrapile.Desktop.Tests;

/// <summary>
/// Shared test data builders and utilities for Desktop tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a Document with the specified properties.
    /// </summary>
    public static Document CreateDocument(string content = "", string? title = null, Guid? id = null)
    {
        return new Document
        {
            Id = id ?? Guid.NewGuid(),
            Filename = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.txt",
            Title = title,
            Content = content,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a Tab with the specified properties.
    /// </summary>
    public static Tab CreateTab(Document? document = null, string? content = null, int order = 0, bool isDirty = false, Guid? tabId = null)
    {
        var doc = document ?? CreateDocument(content ?? string.Empty);
        return new Tab
        {
            TabId = tabId ?? Guid.NewGuid(),
            Document = doc,
            Content = content ?? doc.Content,
            Order = order,
            IsDirty = isDirty
        };
    }

    /// <summary>
    /// Creates a TabWithStats from a Tab.
    /// </summary>
    public static TabWithStats CreateTabWithStats(Tab tab)
    {
        var wordCount = ContentHelper.CountWords(tab.Content);
        var charCount = ContentHelper.CountCharacters(tab.Content);
        var preview = ContentHelper.GetContentPreview(tab.Content);

        return new TabWithStats
        {
            Tab = tab,
            WordCount = wordCount,
            CharacterCount = charCount,
            ContentPreview = preview,
            FormattedWordCount = $"{ContentHelper.FormatCount(wordCount)} words",
            FormattedCharacterCount = $"{ContentHelper.FormatCount(charCount)} chars"
        };
    }

    /// <summary>
    /// Creates a TabWithStats with the specified properties.
    /// </summary>
    public static TabWithStats CreateTabWithStats(
        string content = "",
        string? title = null,
        int order = 0,
        bool isDirty = false,
        Guid? tabId = null,
        Guid? documentId = null)
    {
        var document = CreateDocument(content, title, documentId);
        var tab = CreateTab(document, content, order, isDirty, tabId);
        return CreateTabWithStats(tab);
    }
}
