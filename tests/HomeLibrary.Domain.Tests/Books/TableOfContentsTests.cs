using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Domain.Tests.Books;

public sealed class TableOfContentsTests
{
    [Fact]
    public void Constructor_RemovesXmlDeclaration()
    {
        var toc = new TableOfContents("<?xml version=\"1.0\" encoding=\"utf-8\"?><toc><p>Chapter 1</p></toc>");

        Assert.Equal("<toc><p>Chapter 1</p></toc>", toc.Xml);
    }

    [Fact]
    public void Constructor_PreservesWhitespaceBetweenInlineElements()
    {
        const string XML = "<toc><p><strong>Chapter 1.</strong> <em>Introduction</em></p></toc>";

        Assert.Equal(XML, new TableOfContents(XML).Xml);
    }

    [Theory]
    [InlineData("<toc><p>Unclosed</toc>")]
    [InlineData("plain text")]
    [InlineData("<contents><p>Wrong root</p></contents>")]
    [InlineData("<toc xmlns=\"urn:other\"><p>Namespaced root</p></toc>")]
    public void Constructor_InvalidXml_ThrowsValidationException(string xml)
    {
        var exception = Assert.Throws<DomainValidationException>(() => new TableOfContents(xml));

        Assert.Contains(nameof(BookDetails.TableOfContents), exception.Errors.Keys);
    }

    [Fact]
    public void Constructor_DocumentTypeDefinition_IsRejected()
    {
        const string XML = "<!DOCTYPE toc [<!ENTITY a \"aaaaaaaaaa\">]><toc>&a;</toc>";

        Assert.Throws<DomainValidationException>(() => new TableOfContents(XML));
    }

    [Fact]
    public void Equals_ComparesNormalizedXml()
    {
        var first = new TableOfContents("<?xml version=\"1.0\"?><toc><p>One</p></toc>");
        var second = new TableOfContents("<toc><p>One</p></toc>");
        var other = new TableOfContents("<toc><p>Two</p></toc>");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
    }
}
