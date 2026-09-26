using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books;

/// <summary>
/// Builds normalized book attributes from the user input.
/// </summary>
/// <param name="tableOfContentsConverter">Converts the editor HTML to the stored XML.</param>
public sealed class BookDetailsFactory(ITableOfContentsConverter tableOfContentsConverter) : IBookDetailsFactory
{
    public BookDetails Create(BookInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new BookDetails(
            Title: input.Title?.Trim() ?? string.Empty,
            Author: input.Author?.Trim() ?? string.Empty,
            PublicationYear: input.PublicationYear,
            Isbn: TrimToNull(input.Isbn),
            Publisher: TrimToNull(input.Publisher),
            PageCount: input.PageCount,
            Genre: TrimToNull(input.Genre),
            Notes: TrimToNull(input.Notes),
            TableOfContents: tableOfContentsConverter.FromHtml(input.TableOfContentsHtml));
    }

    private static string? TrimToNull(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
