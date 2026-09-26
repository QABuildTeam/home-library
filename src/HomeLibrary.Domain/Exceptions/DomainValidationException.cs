namespace HomeLibrary.Domain.Exceptions;

/// <summary>
/// Thrown when domain data violates business rules. Contains one error message per invalid field.
/// </summary>
/// <param name="errors">Error messages keyed by field name.</param>
public sealed class DomainValidationException(IReadOnlyDictionary<string, string> errors) : Exception(BuildMessage(errors))
{
    public DomainValidationException(string field, string error)
        : this(new Dictionary<string, string>
        {
            [field] = error
        })
    {
    }

    public IReadOnlyDictionary<string, string> Errors { get; } = errors;

    private static string BuildMessage(IReadOnlyDictionary<string, string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return "Validation failed: " + string.Join("; ", errors.Select(error => $"{error.Key}: {error.Value}"));
    }
}
