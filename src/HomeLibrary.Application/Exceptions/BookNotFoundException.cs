namespace HomeLibrary.Application.Exceptions;

/// <summary>
/// Thrown when the requested book does not exist.
/// </summary>
/// <param name="bookId">Identifier of the missing book.</param>
public sealed class BookNotFoundException(long bookId) : Exception($"Book {bookId} was not found.")
{
    public long BookId { get; } = bookId;
}
