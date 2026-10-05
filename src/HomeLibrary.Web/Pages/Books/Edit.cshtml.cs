using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.UseCases.GetBook;
using HomeLibrary.Application.Books.UseCases.UpdateBook;
using HomeLibrary.Domain.Exceptions;
using HomeLibrary.Web.Extensions;
using HomeLibrary.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeLibrary.Web.Pages.Books;

/// <summary>
/// Book card in edit mode, including the HTML editor of the table of contents.
/// </summary>
public sealed class EditModel(
    IQueryHandler<GetBookQuery, BookView> getBookHandler,
    ICommandHandler<UpdateBookCommand> updateHandler) : PageModel
{
    private const string SAVED_MESSAGE = "The changes have been saved.";

    [BindProperty]
    public BookForm Input { get; set; } = new();

    /// <summary>
    /// Version of the book loaded into the form; the save fails when someone changed the book in the meantime.
    /// </summary>
    [BindProperty]
    public int Version { get; set; }

    public long Id { get; private set; }

    public bool HasConflict { get; private set; }

    public async Task OnGet(long id, CancellationToken cancellationToken)
    {
        var book = await getBookHandler.Handle(new GetBookQuery(id), cancellationToken);

        Id = id;
        Input = BookForm.FromView(book);
        Version = book.Version;
    }

    public async Task<IActionResult> OnPost(long id, CancellationToken cancellationToken)
    {
        Id = id;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await updateHandler.Handle(new UpdateBookCommand(id, Version, Input.ToInput()), cancellationToken);

            TempData[TempDataKeys.STATUS_MESSAGE] = SAVED_MESSAGE;

            return RedirectToPage("/Books/Details", new { id });
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddBookFormErrors(exception, nameof(Input));

            return Page();
        }
        catch (BookConcurrencyException exception)
        {
            HasConflict = true;
            ModelState.AddModelError(string.Empty, exception.Message);

            return Page();
        }
    }
}
