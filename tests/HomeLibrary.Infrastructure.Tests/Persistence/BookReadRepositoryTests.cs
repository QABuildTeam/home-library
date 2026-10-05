using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

/// <summary>
/// Every test uses a unique marker in the searched text, so tests do not see each other's books.
/// </summary>
[Collection(DatabaseCollection.NAME)]
public sealed class BookReadRepositoryTests(DatabaseFixture fixture) : IDisposable
{
    private const int PAGE_SIZE = 10;
    private const int SECOND_PAGE = 2;
    private const int SMALL_PAGE_SIZE = 2;
    private const int PAGED_BOOK_COUNT = 3;
    private const string MARKER_FORMAT = "N";

    // Cyrillic text is the subject of the test: ILIKE must ignore the case of non-ASCII letters.
    private const string CYRILLIC_TITLE_UPPER_CASE = "Война и МИР";
    private const string CYRILLIC_TITLE_LOWER_CASE = "война и мир";

    private readonly IServiceScope _scope = fixture.Services.CreateScope();

    private IBookWriteRepository Writer => _scope.ServiceProvider.GetRequiredService<IBookWriteRepository>();

    private IBookReadRepository Reader => _scope.ServiceProvider.GetRequiredService<IBookReadRepository>();

    public void Dispose() => _scope.Dispose();

    [Fact]
    public async Task Get_MissingBook_ReturnsNull()
    {
        Assert.Null(await Reader.Get(long.MaxValue, CancellationToken.None));
    }

    [Fact]
    public async Task Search_ByTitle_IsCaseInsensitiveForCyrillic()
    {
        var marker = NewMarker();
        var id = await Writer.Add(BookSamples.Create($"{CYRILLIC_TITLE_UPPER_CASE} {marker}"), CancellationToken.None);

        var result = await Search($"{CYRILLIC_TITLE_LOWER_CASE} {marker}", BookSearchScope.Title);

        Assert.Equal(id, Assert.Single(result.Items).Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Search_ByAuthor_FindsBook()
    {
        var marker = NewMarker();
        var id = await Writer.Add(BookSamples.Create("Anna Karenina", author: $"Leo TOLSTOY {marker}"), CancellationToken.None);

        var result = await Search($"tolstoy {marker}", BookSearchScope.Author);

        Assert.Equal(id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Search_ByTableOfContents_FindsTextInsideNestedElements()
    {
        var marker = NewMarker();
        var toc = $"<toc><ol><li>Part one<ol><li>Chapter {marker} &amp; epilogue</li></ol></li></ol></toc>";
        var id = await Writer.Add(BookSamples.Create("Nested", tableOfContentsXml: toc), CancellationToken.None);

        var result = await Search($"chapter {marker} & epilogue", BookSearchScope.TableOfContents);

        Assert.Equal(id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Search_ByTableOfContents_FindsPhraseAcrossFormatting()
    {
        var marker = NewMarker();
        var toc = $"<toc><p><strong>Chapter</strong> {marker} <em>begins</em></p></toc>";
        var id = await Writer.Add(BookSamples.Create("Phrase", tableOfContentsXml: toc), CancellationToken.None);

        var result = await Search($"chapter {marker}  begins", BookSearchScope.TableOfContents);

        Assert.Equal(id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Search_ByTableOfContents_KeepsNeighbouringBlocksAsSeparateWords()
    {
        var marker = NewMarker();
        var toc = $"<toc><ol><li>Part{marker}<ol><li>Chapter</li></ol></li></ol></toc>";
        await Writer.Add(BookSamples.Create("Blocks", tableOfContentsXml: toc), CancellationToken.None);

        var result = await Search($"Part{marker}Chapter", BookSearchScope.TableOfContents);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_OutsideSelectedScope_FindsNothing()
    {
        var marker = NewMarker();
        await Writer.Add(BookSamples.Create("Scope", tableOfContentsXml: $"<toc><p>{marker}</p></toc>"), CancellationToken.None);

        var result = await Search(marker, BookSearchScope.Title | BookSearchScope.Author);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData("100%", "1000")]
    [InlineData("a_b", "axb")]
    [InlineData("c\\d", "c\\\\d")]
    public async Task Search_TreatsLikeSpecialCharactersLiterally(string expected, string similar)
    {
        var marker = NewMarker();
        var expectedId = await Writer.Add(BookSamples.Create($"{expected} {marker}"), CancellationToken.None);
        await Writer.Add(BookSamples.Create($"{similar} {marker}"), CancellationToken.None);

        var result = await Search($"{expected} {marker}", BookSearchScope.Title);

        Assert.Equal(expectedId, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Search_ReturnsRequestedPageWithTotalCount()
    {
        var marker = NewMarker();

        // foreach, not LINQ: every insert is awaited.
        foreach (var letter in "ABC")
        {
            await Writer.Add(BookSamples.Create($"{letter} {marker}"), CancellationToken.None);
        }

        var criteria = new BookSearchCriteria(marker, BookSearchScope.Title, SECOND_PAGE, SMALL_PAGE_SIZE);
        var result = await Reader.Search(criteria, CancellationToken.None);

        Assert.Equal($"C {marker}", Assert.Single(result.Items).Title);
        Assert.Equal(PAGED_BOOK_COUNT, result.TotalCount);
        Assert.Equal(SECOND_PAGE, result.TotalPages);
    }

    private Task<PagedResult<BookListItem>> Search(string text, BookSearchScope scope) =>
        Reader.Search(new BookSearchCriteria(text, scope, Page: 1, PAGE_SIZE), CancellationToken.None);

    private static string NewMarker() => Guid.NewGuid().ToString(MARKER_FORMAT);
}
