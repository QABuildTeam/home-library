using System.Text.RegularExpressions;
using System.Xml.Linq;
using HomeLibrary.Domain.Books;
using HomeLibrary.Infrastructure.Html;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Builds the plain text of a table of contents for substring search (the <c>toc_text</c> column).
/// Inline elements are parts of the same word ("Intro&lt;em&gt;duction&lt;/em&gt;" gives "Introduction"); block
/// elements, line breaks and unknown elements separate words. Whitespace runs, including no-break spaces, collapse into
/// one ordinary space, like the search text is normalized by the normalize_search_text database function.
/// Changing these rules does not update the books already stored: a new migration has to rebuild their text
/// (see MoveTocSearchTextToApplication).
/// </summary>
internal static partial class TableOfContentsSearchText
{
    private const char SEPARATOR = ' ';

    // In .NET \s already matches the no-break space U+00A0 (Unicode category Zs); the SQL function replaces it
    // explicitly because there \s depends on the database locale.
    private const string WHITESPACE_RUN_PATTERN = @"\s+";

    /// <summary>
    /// Extracts the search text.
    /// </summary>
    /// <returns>The text, or <c>null</c> when there is no table of contents.</returns>
    public static string? Extract(TableOfContents? tableOfContents)
    {
        if (tableOfContents is null)
        {
            return null;
        }

        var text = ToText(XElement.Parse(tableOfContents.Xml, LoadOptions.PreserveWhitespace));

        return WhitespaceRunRegex()
            .Replace(text, SEPARATOR.ToString())
            .Trim();
    }

    /// <summary>
    /// Text of a node. XText values are already decoded ("&amp;amp;" in the XML is "&amp;" here).
    /// Inline elements continue the word; all other elements (blocks, line breaks, unknown ones) separate words.
    /// </summary>
    private static string ToText(XNode node) => node switch
    {
        XText text => text.Value,
        XElement element when TableOfContentsMarkup.InlineElements.Contains(element.Name.LocalName) => ChildrenText(element),
        XElement element => $"{SEPARATOR}{ChildrenText(element)}{SEPARATOR}",
        _ => string.Empty
    };

    private static string ChildrenText(XElement element) => string.Concat(element.Nodes().Select(ToText));

    [GeneratedRegex(WHITESPACE_RUN_PATTERN, RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRunRegex();
}
