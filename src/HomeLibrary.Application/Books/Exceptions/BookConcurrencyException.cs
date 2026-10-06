namespace HomeLibrary.Application.Books.Exceptions;

/// <summary>
/// Thrown when a book was changed by someone else after the user loaded it for editing.
/// </summary>
/// <param name="bookId">Identifier of the book.</param>
public sealed class BookConcurrencyException(long bookId)
    : Exception($"Book {bookId} was modified by another user. Reload the book and apply your changes again.")
{
    public long BookId { get; } = bookId;
}
