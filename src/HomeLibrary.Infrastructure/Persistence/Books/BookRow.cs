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
}
