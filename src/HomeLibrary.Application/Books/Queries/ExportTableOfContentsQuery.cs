namespace HomeLibrary.Application.Books.Queries;

/// <summary>
/// Exports the table of contents of a book to an XML file.
/// </summary>
public sealed record ExportTableOfContentsQuery(long BookId);
