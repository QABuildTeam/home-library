using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.UseCases.GetBook;
using HomeLibrary.Domain.Books;
using NSubstitute;

namespace HomeLibrary.Application.Tests.Books.UseCases.GetBook;

public sealed class GetBookHandlerTests
{
    private const long BOOK_ID = 5L;
    private const int VERSION = 2;
    private const int PUBLICATION_YEAR = 2000;
    private const int PAGE_COUNT = 10;

    private readonly IBookReadRepository _repository = Substitute.For<IBookReadRepository>();
    private readonly ITableOfContentsConverter _converter = Substitute.For<ITableOfContentsConverter>();

    [Fact]
    public async Task Handle_ExistingBook_ReturnsViewWithHtmlTableOfContents()
    {
        var toc = new TableOfContents("<toc><p>One</p></toc>");
        var details = new BookDetails("Title", "Author", PUBLICATION_YEAR, "5170906307", "Publisher", PAGE_COUNT, "Genre", "Notes", toc);
        var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        _repository.Get(BOOK_ID, Arg.Any<CancellationToken>()).Returns(new Book(BOOK_ID, details, VERSION, timestamp, timestamp));
        _converter.ToHtml(toc).Returns("<p>One</p>");

        var view = await new GetBookHandler(_repository, _converter).Handle(new GetBookQuery(BOOK_ID), CancellationToken.None);

        var expected = new BookView(
            BOOK_ID,
            "Title",
            "Author",
            PUBLICATION_YEAR,
            "5170906307",
            "Publisher",
            PAGE_COUNT,
            "Genre",
            "Notes",
            "<p>One</p>",
            VERSION,
            timestamp,
            timestamp);

        Assert.Equal(expected, view);
    }

    [Fact]
    public async Task Handle_MissingBook_ThrowsNotFound()
    {
        _repository.Get(BOOK_ID, Arg.Any<CancellationToken>()).Returns((Book?)null);

        var exception = await Assert.ThrowsAsync<BookNotFoundException>(
            () => new GetBookHandler(_repository, _converter).Handle(new GetBookQuery(BOOK_ID), CancellationToken.None));

        Assert.Equal(BOOK_ID, exception.BookId);
    }
}
