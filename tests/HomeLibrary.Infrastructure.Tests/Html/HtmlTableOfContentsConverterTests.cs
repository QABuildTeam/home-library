using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;
using HomeLibrary.Infrastructure.Html;

namespace HomeLibrary.Infrastructure.Tests.Html;

public sealed class HtmlTableOfContentsConverterTests
{
    private readonly HtmlTableOfContentsConverter _converter = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p><br></p>")]
    [InlineData("<p>&nbsp;</p>")]
    public void FromHtml_WithoutText_ReturnsNull(string? html)
    {
        Assert.Null(_converter.FromHtml(html));
    }

    [Fact]
    public void FromHtml_RemovesScriptsAndAttributes()
    {
        var toc = _converter.FromHtml("<p onclick=\"alert(1)\" style=\"color:red\">Intro<script>alert(2)</script></p>");

        Assert.Equal("<toc><p>Intro</p></toc>", toc?.Xml);
    }

    [Fact]
    public void FromHtml_UnwrapsUnsupportedElementsButKeepsTheirText()
    {
        var toc = _converter.FromHtml("<p><a href=\"https://example.com\">Link<style>p{}</style></a> text</p>");

        Assert.Equal("<toc><p>Link text</p></toc>", toc?.Xml);
    }

    [Fact]
    public void FromHtml_KeepsNestedListsAsWellFormedXml()
    {
        var toc = _converter.FromHtml("<ol><li>Part 1<ol><li>Chapter 1<li>Chapter 2</ol></li></ol>");

        Assert.Equal("<toc><ol><li>Part 1<ol><li>Chapter 1</li><li>Chapter 2</li></ol></li></ol></toc>", toc?.Xml);
    }

    [Fact]
    public void FromHtml_ConvertsHtmlEntitiesAndVoidElementsToXml()
    {
        var toc = _converter.FromHtml("<p>Tom&nbsp;&amp;&nbsp;Jerry<br>&copy; 2024</p>");

        Assert.Equal("<toc><p>Tom &amp; Jerry<br />© 2024</p></toc>", toc?.Xml);
    }

    [Fact]
    public void FromHtml_PreservesSpaceBetweenInlineElements()
    {
        const string HTML = "<p><strong>Chapter 1.</strong> <em>Introduction</em></p>";

        var toc = _converter.FromHtml(HTML);

        Assert.NotNull(toc);
        Assert.Equal($"<toc>{HTML}</toc>", toc.Xml);
        Assert.Equal(HTML, _converter.ToHtml(toc));
    }

    [Fact]
    public void FromHtml_CharacterNotAllowedInXml_ThrowsValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(() => _converter.FromHtml("<p>Bell &#7; character</p>"));

        Assert.Contains(nameof(BookDetails.TableOfContents), exception.Errors.Keys);
    }

    [Fact]
    public void ToHtml_ReturnsInnerContentOfRootElement()
    {
        var html = _converter.ToHtml(new TableOfContents("<toc><h2>Contents</h2><ul><li>One</li><li>Two<br /></li></ul></toc>"));

        Assert.Equal("<h2>Contents</h2><ul><li>One</li><li>Two<br></li></ul>", html);
    }

    [Fact]
    public void ToHtml_SanitizesXmlChangedOutsideTheApplication()
    {
        var html = _converter.ToHtml(new TableOfContents("<toc><script>alert(1)</script><p>Safe</p></toc>"));

        Assert.Equal("<p>Safe</p>", html);
    }

    [Fact]
    public void FromHtml_ThenToHtml_PreservesStructure()
    {
        const string HTML = "<h2>Part 1</h2><ol><li>Chapter 1</li><li>Chapter 2</li></ol>";

        var toc = _converter.FromHtml(HTML);

        Assert.NotNull(toc);
        Assert.Equal(HTML, _converter.ToHtml(toc));
    }
}
