using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Domain.Books;

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

        return ToView(book);
    }

    private BookView ToView(Book book)
    {
        var details = book.Details;

        return new BookView(
            Id: book.Id,
            Title: details.Title,
            Author: details.Author,
            PublicationYear: details.PublicationYear,
            Isbn: details.Isbn,
            Publisher: details.Publisher,
            PageCount: details.PageCount,
            Genre: details.Genre,
            Notes: details.Notes,
            TableOfContentsHtml: details.TableOfContents is null ? null : tableOfContentsConverter.ToHtml(details.TableOfContents),
            Version: book.Version,
            CreatedAt: book.CreatedAt,
            UpdatedAt: book.UpdatedAt);
    }
}
