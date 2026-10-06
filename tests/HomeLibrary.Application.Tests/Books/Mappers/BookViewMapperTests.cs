using HomeLibrary.Application.Books.Mappers;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Tests.Books.Mappers;

public sealed class BookViewMapperTests
{
    private const long BOOK_ID = 5L;
    private const int VERSION = 2;
    private const int PUBLICATION_YEAR = 1967;
    private const int PAGE_COUNT = 480;
    private const string TITLE = "The Master and Margarita";
    private const string AUTHOR = "Mikhail Bulgakov";
    private const string ISBN = "978-5-17-090630-7";
    private const string PUBLISHER = "AST";
    private const string GENRE = "Novel";
    private const string NOTES = "A gift";
    private const string TOC_XML = "<toc><p>Stored</p></toc>";
    private const string TOC_HTML = "<p>Converted</p>";

    private static readonly DateTimeOffset _createdAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _updatedAt = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToView_CopiesBookAndUsesGivenTableOfContentsHtml()
    {
        var details = new BookDetails(
            TITLE,
            AUTHOR,
            PUBLICATION_YEAR,
            ISBN,
            PUBLISHER,
            PAGE_COUNT,
            GENRE,
            NOTES,
            new TableOfContents(TOC_XML));

        var view = BookViewMapper.ToView(new Book(BOOK_ID, details, VERSION, _createdAt, _updatedAt), TOC_HTML);

        var expected = new BookView(
            BOOK_ID,
            TITLE,
            AUTHOR,
            PUBLICATION_YEAR,
            ISBN,
            PUBLISHER,
            PAGE_COUNT,
            GENRE,
            NOTES,
            TOC_HTML,
            VERSION,
            _createdAt,
            _updatedAt);

        Assert.Equal(expected, view);
    }

    [Fact]
    public void ToView_WithoutTableOfContentsHtml_LeavesItNull()
    {
        var details = new BookDetails(TITLE, AUTHOR, null, null, null, null, null, null, null);

        var view = BookViewMapper.ToView(new Book(BOOK_ID, details, VERSION, _createdAt, _updatedAt), tableOfContentsHtml: null);

        Assert.Null(view.TableOfContentsHtml);
    }

    [Fact]
    public void ToView_NullBook_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookViewMapper.ToView(null!, TOC_HTML));
    }
}
