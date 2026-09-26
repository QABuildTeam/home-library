using HomeLibrary.Domain.Books;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

/// <summary>
/// Builds book attributes for the database tests.
/// </summary>
internal static class BookSamples
{
    private const string DEFAULT_AUTHOR = "Test Author";

    public static BookDetails Create(string title, string author = DEFAULT_AUTHOR, string? tableOfContentsXml = null) => new(
        Title: title,
        Author: author,
        PublicationYear: null,
        Isbn: null,
        Publisher: null,
        PageCount: null,
        Genre: null,
        Notes: null,
        TableOfContents: tableOfContentsXml is null ? null : new TableOfContents(tableOfContentsXml));
}
