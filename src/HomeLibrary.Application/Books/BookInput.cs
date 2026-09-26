namespace HomeLibrary.Application.Books;

/// <summary>
/// Book attributes as entered by the user, before normalization and validation.
/// </summary>
/// <param name="TableOfContentsHtml">Table of contents as HTML produced by the editor.</param>
public sealed record BookInput(
    string? Title,
    string? Author,
    int? PublicationYear,
    string? Isbn,
    string? Publisher,
    int? PageCount,
    string? Genre,
    string? Notes,
    string? TableOfContentsHtml);
