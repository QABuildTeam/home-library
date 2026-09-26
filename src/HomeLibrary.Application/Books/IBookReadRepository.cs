using HomeLibrary.Application.Common;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books;

/// <summary>
/// Reads books from the storage.
/// </summary>
public interface IBookReadRepository
{
    /// <summary>
    /// Gets a book by its identifier.
    /// </summary>
    /// <returns>The book, or <c>null</c> when it does not exist.</returns>
    Task<Book?> Get(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds books matching the search criteria, ordered by title.
    /// </summary>
    Task<PagedResult<BookListItem>> Search(BookSearchCriteria criteria, CancellationToken cancellationToken);
}
