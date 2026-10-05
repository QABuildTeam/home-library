using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Mappers;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;

namespace HomeLibrary.Application.Books.UseCases.GetBook;

/// <summary>
/// Gets the full description of a book with the table of contents converted to HTML.
/// </summary>
internal sealed class GetBookHandler(
    IBookReadRepository repository,
    ITableOfContentsConverter tableOfContentsConverter) : IQueryHandler<GetBookQuery, BookView>
{
    public async Task<BookView> Handle(GetBookQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var book = await repository.Get(query.Id, cancellationToken) ?? throw new BookNotFoundException(query.Id);

        var tableOfContents = book.Details.TableOfContents;
        var tableOfContentsHtml = tableOfContents is null ? null : tableOfContentsConverter.ToHtml(tableOfContents);

        return BookViewMapper.ToView(book, tableOfContentsHtml);
    }
}
