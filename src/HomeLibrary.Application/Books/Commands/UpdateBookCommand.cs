namespace HomeLibrary.Application.Books.Commands;

/// <summary>
/// Replaces the attributes of an existing book.
/// </summary>
/// <param name="Id">Book identifier.</param>
/// <param name="Version">Version of the book that the user edited.</param>
/// <param name="Input">New attributes.</param>
public sealed record UpdateBookCommand(long Id, int Version, BookInput Input);
