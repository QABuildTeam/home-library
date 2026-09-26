using HomeLibrary.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeLibrary.Web.Filters;

/// <summary>
/// Turns "not found" exceptions thrown by page handlers into the 404 response,
/// so pages do not need to repeat the same try/catch block.
/// </summary>
public sealed class NotFoundExceptionPageFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var executedContext = await next();

        if (executedContext.Exception is BookNotFoundException or TableOfContentsNotFoundException)
        {
            executedContext.Result = new NotFoundResult();
            executedContext.ExceptionHandled = true;
        }
    }
}
