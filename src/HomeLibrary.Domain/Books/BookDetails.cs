namespace HomeLibrary.Domain.Books;

/// <summary>
/// Descriptive attributes of a book: everything except identity and audit data.
/// </summary>
public sealed record BookDetails(
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    string? Publisher,
    int? PageCount,
    string? Genre,
    string? Notes,
    TableOfContents? TableOfContents);
