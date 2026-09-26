using System.Globalization;
using System.Net;
using HomeLibrary.Application.Books;
using HomeLibrary.Domain.Books;
using Microsoft.Extensions.DependencyInjection;

namespace HomeLibrary.Web.Tests.Pages;

[Collection(WebCollection.NAME)]
public sealed class BookPagesTests(HomeLibraryWebApplicationFactory factory)
{
    private const string MISSING_BOOK_URL = "/books/9223372036854775807";
    private const string NOT_FOUND_TEXT = "does not exist";

    [Theory]
    [InlineData(MISSING_BOOK_URL)]
    [InlineData(MISSING_BOOK_URL + "/edit")]
    [InlineData(MISSING_BOOK_URL + "/delete")]
    public async Task Get_MissingBook_ReturnsFriendlyNotFoundPage(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(NOT_FOUND_TEXT, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExportToc_BookWithoutTableOfContents_ReturnsNotFound()
    {
        var id = await AddBook(tableOfContents: null);

        var response = await factory.CreateClient().GetAsync(ExportUrl(id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportToc_BookWithTableOfContents_ReturnsXmlFile()
    {
        var id = await AddBook(new TableOfContents("<toc><p>Chapter 1</p></toc>"));

        var response = await factory.CreateClient().GetAsync(ExportUrl(id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<p>Chapter 1</p>", await response.Content.ReadAsStringAsync());
    }

    private async Task<long> AddBook(TableOfContents? tableOfContents)
    {
        using var scope = factory.Services.CreateScope();

        var details = new BookDetails("Export", "Author", null, null, null, null, null, null, tableOfContents);

        return await scope.ServiceProvider.GetRequiredService<IBookWriteRepository>().Add(details, CancellationToken.None);
    }

    private static string ExportUrl(long id) => $"/books/{id.ToString(CultureInfo.InvariantCulture)}?handler=ExportToc";
}
