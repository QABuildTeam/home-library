using HomeLibrary.Application.Books.Models;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Row returned by the <c>book_search</c> function.
/// </summary>
internal sealed class BookListRow
{
    public long Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public int? PublicationYear { get; init; }

    public string? Genre { get; init; }

    /// <summary>
    /// Number of matching books on all pages.
    /// </summary>
    public long TotalCount { get; init; }

    public BookListItem ToListItem() => new(Id, Title, Author, PublicationYear, Genre);
}
