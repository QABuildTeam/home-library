using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.Services;
using HomeLibrary.Domain.Books;
using NSubstitute;

namespace HomeLibrary.Application.Tests.Books.Services;

public sealed class BookDetailsFactoryTests
{
    private const int PUBLICATION_YEAR = 2001;
    private const int PAGE_COUNT = 100;

    private readonly ITableOfContentsConverter _converter = Substitute.For<ITableOfContentsConverter>();

    [Fact]
    public void Create_TrimsTextAndTurnsEmptyOptionalFieldsIntoNull()
    {
        var factory = new BookDetailsFactory(_converter);
        var input = new BookInput("  Title  ", " Author ", PUBLICATION_YEAR, "  ", " Publisher ", PAGE_COUNT, "", null, null);

        var details = factory.Create(input);

        Assert.Equal(new BookDetails("Title", "Author", PUBLICATION_YEAR, null, "Publisher", PAGE_COUNT, null, null, null), details);
    }

    [Fact]
    public void Create_MissingRequiredText_BecomesEmptyStringForValidation()
    {
        var details = new BookDetailsFactory(_converter).Create(new BookInput(null, null, null, null, null, null, null, null, null));

        Assert.Equal(string.Empty, details.Title);
        Assert.Equal(string.Empty, details.Author);
    }

    [Fact]
    public void Create_ConvertsTableOfContentsHtml()
    {
        var toc = new TableOfContents("<toc><p>One</p></toc>");
        _converter.FromHtml("<p>One</p>").Returns(toc);

        var details = new BookDetailsFactory(_converter).Create(new BookInput("T", "A", null, null, null, null, null, null, "<p>One</p>"));

        Assert.Same(toc, details.TableOfContents);
    }
}
