namespace HomeLibrary.Application.Books;

/// <summary>
/// Normalized search parameters passed to the storage.
/// </summary>
/// <param name="Text">Substring to search for (case-insensitive); <c>null</c> returns all books.</param>
/// <param name="Scope">Attributes to search in; never <see cref="BookSearchScope.None"/>.</param>
/// <param name="Page">Page number, starting from 1.</param>
/// <param name="PageSize">Maximum number of books on a page.</param>
public sealed record BookSearchCriteria(string? Text, BookSearchScope Scope, int Page, int PageSize)
{
    public int Offset => (Page - 1) * PageSize;
}
