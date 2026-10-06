using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace HomeLibrary.Infrastructure.Persistence;

/// <summary>
/// Validates the database settings at startup. The schema name is inserted into SQL text,
/// so only simple lowercase identifiers are accepted.
/// </summary>
internal sealed partial class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    private const string SCHEMA_NAME_PATTERN = "^[a-z_][a-z0-9_]{0,62}$";

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add(
                $"{DatabaseOptions.SECTION_NAME}:{nameof(DatabaseOptions.ConnectionString)} is not configured. "
                + "Set it with 'dotnet user-secrets' or an environment variable.");
        }

        if (!SchemaNameRegex().IsMatch(options.Schema))
        {
            failures.Add(
                $"{DatabaseOptions.SECTION_NAME}:{nameof(DatabaseOptions.Schema)} must be a lowercase identifier "
                + "(letters, digits and underscores).");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    [GeneratedRegex(SCHEMA_NAME_PATTERN, RegexOptions.CultureInvariant)]
    private static partial Regex SchemaNameRegex();
}
