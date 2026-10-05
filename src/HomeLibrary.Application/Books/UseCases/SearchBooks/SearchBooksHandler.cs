using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Common;

namespace HomeLibrary.Application.Books.UseCases.SearchBooks;

/// <summary>
/// Normalizes the search parameters and finds books page by page.
/// </summary>
internal sealed class SearchBooksHandler(IBookReadRepository repository) : IQueryHandler<SearchBooksQuery, PagedResult<BookListItem>>
{
    public const int MAX_PAGE_SIZE = 100;

    /// <summary>
    /// Largest page number whose offset still fits into <see cref="int"/>.
    /// </summary>
    public const int MAX_PAGE = int.MaxValue / MAX_PAGE_SIZE;

    public Task<PagedResult<BookListItem>> Handle(SearchBooksQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var text = query.Text?.Trim();

        // Unknown flags (for example, a hand-edited URL) are ignored; no known flag means all attributes.
        var scope = query.Scope & BookSearchScope.All;

        var criteria = new BookSearchCriteria(
            Text: string.IsNullOrEmpty(text) ? null : text,
            Scope: scope == BookSearchScope.None ? BookSearchScope.All : scope,
            Page: Math.Clamp(query.Page, 1, MAX_PAGE),
            PageSize: Math.Clamp(query.PageSize, 1, MAX_PAGE_SIZE));

        return repository.Search(criteria, cancellationToken);
    }
}
