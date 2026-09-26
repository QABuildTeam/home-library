namespace HomeLibrary.Application.Books.Commands;

/// <summary>
/// Deletes a book from the library.
/// </summary>
public sealed record DeleteBookCommand(long Id);
