using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Ports;

/// <summary>
/// Creates, changes and deletes books in the storage.
/// </summary>
public interface IBookWriteRepository
{
    /// <summary>
    /// Adds a new book.
    /// </summary>
    /// <returns>Identifier of the new book.</returns>
    Task<long> Add(BookDetails details, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the attributes of an existing book.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    /// <param name="expectedVersion">Version of the book that the user edited.</param>
    /// <param name="details">New attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="BookNotFoundException">The book does not exist.</exception>
    /// <exception cref="BookConcurrencyException">The book was changed after the user loaded it.</exception>
    Task Update(long id, int expectedVersion, BookDetails details, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a book.
    /// </summary>
    /// <exception cref="BookNotFoundException">The book does not exist.</exception>
    Task Delete(long id, CancellationToken cancellationToken);
}
