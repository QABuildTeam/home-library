using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Queries;
using HomeLibrary.Application.Common;
using NSubstitute;

namespace HomeLibrary.Application.Tests.Books.Queries;

public sealed class SearchBooksHandlerTests
{
    private const int PAGE_SIZE = 20;
    private const int THIRD_PAGE = 3;
    private const BookSearchScope UNKNOWN_SCOPE = (BookSearchScope)8;

    private readonly IBookReadRepository _repository = Substitute.For<IBookReadRepository>();

    public SearchBooksHandlerTests()
    {
        _repository
            .Search(Arg.Any<BookSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BookListItem>([], 0, 1, PAGE_SIZE));
    }

    [Fact]
    public async Task Handle_NormalizesTextScopeAndPaging()
    {
        await Handle(new SearchBooksQuery("  war  ", BookSearchScope.None, 0, 0));

        await _repository.Received(1).Search(
            new BookSearchCriteria("war", BookSearchScope.All, Page: 1, PageSize: 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankText_SearchesAllBooks()
    {
        await Handle(new SearchBooksQuery("   ", BookSearchScope.Title, THIRD_PAGE, PAGE_SIZE));

        await _repository.Received(1).Search(
            new BookSearchCriteria(null, BookSearchScope.Title, THIRD_PAGE, PAGE_SIZE),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TooLargePageSize_IsLimited()
    {
        const int TOO_LARGE_PAGE_SIZE = SearchBooksHandler.MAX_PAGE_SIZE + 1;

        await Handle(new SearchBooksQuery(null, BookSearchScope.All, 1, TOO_LARGE_PAGE_SIZE));

        await _repository.Received(1).Search(
            Arg.Is<BookSearchCriteria>(criteria => criteria.PageSize == SearchBooksHandler.MAX_PAGE_SIZE),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HugePageNumber_IsLimitedSoThatOffsetDoesNotOverflow()
    {
        await Handle(new SearchBooksQuery(null, BookSearchScope.All, int.MaxValue, SearchBooksHandler.MAX_PAGE_SIZE));

        await _repository.Received(1).Search(
            Arg.Is<BookSearchCriteria>(criteria => criteria.Page == SearchBooksHandler.MAX_PAGE && criteria.Offset >= 0),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(UNKNOWN_SCOPE, BookSearchScope.All)]
    [InlineData(UNKNOWN_SCOPE | BookSearchScope.Author, BookSearchScope.Author)]
    public async Task Handle_UnknownScopeFlags_AreIgnored(BookSearchScope requested, BookSearchScope expected)
    {
        await Handle(new SearchBooksQuery("text", requested, 1, PAGE_SIZE));

        await _repository.Received(1).Search(
            Arg.Is<BookSearchCriteria>(criteria => criteria.Scope == expected),
            Arg.Any<CancellationToken>());
    }

    private Task<PagedResult<BookListItem>> Handle(SearchBooksQuery query) =>
        new SearchBooksHandler(_repository).Handle(query, CancellationToken.None);
}
