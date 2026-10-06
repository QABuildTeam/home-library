using HomeLibrary.Application.Books.Models;

namespace HomeLibrary.Application.Books.UseCases.SearchBooks;

/// <summary>
/// Finds books by a substring of the title, the author or the table of contents.
/// </summary>
/// <param name="Text">Search text; empty text returns all books.</param>
/// <param name="Scope">Attributes to search in; <see cref="BookSearchScope.None"/> means all attributes.</param>
/// <param name="Page">Page number, starting from 1.</param>
/// <param name="PageSize">Maximum number of books on a page.</param>
public sealed record SearchBooksQuery(string? Text, BookSearchScope Scope, int Page, int PageSize);
