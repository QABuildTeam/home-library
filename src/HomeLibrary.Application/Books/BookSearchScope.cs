namespace HomeLibrary.Application.Books;

/// <summary>
/// Book attributes that the search text is matched against.
/// </summary>
[Flags]
public enum BookSearchScope
{
    None = 0,
    Title = 1,
    Author = 2,
    TableOfContents = 4,
    All = Title | Author | TableOfContents
}
