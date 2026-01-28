using Scrapile.Application.Helpers;

namespace Scrapile.Application.Tests;

public class ContentHelperTests
{
    #region GetContentPreview Tests

    [Fact]
    public void GetContentPreview_NullContent_ReturnsEmpty()
    {
        var result = ContentHelper.GetContentPreview(null);
        Assert.Equal("(empty)", result);
    }

    [Fact]
    public void GetContentPreview_EmptyString_ReturnsEmpty()
    {
        var result = ContentHelper.GetContentPreview("");
        Assert.Equal("(empty)", result);
    }

    [Fact]
    public void GetContentPreview_WhitespaceOnly_ReturnsEmpty()
    {
        var result = ContentHelper.GetContentPreview("   \t\n\r  ");
        Assert.Equal("(empty)", result);
    }

    [Fact]
    public void GetContentPreview_ShortContent_ReturnsFullContent()
    {
        var result = ContentHelper.GetContentPreview("Hello World");
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void GetContentPreview_ExactlyPreviewLength_ReturnsFullContent()
    {
        var content = new string('a', 40);
        var result = ContentHelper.GetContentPreview(content);
        Assert.Equal(content, result);
    }

    [Fact]
    public void GetContentPreview_LongContent_TruncatesWithEllipsis()
    {
        var content = new string('a', 50);
        var result = ContentHelper.GetContentPreview(content);
        Assert.Equal(new string('a', 40) + "...", result);
    }

    [Fact]
    public void GetContentPreview_ContentWithNewlines_CollapsesToSpaces()
    {
        var result = ContentHelper.GetContentPreview("Hello\nWorld\r\nTest");
        Assert.Equal("Hello World Test", result);
    }

    [Fact]
    public void GetContentPreview_ContentWithMultipleSpaces_CollapsesToSingleSpace()
    {
        var result = ContentHelper.GetContentPreview("Hello    World");
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void GetContentPreview_ContentWithTabs_CollapsesToSpaces()
    {
        var result = ContentHelper.GetContentPreview("Hello\t\tWorld");
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void GetContentPreview_LeadingAndTrailingWhitespace_Trimmed()
    {
        var result = ContentHelper.GetContentPreview("  Hello World  ");
        Assert.Equal("Hello World", result);
    }

    #endregion

    #region CountWords Tests

    [Fact]
    public void CountWords_NullContent_ReturnsZero()
    {
        var result = ContentHelper.CountWords(null);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountWords_EmptyString_ReturnsZero()
    {
        var result = ContentHelper.CountWords("");
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountWords_WhitespaceOnly_ReturnsZero()
    {
        var result = ContentHelper.CountWords("   \t\n\r  ");
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountWords_SingleWord_ReturnsOne()
    {
        var result = ContentHelper.CountWords("Hello");
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountWords_MultipleWords_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("Hello World Test");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountWords_WordsSeparatedByNewlines_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("Hello\nWorld\r\nTest");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountWords_WordsSeparatedByTabs_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("Hello\tWorld\t\tTest");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountWords_MultipleWhitespaceBetweenWords_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("Hello    World     Test");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountWords_MixedWhitespace_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("Hello \t\n World \r\n Test");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountWords_LeadingAndTrailingWhitespace_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountWords("   Hello World   ");
        Assert.Equal(2, result);
    }

    #endregion

    #region CountCharacters Tests

    [Fact]
    public void CountCharacters_NullContent_ReturnsZero()
    {
        var result = ContentHelper.CountCharacters(null);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountCharacters_EmptyString_ReturnsZero()
    {
        var result = ContentHelper.CountCharacters("");
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountCharacters_SimpleString_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountCharacters("Hello");
        Assert.Equal(5, result);
    }

    [Fact]
    public void CountCharacters_IncludesWhitespace()
    {
        var result = ContentHelper.CountCharacters("Hello World");
        Assert.Equal(11, result);
    }

    [Fact]
    public void CountCharacters_IncludesNewlines()
    {
        var result = ContentHelper.CountCharacters("Hello\nWorld");
        Assert.Equal(11, result);
    }

    [Fact]
    public void CountCharacters_LargeString_ReturnsCorrectCount()
    {
        var content = new string('a', 100000);
        var result = ContentHelper.CountCharacters(content);
        Assert.Equal(100000, result);
    }

    #endregion

    #region FormatCount Tests

    [Fact]
    public void FormatCount_Zero_ReturnsZero()
    {
        var result = ContentHelper.FormatCount(0);
        Assert.Equal("0", result);
    }

    [Fact]
    public void FormatCount_NegativeNumber_ReturnsZero()
    {
        var result = ContentHelper.FormatCount(-5);
        Assert.Equal("0", result);
    }

    [Fact]
    public void FormatCount_SmallNumber_ReturnsExact()
    {
        var result = ContentHelper.FormatCount(245);
        Assert.Equal("245", result);
    }

    [Fact]
    public void FormatCount_JustUnderThousand_ReturnsExact()
    {
        var result = ContentHelper.FormatCount(999);
        Assert.Equal("999", result);
    }

    [Fact]
    public void FormatCount_ExactlyThousand_ReturnsOneDecimal()
    {
        var result = ContentHelper.FormatCount(1000);
        Assert.Equal("1.0k", result);
    }

    [Fact]
    public void FormatCount_Thousands_ReturnsOneDecimal()
    {
        var result = ContentHelper.FormatCount(1500);
        Assert.Equal("1.5k", result);
    }

    [Fact]
    public void FormatCount_ThousandsRounding()
    {
        var result = ContentHelper.FormatCount(2345);
        Assert.Equal("2.3k", result);
    }

    [Fact]
    public void FormatCount_JustUnderTenThousand_ReturnsOneDecimal()
    {
        var result = ContentHelper.FormatCount(9999);
        Assert.Equal("10.0k", result);
    }

    [Fact]
    public void FormatCount_TenThousand_ReturnsNoDecimal()
    {
        var result = ContentHelper.FormatCount(10000);
        Assert.Equal("10k", result);
    }

    [Fact]
    public void FormatCount_LargeNumber_ReturnsNoDecimal()
    {
        var result = ContentHelper.FormatCount(23456);
        Assert.Equal("23k", result);
    }

    [Fact]
    public void FormatCount_HundredThousand_ReturnsNoDecimal()
    {
        var result = ContentHelper.FormatCount(100000);
        Assert.Equal("100k", result);
    }

    [Fact]
    public void FormatCount_Million_ReturnsNoDecimal()
    {
        var result = ContentHelper.FormatCount(1000000);
        Assert.Equal("1000k", result);
    }

    [Fact]
    public void FormatCount_OverMillion_ReturnsNoDecimal()
    {
        var result = ContentHelper.FormatCount(1500000);
        Assert.Equal("1500k", result);
    }

    #endregion

    #region CountLines Tests

    [Fact]
    public void CountLines_NullContent_ReturnsZero()
    {
        var result = ContentHelper.CountLines(null);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountLines_EmptyString_ReturnsZero()
    {
        var result = ContentHelper.CountLines("");
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountLines_SingleLine_ReturnsOne()
    {
        var result = ContentHelper.CountLines("Hello World");
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountLines_MultipleLines_ReturnsCorrectCount()
    {
        var result = ContentHelper.CountLines("Line 1\nLine 2\nLine 3");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountLines_TrailingNewline_CountsExtraLine()
    {
        var result = ContentHelper.CountLines("Line 1\nLine 2\n");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountLines_WindowsLineEndings_CountsCorrectly()
    {
        // \r\n should count as one line break (the \n part)
        var result = ContentHelper.CountLines("Line 1\r\nLine 2\r\nLine 3");
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountLines_OnlyNewlines_CountsCorrectly()
    {
        var result = ContentHelper.CountLines("\n\n\n");
        Assert.Equal(4, result);
    }

    [Fact]
    public void CountLines_MixedLineEndings_CountsCorrectly()
    {
        var result = ContentHelper.CountLines("Line 1\nLine 2\r\nLine 3\rLine 4");
        // Only \n counts as line break, \r alone doesn't
        Assert.Equal(3, result);
    }

    #endregion
}
