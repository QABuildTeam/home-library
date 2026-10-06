using HomeLibrary.Application.Books.Models;

namespace HomeLibrary.Application.Books.UseCases.CreateBook;

/// <summary>
/// Adds a new book to the library.
/// </summary>
public sealed record CreateBookCommand(BookInput Input);
