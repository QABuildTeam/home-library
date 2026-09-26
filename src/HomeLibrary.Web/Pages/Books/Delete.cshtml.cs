using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Commands;
using HomeLibrary.Application.Books.Queries;
using HomeLibrary.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeLibrary.Web.Pages.Books;

/// <summary>
/// Confirmation of book deletion.
/// </summary>
public sealed class DeleteModel(
    IQueryHandler<GetBookQuery, BookView> getBookHandler,
    ICommandHandler<DeleteBookCommand> deleteHandler) : PageModel
{
    private const string DELETED_MESSAGE = "The book has been deleted.";

    public BookView Book { get; private set; } = null!;

    public async Task OnGet(long id, CancellationToken cancellationToken)
    {
        Book = await getBookHandler.Handle(new GetBookQuery(id), cancellationToken);
    }

    public async Task<IActionResult> OnPost(long id, CancellationToken cancellationToken)
    {
        await deleteHandler.Handle(new DeleteBookCommand(id), cancellationToken);

        TempData[TempDataKeys.STATUS_MESSAGE] = DELETED_MESSAGE;

        return RedirectToPage("/Index");
    }
}
