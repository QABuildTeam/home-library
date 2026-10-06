using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;
using HomeLibrary.Infrastructure.Persistence.Books;

namespace HomeLibrary.Infrastructure.Tests.Persistence.Books;

public sealed class BookRowMapperTests
{
    private const long BOOK_ID = 17L;
    private const int VERSION = 3;
    private const int PUBLICATION_YEAR = 1967;
    private const int PAGE_COUNT = 480;
    private const long TOTAL_COUNT = 42L;
    private const string TITLE = "The Master and Margarita";
    private const string AUTHOR = "Mikhail Bulgakov";
    private const string ISBN = "978-5-17-090630-7";
    private const string PUBLISHER = "AST";
    private const string GENRE = "Novel";
    private const string NOTES = "A gift";
    private const string TOC_XML = "<toc><p><strong>Chapter 1.</strong> <em>Introduction</em></p></toc>";

    private static readonly DateTime _createdAt = new(2026, 1, 2, 3, 4, 5);
    private static readonly DateTime _updatedAt = new(2026, 2, 3, 4, 5, 6);

    [Fact]
    public void ToBook_MapsAllColumns()
    {
        var book = BookRowMapper.ToBook(CreateRow(TOC_XML, DateTimeKind.Utc));

        var expectedDetails = new BookDetails(
            TITLE,
            AUTHOR,
            PUBLICATION_YEAR,
            ISBN,
            PUBLISHER,
            PAGE_COUNT,
            GENRE,
            NOTES,
            new TableOfContents(TOC_XML));

        Assert.Equal(BOOK_ID, book.Id);
        Assert.Equal(VERSION, book.Version);
        Assert.Equal(expectedDetails, book.Details);
    }

    /// <summary>
    /// The timestamps must be UTC with a zero offset for any DateTime.Kind: the book card prints them with a "UTC" label.
    /// Note: DateTimeOffset equality ignores the offset, so the offset is checked separately. On a machine whose
    /// local time zone is UTC a lost DateTime.SpecifyKind cannot be detected by this test.
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ToBook_ReturnsTimestampsInUtc(DateTimeKind kind)
    {
        var book = BookRowMapper.ToBook(CreateRow(TOC_XML, kind));

        Assert.Equal(_createdAt.Ticks, book.CreatedAt.UtcTicks);
        Assert.Equal(TimeSpan.Zero, book.CreatedAt.Offset);
        Assert.Equal(_updatedAt.Ticks, book.UpdatedAt.UtcTicks);
        Assert.Equal(TimeSpan.Zero, book.UpdatedAt.Offset);
    }

    [Fact]
    public void ToBook_WithoutTableOfContents_LeavesItNull()
    {
        var book = BookRowMapper.ToBook(CreateRow(toc: null, DateTimeKind.Utc));

        Assert.Null(book.Details.TableOfContents);
    }

    [Fact]
    public void ToListItem_MapsColumnsExceptTotalCount()
    {
        var row = new BookListRow
        {
            Id = BOOK_ID,
            Title = TITLE,
            Author = AUTHOR,
            PublicationYear = PUBLICATION_YEAR,
            Genre = GENRE,
            TotalCount = TOTAL_COUNT
        };

        Assert.Equal(new BookListItem(BOOK_ID, TITLE, AUTHOR, PUBLICATION_YEAR, GENRE), BookRowMapper.ToListItem(row));
    }

    [Fact]
    public void ToBook_NullRow_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookRowMapper.ToBook(null!));
    }

    [Fact]
    public void ToListItem_NullRow_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookRowMapper.ToListItem(null!));
    }

    private static BookRow CreateRow(string? toc, DateTimeKind kind) => new()
    {
        Id = BOOK_ID,
        Title = TITLE,
        Author = AUTHOR,
        PublicationYear = PUBLICATION_YEAR,
        Isbn = ISBN,
        Publisher = PUBLISHER,
        PageCount = PAGE_COUNT,
        Genre = GENRE,
        Notes = NOTES,
        Toc = toc,
        CreatedAt = DateTime.SpecifyKind(_createdAt, kind),
        UpdatedAt = DateTime.SpecifyKind(_updatedAt, kind),
        Version = VERSION
    };
}
