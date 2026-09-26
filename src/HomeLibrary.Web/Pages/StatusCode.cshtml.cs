using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HomeLibrary.Web.Pages;

/// <summary>
/// Friendly page for error status codes without a body (404 and others).
/// </summary>
public sealed class StatusCodeModel : PageModel
{
    public int Code { get; private set; }

    public void OnGet(int code)
    {
        Code = code;
    }
}
