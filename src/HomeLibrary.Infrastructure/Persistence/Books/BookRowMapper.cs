using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Converts rows returned by the book stored functions into domain objects and application models.
/// </summary>
internal static class BookRowMapper
{
    /// <summary>
    /// Converts a <c>book_get</c> row into a book. Timestamps read from <c>timestamptz</c> columns are UTC.
    /// </summary>
    public static Book ToBook(BookRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var details = new BookDetails(
            row.Title,
            row.Author,
            row.PublicationYear,
            row.Isbn,
            row.Publisher,
            row.PageCount,
            row.Genre,
            row.Notes,
            row.Toc is null ? null : new TableOfContents(row.Toc));

        return new Book(row.Id, details, row.Version, ToUtcOffset(row.CreatedAt), ToUtcOffset(row.UpdatedAt));
    }

    /// <summary>
    /// Converts a <c>book_search</c> row into a list item; the total count of the row is not part of the item.
    /// </summary>
    public static BookListItem ToListItem(BookListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new BookListItem(row.Id, row.Title, row.Author, row.PublicationYear, row.Genre);
    }

    // timestamptz values are UTC. new DateTimeOffset(DateTime) would apply the local offset to a value with
    // DateTimeKind.Unspecified, so the kind is set explicitly to get a zero offset for any input.
    private static DateTimeOffset ToUtcOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
