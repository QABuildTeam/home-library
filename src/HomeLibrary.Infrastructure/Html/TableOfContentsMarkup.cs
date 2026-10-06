using System.Collections.Frozen;

namespace HomeLibrary.Infrastructure.Html;

/// <summary>
/// Elements allowed in the stored table of contents, split by their role in the text flow.
/// The sanitizer allows exactly these elements; the search text treats inline elements as parts of a word
/// and block elements and line breaks as word boundaries. The editor (wwwroot/js/toc-editor.js) uses the same list.
/// </summary>
internal static class TableOfContentsMarkup
{
    public const string LINE_BREAK = "br";

    public static readonly FrozenSet<string> InlineElements = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "strong",
        "b",
        "em",
        "i",
        "u",
        "s",
        "span");

    public static readonly FrozenSet<string> BlockElements = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "p",
        "div",
        "ul",
        "ol",
        "li");
}
