namespace HomeLibrary.Domain.Books;

/// <summary>
/// Limits of the book attributes. The same limits are enforced by the database schema.
/// </summary>
public static class BookConstraints
{
    public const int TITLE_MAX_LENGTH = 500;
    public const int AUTHOR_MAX_LENGTH = 300;
    public const int ISBN_MAX_LENGTH = 20;
    public const int PUBLISHER_MAX_LENGTH = 300;
    public const int GENRE_MAX_LENGTH = 100;
    public const int NOTES_MAX_LENGTH = 4000;

    public const int MIN_PUBLICATION_YEAR = 1;
    public const int MIN_PAGE_COUNT = 1;
    public const int MAX_PAGE_COUNT = 100_000;

    public const int ISBN10_LENGTH = 10;
    public const int ISBN13_LENGTH = 13;
}
