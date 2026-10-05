using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Infrastructure.Export;

/// <summary>
/// Builds a UTF-8 XML file with the table of contents exactly as stored. The root element gets the book identifier, title and author
/// as attributes, so the file describes itself.
/// </summary>
public sealed class XmlTableOfContentsFileBuilder : ITableOfContentsFileBuilder
{
    public const string CONTENT_TYPE = "application/xml";

    private const string FILE_NAME_FORMAT = "book-{0}-toc.xml";
    private const string XML_VERSION = "1.0";
    private const string XML_ENCODING = "utf-8";
    private const string BOOK_ID_ATTRIBUTE = "bookId";
    private const string TITLE_ATTRIBUTE = "title";
    private const string AUTHOR_ATTRIBUTE = "author";

    private static readonly XmlWriterSettings _writerSettings = new()
    {
        // No indentation: it would insert whitespace into mixed content (for example, before <strong> inside <p>).
        Indent = false,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
    };

    public TableOfContentsFile Build(Book book, TableOfContents tableOfContents)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(tableOfContents);

        var fileName = string.Format(CultureInfo.InvariantCulture, FILE_NAME_FORMAT, book.Id);

        return new TableOfContentsFile(fileName, CONTENT_TYPE, BuildContent(book, tableOfContents));
    }

    private static byte[] BuildContent(Book book, TableOfContents tableOfContents)
    {
        var root = XElement.Parse(tableOfContents.Xml, LoadOptions.PreserveWhitespace);

        root.SetAttributeValue(BOOK_ID_ATTRIBUTE, book.Id);
        root.SetAttributeValue(TITLE_ATTRIBUTE, book.Details.Title);
        root.SetAttributeValue(AUTHOR_ATTRIBUTE, book.Details.Author);

        var document = new XDocument(new XDeclaration(XML_VERSION, XML_ENCODING, standalone: null), root);

        using var stream = new MemoryStream();

        using (var writer = XmlWriter.Create(stream, _writerSettings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
