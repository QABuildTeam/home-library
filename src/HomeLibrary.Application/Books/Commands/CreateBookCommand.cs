namespace HomeLibrary.Application.Books.Commands;

/// <summary>
/// Adds a new book to the library.
/// </summary>
public sealed record CreateBookCommand(BookInput Input);
