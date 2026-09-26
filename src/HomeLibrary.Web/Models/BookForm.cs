using System.ComponentModel.DataAnnotations;
using HomeLibrary.Application.Books;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Web.Models;

/// <summary>
/// Book card form. Data annotations give quick client-side feedback;
/// the authoritative validation is done by the domain validator.
/// </summary>
public sealed class BookForm
{
    [Required]
    [StringLength(BookConstraints.TITLE_MAX_LENGTH)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Required]
    [StringLength(BookConstraints.AUTHOR_MAX_LENGTH)]
    [Display(Name = "Author")]
    public string? Author { get; set; }

    [Range(BookConstraints.MIN_PUBLICATION_YEAR, int.MaxValue)]
    [Display(Name = "Publication year")]
    public int? PublicationYear { get; set; }

    [StringLength(BookConstraints.ISBN_MAX_LENGTH)]
    [Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [StringLength(BookConstraints.PUBLISHER_MAX_LENGTH)]
    [Display(Name = "Publisher")]
    public string? Publisher { get; set; }

    [Range(BookConstraints.MIN_PAGE_COUNT, BookConstraints.MAX_PAGE_COUNT)]
    [Display(Name = "Pages")]
    public int? PageCount { get; set; }

    [StringLength(BookConstraints.GENRE_MAX_LENGTH)]
    [Display(Name = "Genre")]
    public string? Genre { get; set; }

    [StringLength(BookConstraints.NOTES_MAX_LENGTH)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Table of contents")]
    public string? TableOfContentsHtml { get; set; }

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
    /// Maps a domain field name from a validation error to the name of the form property that shows it.
    /// </summary>
    public static string ToFormField(string domainField) =>
        domainField == nameof(BookDetails.TableOfContents) ? nameof(TableOfContentsHtml) : domainField;

    public BookInput ToInput() => new(Title, Author, PublicationYear, Isbn, Publisher, PageCount, Genre, Notes, TableOfContentsHtml);
}
