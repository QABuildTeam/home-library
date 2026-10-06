using HomeLibrary.Domain.Books;
using HomeLibrary.Infrastructure.Persistence.Books;

namespace HomeLibrary.Infrastructure.Tests.Persistence.Books;

public sealed class TableOfContentsSearchTextTests
{
    [Fact]
    public void Extract_NoTableOfContents_ReturnsNull()
    {
        Assert.Null(TableOfContentsSearchText.Extract(null));
    }

    [Theory]
    [InlineData("<toc><p>Intro<em>duction</em></p></toc>", "Introduction")]
    [InlineData("<toc><p><strong>Chapter</strong> 3</p></toc>", "Chapter 3")]
    [InlineData("<toc><p>A<b>B</b><i>C</i><u>D</u><s>E</s><span>F</span><strong>G</strong><em>H</em></p></toc>", "ABCDEFGH")]
    [InlineData("<toc><ol><li>Part 1<ol><li>Chapter 1</li><li>Chapter 2</li></ol></li></ol></toc>", "Part 1 Chapter 1 Chapter 2")]
    [InlineData("<toc><h2>Volume</h2><p>One</p><div>Two</div></toc>", "Volume One Two")]
    [InlineData("<toc><p>Line one<br />Line two</p></toc>", "Line one Line two")]
    [InlineData("<toc><p>Tom &amp; Jerry &lt;3</p></toc>", "Tom & Jerry <3")]
    [InlineData("<toc><p>Glava 1  begins</p>\n\n<p>  next  </p></toc>", "Glava 1 begins next")]
    public void Extract_BuildsSearchText(string xml, string expected)
    {
        Assert.Equal(expected, TableOfContentsSearchText.Extract(new TableOfContents(xml)));
    }

    [Fact]
    public void Extract_NoBreakSpace_BecomesOrdinarySpace()
    {
        // TinyMCE stores repeated spaces as no-break spaces; the escapes keep the invisible character visible here.
        var toc = new TableOfContents("<toc><p>Glava\u00a01\u00a0\u00a0begins</p></toc>");

        Assert.Equal("Glava 1 begins", TableOfContentsSearchText.Extract(toc));
    }

    [Fact]
    public void Extract_UnknownElement_SeparatesWords()
    {
        // The sanitizer never stores unknown elements; if one appears anyway, a word boundary is the safer guess.
        Assert.Equal("Part Chapter", TableOfContentsSearchText.Extract(new TableOfContents("<toc>Part<section>Chapter</section></toc>")));
    }
}
