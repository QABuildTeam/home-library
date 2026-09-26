namespace HomeLibrary.Application.Books;

/// <summary>
/// Full book description shown in the book card and in the edit form.
/// </summary>
/// <param name="TableOfContentsHtml">Sanitized table of contents as HTML, or <c>null</c> when it is absent.</param>
/// <param name="Version">Row version to send back when saving changes.</param>
public sealed record BookView(
    long Id,
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    string? Publisher,
    int? PageCount,
    string? Genre,
    string? Notes,
    string? TableOfContentsHtml,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
