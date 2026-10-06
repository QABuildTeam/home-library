using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Ports;

/// <summary>
/// Converts the table of contents between the HTML used by the editor and the XML stored in the database.
/// </summary>
public interface ITableOfContentsConverter
{
    /// <summary>
    /// Sanitizes the editor HTML and converts it to XML.
    /// </summary>
    /// <param name="html">HTML produced by the editor.</param>
    /// <returns>Table of contents, or <c>null</c> when the HTML contains no text.</returns>
    TableOfContents? FromHtml(string? html);

    /// <summary>
    /// Converts the stored XML back to sanitized HTML suitable for the editor and for display.
    /// </summary>
    /// <param name="tableOfContents">Stored table of contents.</param>
    string ToHtml(TableOfContents tableOfContents);
}
