using HomeLibrary.Domain.Books;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Row returned by the <c>book_get</c> function.
/// </summary>
internal sealed class BookRow
{
    public long Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public int? PublicationYear { get; init; }

    public string? Isbn { get; init; }

    public string? Publisher { get; init; }

    public int? PageCount { get; init; }

    public string? Genre { get; init; }

    public string? Notes { get; init; }

    public string? Toc { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int Version { get; init; }

    public Book ToBook()
    {
        var details = new BookDetails(
            Title,
            Author,
            PublicationYear,
            Isbn,
            Publisher,
            PageCount,
            Genre,
            Notes,
            Toc is null ? null : new TableOfContents(Toc));

        return new Book(Id, details, Version, ToUtcOffset(CreatedAt), ToUtcOffset(UpdatedAt));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
