using System.Xml;
using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Domain.Books;

/// <summary>
/// Validates book attributes: required fields, lengths, allowed characters, publication year, page count and ISBN format.
/// </summary>
/// <param name="timeProvider">Source of the current date used to reject publication years in the future.</param>
public sealed class BookDetailsValidator(TimeProvider timeProvider) : IBookDetailsValidator
{
    private const char ISBN_HYPHEN = '-';
    private const char ISBN_SPACE = ' ';
    private const char ISBN10_CHECK_DIGIT_TEN = 'X';

    public void Validate(BookDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        Dictionary<string, string> errors = [];

        ValidateRequiredText(errors, nameof(BookDetails.Title), details.Title, BookConstraints.TITLE_MAX_LENGTH);
        ValidateRequiredText(errors, nameof(BookDetails.Author), details.Author, BookConstraints.AUTHOR_MAX_LENGTH);
        ValidateOptionalText(errors, nameof(BookDetails.Publisher), details.Publisher, BookConstraints.PUBLISHER_MAX_LENGTH);
        ValidateOptionalText(errors, nameof(BookDetails.Genre), details.Genre, BookConstraints.GENRE_MAX_LENGTH);
        ValidateOptionalText(errors, nameof(BookDetails.Notes), details.Notes, BookConstraints.NOTES_MAX_LENGTH);
        ValidatePublicationYear(errors, details.PublicationYear);
        ValidatePageCount(errors, details.PageCount);
        ValidateIsbn(errors, details.Isbn);

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }
    }

    private static void ValidateRequiredText(Dictionary<string, string> errors, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = $"{field} is required.";

            return;
        }

        ValidateOptionalText(errors, field, value, maxLength);
    }

    private static void ValidateOptionalText(Dictionary<string, string> errors, string field, string? value, int maxLength)
    {
        if (value is null)
        {
            return;
        }

        if (value.Length > maxLength)
        {
            errors[field] = $"{field} must not exceed {maxLength} characters.";

            return;
        }

        if (ContainsNonXmlCharacters(value))
        {
            errors[field] = $"{field} contains characters that are not allowed.";
        }
    }

    /// <summary>
    /// Detects characters that cannot be stored in PostgreSQL text (U+0000) or exported to XML: control characters,
    /// lone surrogates, U+FFFE and U+FFFF.
    /// Surrogate pairs (emoji and other characters outside the BMP) are valid.
    /// </summary>
    private static bool ContainsNonXmlCharacters(string value)
    {
        for (var index = 0; index < value.Length; ++index)
        {
            if (XmlConvert.IsXmlChar(value[index]))
            {
                continue;
            }

            if (index + 1 < value.Length && XmlConvert.IsXmlSurrogatePair(value[index + 1], value[index]))
            {
                ++index;

                continue;
            }

            return true;
        }

        return false;
    }

    private void ValidatePublicationYear(Dictionary<string, string> errors, int? year)
    {
        var currentYear = timeProvider.GetUtcNow().Year;

        if (year < BookConstraints.MIN_PUBLICATION_YEAR || year > currentYear)
        {
            errors[nameof(BookDetails.PublicationYear)] =
                $"Publication year must be between {BookConstraints.MIN_PUBLICATION_YEAR} and {currentYear}.";
        }
    }

    private static void ValidatePageCount(Dictionary<string, string> errors, int? pageCount)
    {
        if (pageCount < BookConstraints.MIN_PAGE_COUNT || pageCount > BookConstraints.MAX_PAGE_COUNT)
        {
            errors[nameof(BookDetails.PageCount)] =
                $"Page count must be between {BookConstraints.MIN_PAGE_COUNT} and {BookConstraints.MAX_PAGE_COUNT}.";
        }
    }

    private static void ValidateIsbn(Dictionary<string, string> errors, string? isbn)
    {
        if (isbn is null)
        {
            return;
        }

        if (isbn.Length > BookConstraints.ISBN_MAX_LENGTH || !IsIsbnFormat(isbn))
        {
            errors[nameof(BookDetails.Isbn)] =
                "ISBN must contain 10 or 13 digits (ISBN-10 may end with 'X'), hyphens and spaces are allowed.";
        }
    }

    private static bool IsIsbnFormat(string isbn)
    {
        var digits = isbn
            .Where(symbol => symbol is not (ISBN_HYPHEN or ISBN_SPACE))
            .Select(char.ToUpperInvariant)
            .ToArray();

        return digits.Length switch
        {
            BookConstraints.ISBN10_LENGTH => digits[..^1].All(char.IsAsciiDigit)
                && (char.IsAsciiDigit(digits[^1]) || digits[^1] == ISBN10_CHECK_DIGIT_TEN),
            BookConstraints.ISBN13_LENGTH => digits.All(char.IsAsciiDigit),
            _ => false
        };
    }
}
