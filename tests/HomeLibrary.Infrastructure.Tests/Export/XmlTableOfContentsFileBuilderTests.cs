using System.Globalization;
using System.Text;
using System.Xml.Linq;
using HomeLibrary.Domain.Books;
using HomeLibrary.Infrastructure.Export;

namespace HomeLibrary.Infrastructure.Tests.Export;

public sealed class XmlTableOfContentsFileBuilderTests
{
    private const long BOOK_ID = 12L;
    private const int VERSION = 1;

    // Cyrillic text is the subject of the test: the file must be written in UTF-8 without mojibake.
    private const string CYRILLIC_TITLE = "Война и мир";
    private const string CYRILLIC_AUTHOR = "Лев Толстой";
    private const string CYRILLIC_CHAPTER = "Глава 1";

    private static readonly DateTimeOffset _timestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly XmlTableOfContentsFileBuilder _builder = new();

    [Fact]
    public void Build_ReturnsSelfDescribingUtf8XmlFile()
    {
        var toc = new TableOfContents($"<toc><ol><li>{CYRILLIC_CHAPTER}</li></ol></toc>");
        var details = new BookDetails(CYRILLIC_TITLE, CYRILLIC_AUTHOR, null, null, null, null, null, null, toc);

        var file = _builder.Build(new Book(BOOK_ID, details, VERSION, _timestamp, _timestamp), toc);

        Assert.Equal("book-12-toc.xml", file.FileName);
        Assert.Equal(XmlTableOfContentsFileBuilder.CONTENT_TYPE, file.ContentType);

        var text = Encoding.UTF8.GetString(file.Content);
        var root = XDocument.Parse(text).Root!;

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", text);
        Assert.Equal(BOOK_ID.ToString(CultureInfo.InvariantCulture), root.Attribute("bookId")?.Value);
        Assert.Equal(CYRILLIC_TITLE, root.Attribute("title")?.Value);
        Assert.Equal(CYRILLIC_AUTHOR, root.Attribute("author")?.Value);
        Assert.Equal(CYRILLIC_CHAPTER, root.Value);
    }

    [Fact]
    public void Build_PreservesSpaceBetweenInlineElements()
    {
        var toc = new TableOfContents("<toc><p><strong>Chapter 1.</strong> <em>Introduction</em></p></toc>");
        var details = new BookDetails("Title", "Author", null, null, null, null, null, null, toc);

        var file = _builder.Build(new Book(BOOK_ID, details, VERSION, _timestamp, _timestamp), toc);

        var root = XDocument.Parse(Encoding.UTF8.GetString(file.Content), LoadOptions.PreserveWhitespace).Root!;

        Assert.Equal("Chapter 1. Introduction", root.Element("p")?.Value);
    }
}
