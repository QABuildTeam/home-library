using System.Globalization;
using System.Net;
using HomeLibrary.Application.Books;
using HomeLibrary.Domain.Books;
using HomeLibrary.Web.Pages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HomeLibrary.Web.Tests.Pages;

/// <summary>
/// Every test uses a unique marker in the book titles, so tests do not see each other's books.
/// </summary>
[Collection(WebCollection.NAME)]
public sealed class IndexPageTests(HomeLibraryWebApplicationFactory factory)
{
    private const string MARKER_FORMAT = "N";
    private const string TITLE_NUMBER_FORMAT = "D2";
    private const int SECOND_PAGE = 2;
    private const int FAR_PAGE = 50;

    [Fact]
    public async Task Get_SecondPage_ShowsBooksAfterTheFirstPage()
    {
        var marker = NewMarker();

        await AddBooks(marker, IndexModel.PAGE_SIZE + 1);

        var html = await factory.CreateClient().GetStringAsync(ListUrl(marker, SECOND_PAGE));

        Assert.Contains(Title(marker, IndexModel.PAGE_SIZE + 1), html);
        Assert.DoesNotContain(Title(marker, 1), html);
        Assert.Contains($"Page {SECOND_PAGE} of {SECOND_PAGE}", html);
    }

    [Fact]
    public async Task Get_PageBeyondTheEnd_RedirectsToFirstPage()
    {
        var marker = NewMarker();

        await AddBooks(marker, 1);

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(ListUrl(marker, FAR_PAGE));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"{IndexModel.PAGE_PARAMETER}=1", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_SearchWithoutMatches_ShowsNoMatchesMessage()
    {
        var html = await factory.CreateClient().GetStringAsync(ListUrl(NewMarker(), 1));

        Assert.Contains("No books match the search.", html);
    }

    private async Task AddBooks(string marker, int count)
    {
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IBookWriteRepository>();

        // foreach, not LINQ: every insert is awaited.
        foreach (var number in Enumerable.Range(1, count))
        {
            var details = new BookDetails(Title(marker, number), "Author", null, null, null, null, null, null, null);

            await repository.Add(details, CancellationToken.None);
        }
    }

    private static string ListUrl(string marker, int page) =>
        $"/?{IndexModel.QUERY_PARAMETER}={marker}&{IndexModel.SCOPE_PARAMETER}={BookSearchScope.Title}"
        + $"&{IndexModel.PAGE_PARAMETER}={page.ToString(CultureInfo.InvariantCulture)}";

    private static string Title(string marker, int number) =>
        $"{marker} {number.ToString(TITLE_NUMBER_FORMAT, CultureInfo.InvariantCulture)}";

    private static string NewMarker() => Guid.NewGuid().ToString(MARKER_FORMAT);
}
