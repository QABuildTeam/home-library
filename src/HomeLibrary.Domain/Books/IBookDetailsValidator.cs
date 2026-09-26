using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Domain.Books;

/// <summary>
/// Checks book attributes against the business rules.
/// </summary>
public interface IBookDetailsValidator
{
    /// <summary>
    /// Validates the book attributes.
    /// </summary>
    /// <param name="details">Attributes to validate.</param>
    /// <exception cref="DomainValidationException">One or more attributes are invalid.</exception>
    void Validate(BookDetails details);
}
