using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Queries;
using HomeLibrary.Application.Exceptions;
using HomeLibrary.Domain.Books;
using NSubstitute;

namespace HomeLibrary.Application.Tests.Books.Queries;

public sealed class ExportTableOfContentsHandlerTests
{
    private const long BOOK_ID = 12L;
    private const int VERSION = 1;

    private static readonly DateTimeOffset _timestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IBookReadRepository _repository = Substitute.For<IBookReadRepository>();
    private readonly ITableOfContentsFileBuilder _fileBuilder = Substitute.For<ITableOfContentsFileBuilder>();

    [Fact]
    public async Task Handle_BookWithTableOfContents_ReturnsBuiltFile()
    {
        var toc = new TableOfContents("<toc><ol><li>Chapter 1</li></ol></toc>");
        var book = StubBook(new BookDetails("Title", "Author", null, null, null, null, null, null, toc));
        var expectedFile = new TableOfContentsFile("book-12-toc.xml", "application/xml", [1]);

        _fileBuilder.Build(book, toc).Returns(expectedFile);

        var file = await CreateHandler().Handle(new ExportTableOfContentsQuery(BOOK_ID), CancellationToken.None);

        Assert.Same(expectedFile, file);
    }

    [Fact]
    public async Task Handle_BookWithoutTableOfContents_ThrowsTableOfContentsNotFound()
    {
        StubBook(new BookDetails("Title", "Author", null, null, null, null, null, null, null));

        await Assert.ThrowsAsync<TableOfContentsNotFoundException>(
            () => CreateHandler().Handle(new ExportTableOfContentsQuery(BOOK_ID), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingBook_ThrowsBookNotFound()
    {
        _repository.Get(BOOK_ID, Arg.Any<CancellationToken>()).Returns((Book?)null);

        await Assert.ThrowsAsync<BookNotFoundException>(
            () => CreateHandler().Handle(new ExportTableOfContentsQuery(BOOK_ID), CancellationToken.None));
    }

    private ExportTableOfContentsHandler CreateHandler() => new(_repository, _fileBuilder);

    private Book StubBook(BookDetails details)
    {
        var book = new Book(BOOK_ID, details, VERSION, _timestamp, _timestamp);

        _repository.Get(BOOK_ID, Arg.Any<CancellationToken>()).Returns(book);

        return book;
    }
}
