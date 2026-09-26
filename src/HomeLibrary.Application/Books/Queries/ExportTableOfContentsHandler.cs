using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Exceptions;

namespace HomeLibrary.Application.Books.Queries;

/// <summary>
/// Exports the table of contents of a book to a file.
/// </summary>
public sealed class ExportTableOfContentsHandler(IBookReadRepository repository, ITableOfContentsFileBuilder fileBuilder)
    : IQueryHandler<ExportTableOfContentsQuery, TableOfContentsFile>
{
    public async Task<TableOfContentsFile> Handle(ExportTableOfContentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var book = await repository.Get(query.BookId, cancellationToken) ?? throw new BookNotFoundException(query.BookId);
        var tableOfContents = book.Details.TableOfContents ?? throw new TableOfContentsNotFoundException(query.BookId);

        return fileBuilder.Build(book, tableOfContents);
    }
}
