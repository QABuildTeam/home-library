using System.Net;
using System.Text.RegularExpressions;

namespace HomeLibrary.Web.Tests.Pages;

/// <summary>
/// Checks the scripts and stylesheets referenced by the pages: which pages load jQuery, in what order,
/// and that every local file referenced by the page markup exists. Files that TinyMCE loads at runtime
/// (theme, model, icons, plugins, skins) and source maps are not covered.
/// </summary>
[Collection(WebCollection.NAME)]
public sealed partial class PageAssetsTests(HomeLibraryWebApplicationFactory factory)
{
    private const string LIST_URL = "/";
    private const string CREATE_URL = "/books/create";

    // URLs are fingerprinted by MapStaticAssets (jquery.min.<hash>.js), so only the stable part is matched.
    private const string JQUERY = "/lib/jquery/dist/jquery.min.";
    private const string JQUERY_FOLDER = "/lib/jquery/";
    private const string VALIDATE = "/lib/jquery-validation/dist/jquery.validate.min.";
    private const string UNOBTRUSIVE = "/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.";
    private const string TINYMCE = "/lib/tinymce/tinymce.min.";

    // src of <script> and href of <link> with a local path ("/..." but not protocol-relative "//...").
    private const string LOCAL_ASSET_PATTERN = @"<(?:script[^>]*\ssrc|link[^>]*\shref)=""(/[^""/][^""]*)""";
    private const int URL_GROUP = 1;

    [Fact]
    public async Task Get_BookList_DoesNotLoadJQuery()
    {
        var html = await factory.CreateClient().GetStringAsync(LIST_URL);

        Assert.DoesNotContain(JQUERY_FOLDER, html);
    }

    [Fact]
    public async Task Get_CreatePage_LoadsJQueryBeforeValidationAndEditor()
    {
        var html = await factory.CreateClient().GetStringAsync(CREATE_URL);

        int[] positions =
        [
            html.IndexOf(JQUERY, StringComparison.Ordinal),
            html.IndexOf(VALIDATE, StringComparison.Ordinal),
            html.IndexOf(UNOBTRUSIVE, StringComparison.Ordinal),
            html.IndexOf(TINYMCE, StringComparison.Ordinal)
        ];

        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.Order(), positions);
    }

    [Theory]
    [InlineData(LIST_URL)]
    [InlineData(CREATE_URL)]
    public async Task Get_Page_EveryLocalAssetInMarkupExists(string pageUrl)
    {
        var client = factory.CreateClient();
        var html = await client.GetStringAsync(pageUrl);

        var assetUrls = LocalAssetRegex()
            .Matches(html)
            .Select(match => match.Groups[URL_GROUP].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(assetUrls);

        var responses = await Task.WhenAll(assetUrls.Select(assetUrl => client.GetAsync(assetUrl)));

        // All missing files are reported at once, not only the first one.
        var missing = assetUrls
            .Zip(responses, (assetUrl, response) => (AssetUrl: assetUrl, response.StatusCode))
            .Where(asset => asset.StatusCode != HttpStatusCode.OK)
            .Select(asset => $"{asset.AssetUrl} returned {(int)asset.StatusCode}")
            .ToList();

        Assert.Empty(missing);
    }

    [GeneratedRegex(LOCAL_ASSET_PATTERN, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LocalAssetRegex();
}
