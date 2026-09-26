namespace HomeLibrary.Application.Exceptions;

/// <summary>
/// Thrown when the table of contents of an existing book is requested but the book has none.
/// </summary>
/// <param name="bookId">Identifier of the book.</param>
public sealed class TableOfContentsNotFoundException(long bookId) : Exception($"Book {bookId} has no table of contents.")
{
    public long BookId { get; } = bookId;
}
