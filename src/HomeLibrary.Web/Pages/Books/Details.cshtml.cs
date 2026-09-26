using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeLibrary.Web.Pages.Books;

/// <summary>
/// Read-only book card with the table of contents and its export to an XML file.
/// </summary>
public sealed class DetailsModel(
    IQueryHandler<GetBookQuery, BookView> getBookHandler,
    IQueryHandler<ExportTableOfContentsQuery, TableOfContentsFile> exportHandler) : PageModel
{
    public BookView Book { get; private set; } = null!;

    public async Task OnGet(long id, CancellationToken cancellationToken)
    {
        Book = await getBookHandler.Handle(new GetBookQuery(id), cancellationToken);
    }

    public async Task<IActionResult> OnGetExportToc(long id, CancellationToken cancellationToken)
    {
        var file = await exportHandler.Handle(new ExportTableOfContentsQuery(id), cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }
}
