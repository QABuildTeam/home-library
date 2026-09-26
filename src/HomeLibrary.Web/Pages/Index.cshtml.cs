using System.Globalization;
using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Queries;
using HomeLibrary.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HomeLibrary.Web.Pages;

/// <summary>
/// Book list with search by title, author and table of contents.
/// </summary>
public sealed class IndexModel(IQueryHandler<SearchBooksQuery, PagedResult<BookListItem>> searchHandler) : PageModel
{
    public const int PAGE_SIZE = 20;
    public const string QUERY_PARAMETER = "q";
    public const string SCOPE_PARAMETER = "in";

    // Not "page": Razor Pages reserves that route value for the page path.
    public const string PAGE_PARAMETER = "p";

    /// <summary>
    /// Search areas shown as checkboxes.
    /// </summary>
    public static IReadOnlyList<(BookSearchScope Scope, string Label)> SearchScopes { get; } =
    [
        (BookSearchScope.Title, "Title"),
        (BookSearchScope.Author, "Author"),
        (BookSearchScope.TableOfContents, "Table of contents")
    ];

    [BindProperty(SupportsGet = true, Name = QUERY_PARAMETER)]
    public string? Query { get; set; }

    /// <summary>
    /// Selected search areas. Nothing selected means all areas.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = SCOPE_PARAMETER)]
    public List<BookSearchScope> Scopes { get; set; } = [];

    [BindProperty(SupportsGet = true, Name = PAGE_PARAMETER)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<BookListItem> Result { get; private set; } = new([], 0, 1, PAGE_SIZE);

    private BookSearchScope Scope => Scopes.Aggregate(BookSearchScope.None, (scope, item) => scope | item);

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        Result = await searchHandler.Handle(new SearchBooksQuery(Query, Scope, PageNumber, PAGE_SIZE), cancellationToken);

        // A page beyond the end of the list (for example, after deletions) has no rows and no total count.
        if (Result.Items.Count == 0 && Result.Page > 1)
        {
            return Redirect(PageUrl(1));
        }

        return Page();
    }

    /// <summary>
    /// A checkbox is checked when its area is selected or when no area is selected at all.
    /// </summary>
    public bool IsSearchedIn(BookSearchScope scope) => Scope == BookSearchScope.None || Scope.HasFlag(scope);

    /// <summary>
    /// URL of another page of the same search.
    /// </summary>
    public string PageUrl(int page)
    {
        List<KeyValuePair<string, string?>> parameters =
        [
            new(QUERY_PARAMETER, Query),
            .. Scopes.Select(scope => new KeyValuePair<string, string?>(SCOPE_PARAMETER, scope.ToString())),
            new(PAGE_PARAMETER, page.ToString(CultureInfo.InvariantCulture))
        ];

        return QueryHelpers.AddQueryString(Url.Page("/Index") ?? string.Empty, parameters);
    }
}
