namespace HomeLibrary.Application.Books.UseCases.DeleteBook;

/// <summary>
/// Deletes a book from the library.
/// </summary>
public sealed record DeleteBookCommand(long Id);
