using System.Xml;
using System.Xml.Linq;
using AngleSharp.Xhtml;
using Ganss.Xss;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Infrastructure.Html;

/// <summary>
/// Converts the table of contents between the editor HTML and the stored XML.
/// The HTML is sanitized to a small set of structural tags without attributes, serialized as XHTML
/// and wrapped into the <c>&lt;toc&gt;</c> root element. Unsupported formatting elements (links, tables...) are unwrapped
/// and keep their text, like the editor does; executable and embedded content is removed completely.
/// Whitespace is preserved, because text between inline elements is meaningful.
/// </summary>
public sealed class HtmlTableOfContentsConverter : ITableOfContentsConverter
{
    private static readonly string[] _allowedTags =
    [
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "p",
        "ul",
        "ol",
        "li",
        "strong",
        "b",
        "em",
        "i",
        "u",
        "s",
        "br",
        "div",
        "span"
    ];

    private static readonly HashSet<string> _elementsRemovedWithContent = new(
        [
            "script",
            "style",
            "template",
            "noscript",
            "iframe",
            "object",
            "embed",
            "svg",
            "math",
            "textarea",
            "select",
            "title"
        ],
        StringComparer.OrdinalIgnoreCase);

    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    public TableOfContents? FromHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var xhtml = _sanitizer.Sanitize(html, string.Empty, XhtmlMarkupFormatter.Instance);
        var root = ParseRoot(xhtml);

        return string.IsNullOrWhiteSpace(root.Value) ? null : new TableOfContents(root.ToString(SaveOptions.DisableFormatting));
    }

    public string ToHtml(TableOfContents tableOfContents)
    {
        ArgumentNullException.ThrowIfNull(tableOfContents);

        var root = XElement.Parse(tableOfContents.Xml, LoadOptions.PreserveWhitespace);
        var innerXhtml = string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));

        // The stored XML is sanitized on write; sanitizing again protects against rows changed directly in the database.
        return _sanitizer.Sanitize(innerXhtml);
    }

    private static XElement ParseRoot(string xhtml)
    {
        try
        {
            return XElement.Parse(
                $"<{TableOfContents.ROOT_ELEMENT_NAME}>{xhtml}</{TableOfContents.ROOT_ELEMENT_NAME}>",
                LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            // For example, control characters written as character references (&#1;) are valid in HTML but not in XML.
            throw new DomainValidationException(
                nameof(BookDetails.TableOfContents),
                $"Table of contents contains characters that are not allowed in XML: {exception.Message}");
        }
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(_allowedTags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        var sanitizer = new HtmlSanitizer(options)
        {
            KeepChildNodes = true
        };

        sanitizer.RemovingTag += (_, args) =>
        {
            if (_elementsRemovedWithContent.Contains(args.Tag.LocalName))
            {
                args.Tag.TextContent = string.Empty;
            }
        };

        return sanitizer;
    }
}
