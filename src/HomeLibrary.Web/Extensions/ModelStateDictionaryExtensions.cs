using HomeLibrary.Domain.Exceptions;
using HomeLibrary.Web.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HomeLibrary.Web.Extensions;

/// <summary>
/// Shows domain validation errors next to the corresponding fields of the book form.
/// </summary>
public static class ModelStateDictionaryExtensions
{
    private const string PROPERTY_SEPARATOR = ".";

    public static void AddBookFormErrors(this ModelStateDictionary modelState, DomainValidationException exception, string formPrefix)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(exception);

        foreach (var (field, error) in exception.Errors)
        {
            modelState.AddModelError(formPrefix + PROPERTY_SEPARATOR + BookForm.ToFormField(field), error);
        }
    }
}
