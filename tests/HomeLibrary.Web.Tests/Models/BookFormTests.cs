using HomeLibrary.Domain.Books;
using HomeLibrary.Web.Models;

namespace HomeLibrary.Web.Tests.Models;

public sealed class BookFormTests
{
    [Theory]
    [InlineData(nameof(BookDetails.TableOfContents), nameof(BookForm.TableOfContentsHtml))]
    [InlineData(nameof(BookDetails.Title), nameof(BookForm.Title))]
    [InlineData(nameof(BookDetails.PublicationYear), nameof(BookForm.PublicationYear))]
    public void ToFormField_MapsDomainFieldToFormProperty(string domainField, string formField)
    {
        Assert.Equal(formField, BookForm.ToFormField(domainField));
    }

    [Fact]
    public void ToFormField_EveryDomainFieldExistsInForm()
    {
        var formProperties = typeof(BookForm)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet();

        var missing = typeof(BookDetails)
            .GetProperties()
            .Select(property => BookForm.ToFormField(property.Name))
            .Where(field => !formProperties.Contains(field))
            .ToList();

        Assert.Empty(missing);
    }
}
