using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;
using HomeLibrary.Web.Models;

namespace HomeLibrary.Web.Mappers;

/// <summary>
/// Converts between the book card form and the application models.
/// </summary>
internal static class BookFormMapper
{
    /// <summary>
    /// Fills the edit form with the attributes of an existing book.
    /// </summary>
    public static BookForm FromView(BookView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new BookForm
        {
            Title = view.Title,
            Author = view.Author,
            PublicationYear = view.PublicationYear,
            Isbn = view.Isbn,
            Publisher = view.Publisher,
            PageCount = view.PageCount,
            Genre = view.Genre,
            Notes = view.Notes,
            TableOfContentsHtml = view.TableOfContentsHtml
        };
    }

    /// <summary>
    /// Turns the submitted form into the input of the create and update use cases.
    /// </summary>
    public static BookInput ToInput(BookForm form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new BookInput(
            Title: form.Title,
            Author: form.Author,
            PublicationYear: form.PublicationYear,
            Isbn: form.Isbn,
            Publisher: form.Publisher,
            PageCount: form.PageCount,
            Genre: form.Genre,
            Notes: form.Notes,
            TableOfContentsHtml: form.TableOfContentsHtml);
    }

    /// <summary>
    /// Maps a domain field name from a validation error to the name of the form property that shows it.
    /// </summary>
    public static string ToFormField(string domainField)
    {
        ArgumentNullException.ThrowIfNull(domainField);

        return domainField == nameof(BookDetails.TableOfContents) ? nameof(BookForm.TableOfContentsHtml) : domainField;
    }
}
