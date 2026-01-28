using System.Text.RegularExpressions;

namespace Scrapile.Application.Helpers;

/// <summary>
/// Provides utility methods for content processing including previews, word counts, and formatting.
/// </summary>
public static partial class ContentHelper
{
    private const int PreviewLength = 40;

    /// <summary>
    /// Extracts a preview of the content, limited to the first 40 characters.
    /// </summary>
    /// <param name="content">The content to extract a preview from.</param>
    /// <returns>A preview string, or "(empty)" if the content is null/whitespace.</returns>
    public static string GetContentPreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "(empty)";

        // Remove extra whitespace and newlines, collapse to single spaces
        var cleaned = WhitespaceRegex().Replace(content.Trim(), " ");

        if (cleaned.Length <= PreviewLength)
            return cleaned;

        return string.Concat(cleaned.AsSpan(0, PreviewLength), "...");
    }

    /// <summary>
    /// Counts the number of words in the content.
    /// </summary>
    /// <param name="content">The content to count words in.</param>
    /// <returns>The word count, or 0 if content is null/whitespace.</returns>
    public static int CountWords(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Split on all whitespace types (space, tab, newline, carriage return, etc.)
        return content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Counts the total number of characters in the content.
    /// </summary>
    /// <param name="content">The content to count characters in.</param>
    /// <returns>The character count, or 0 if content is null.</returns>
    public static int CountCharacters(string? content)
    {
        return content?.Length ?? 0;
    }

    /// <summary>
    /// Counts the number of lines in the content.
    /// </summary>
    /// <param name="content">The content to count lines in.</param>
    /// <returns>The line count (1 for single line, 0 for null/empty).</returns>
    public static int CountLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        int count = 1;
        foreach (char c in content)
        {
            if (c == '\n')
                count++;
        }
        return count;
    }

    /// <summary>
    /// Formats a count for display, abbreviating large numbers.
    /// </summary>
    /// <param name="count">The count to format.</param>
    /// <returns>
    /// Formatted string: exact number for &lt;1000, "X.Xk" for 1000-9999, "Xk" for 10000+.
    /// </returns>
    public static string FormatCount(int count)
    {
        if (count < 0)
            return "0";

        if (count < 1000)
            return count.ToString();

        if (count < 10000)
            return $"{count / 1000.0:F1}k";

        return $"{count / 1000}k";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
