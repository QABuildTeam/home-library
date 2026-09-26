using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Commands;
using HomeLibrary.Domain.Exceptions;
using HomeLibrary.Web.Extensions;
using HomeLibrary.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeLibrary.Web.Pages.Books;

/// <summary>
/// Form for adding a new book.
/// </summary>
public sealed class CreateModel(ICommandHandler<CreateBookCommand, long> createHandler) : PageModel
{
    private const string CREATED_MESSAGE = "The book has been added.";

    [BindProperty]
    public BookForm Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var id = await createHandler.Handle(new CreateBookCommand(Input.ToInput()), cancellationToken);

            TempData[TempDataKeys.STATUS_MESSAGE] = CREATED_MESSAGE;

            return RedirectToPage("/Books/Details", new { id });
        }
        catch (DomainValidationException exception)
        {
            ModelState.AddBookFormErrors(exception, nameof(Input));

            return Page();
        }
    }
}
