using System.Xml;
using System.Xml.Linq;
using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Domain.Books;

/// <summary>
/// Table of contents of a book: a well-formed XML document with the <c>&lt;toc&gt;</c> root element.
/// Two tables of contents are equal when their normalized XML texts are equal.
/// </summary>
/// <param name="xml">XML text of the table of contents.</param>
public sealed class TableOfContents(string xml) : IEquatable<TableOfContents>
{
    public const string ROOT_ELEMENT_NAME = "toc";

    private static readonly XmlReaderSettings _readerSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };

    /// <summary>
    /// XML text of the root element. The XML declaration and nodes outside the root element are removed;
    /// whitespace inside the root element is preserved, because it separates words in mixed content.
    /// </summary>
    public string Xml { get; } = Normalize(xml);

    public bool Equals(TableOfContents? other) => other is not null && string.Equals(Xml, other.Xml, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as TableOfContents);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Xml);

    private static string Normalize(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var root = Parse(xml).Root;

        if (root is null
            || root.Name.LocalName != ROOT_ELEMENT_NAME
            || root.Name.Namespace != XNamespace.None)
        {
            throw new DomainValidationException(
                nameof(BookDetails.TableOfContents),
                $"Table of contents must have the <{ROOT_ELEMENT_NAME}> root element without a namespace.");
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static XDocument Parse(string xml)
    {
        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, _readerSettings);

            return XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            throw new DomainValidationException(
                nameof(BookDetails.TableOfContents),
                $"Table of contents is not a well-formed XML document: {exception.Message}");
        }
    }
}
