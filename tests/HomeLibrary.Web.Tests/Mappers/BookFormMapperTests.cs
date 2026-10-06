using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;
using HomeLibrary.Web.Mappers;
using HomeLibrary.Web.Models;

namespace HomeLibrary.Web.Tests.Mappers;

public sealed class BookFormMapperTests
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
    private const string TOC_HTML = "<ol><li>Chapter 1</li></ol>";

    private static readonly DateTimeOffset _timestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FromView_CopiesAllEditableAttributes()
    {
        var view = new BookView(
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
            _timestamp,
            _timestamp);

        var form = BookFormMapper.FromView(view);

        Assert.Equal(TITLE, form.Title);
        Assert.Equal(AUTHOR, form.Author);
        Assert.Equal(PUBLICATION_YEAR, form.PublicationYear);
        Assert.Equal(ISBN, form.Isbn);
        Assert.Equal(PUBLISHER, form.Publisher);
        Assert.Equal(PAGE_COUNT, form.PageCount);
        Assert.Equal(GENRE, form.Genre);
        Assert.Equal(NOTES, form.Notes);
        Assert.Equal(TOC_HTML, form.TableOfContentsHtml);
    }

    [Fact]
    public void ToInput_CopiesAllFormFields()
    {
        var form = new BookForm
        {
            Title = TITLE,
            Author = AUTHOR,
            PublicationYear = PUBLICATION_YEAR,
            Isbn = ISBN,
            Publisher = PUBLISHER,
            PageCount = PAGE_COUNT,
            Genre = GENRE,
            Notes = NOTES,
            TableOfContentsHtml = TOC_HTML
        };

        // Named arguments keep the expectation independent of the parameter order,
        // so a mapper that passes them by position fails after a reordering.
        var expected = new BookInput(
            Title: TITLE,
            Author: AUTHOR,
            PublicationYear: PUBLICATION_YEAR,
            Isbn: ISBN,
            Publisher: PUBLISHER,
            PageCount: PAGE_COUNT,
            Genre: GENRE,
            Notes: NOTES,
            TableOfContentsHtml: TOC_HTML);

        Assert.Equal(expected, BookFormMapper.ToInput(form));
    }

    [Theory]
    [InlineData(nameof(BookDetails.TableOfContents), nameof(BookForm.TableOfContentsHtml))]
    [InlineData(nameof(BookDetails.Title), nameof(BookForm.Title))]
    [InlineData(nameof(BookDetails.PublicationYear), nameof(BookForm.PublicationYear))]
    public void ToFormField_MapsDomainFieldToFormProperty(string domainField, string formField)
    {
        Assert.Equal(formField, BookFormMapper.ToFormField(domainField));
    }

    [Fact]
    public void ToFormField_EveryDomainFieldExistsInForm()
    {
        var formProperties = typeof(BookForm)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet();

        var missing = typeof(BookDetails)
            .GetProperties()
            .Select(property => BookFormMapper.ToFormField(property.Name))
            .Where(field => !formProperties.Contains(field))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void FromView_NullView_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookFormMapper.FromView(null!));
    }

    [Fact]
    public void ToInput_NullForm_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookFormMapper.ToInput(null!));
    }

    [Fact]
    public void ToFormField_NullField_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BookFormMapper.ToFormField(null!));
    }
}
