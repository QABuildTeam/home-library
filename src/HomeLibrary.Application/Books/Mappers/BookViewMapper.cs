using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Mappers;

/// <summary>
/// Converts a book into the model shown in the book card and the edit form.
/// </summary>
internal static class BookViewMapper
{
    /// <summary>
    /// Builds the view of a book.
    /// </summary>
    /// <param name="book">Book to show.</param>
    /// <param name="tableOfContentsHtml">
    /// Table of contents already converted to sanitized HTML, or <c>null</c> when the book has none.
    /// </param>
    public static BookView ToView(Book book, string? tableOfContentsHtml)
    {
        ArgumentNullException.ThrowIfNull(book);

        var details = book.Details;

        return new BookView(
            Id: book.Id,
            Title: details.Title,
            Author: details.Author,
            PublicationYear: details.PublicationYear,
            Isbn: details.Isbn,
            Publisher: details.Publisher,
            PageCount: details.PageCount,
            Genre: details.Genre,
            Notes: details.Notes,
            TableOfContentsHtml: tableOfContentsHtml,
            Version: book.Version,
            CreatedAt: book.CreatedAt,
            UpdatedAt: book.UpdatedAt);
    }
}
