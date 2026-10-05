using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Services;

/// <summary>
/// Builds normalized book attributes from the user input.
/// </summary>
public interface IBookDetailsFactory
{
    /// <summary>
    /// Trims the text fields, turns empty optional fields into <c>null</c> and converts the table of contents to XML.
    /// </summary>
    /// <param name="input">User input.</param>
    BookDetails Create(BookInput input);
}
