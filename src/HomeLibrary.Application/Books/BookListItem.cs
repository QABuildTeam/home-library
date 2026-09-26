namespace HomeLibrary.Application.Books;

/// <summary>
/// Short book description shown in the book list.
/// </summary>
public sealed record BookListItem(long Id, string Title, string Author, int? PublicationYear, string? Genre);
