namespace HomeLibrary.Infrastructure.Persistence;

/// <summary>
/// Database connection settings bound from the <c>Database</c> configuration section.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SECTION_NAME = "Database";
    public const string DEFAULT_SCHEMA = "library";

    /// <summary>
    /// Npgsql connection string. Keep the password in user secrets or environment variables.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Schema that holds the library tables and stored routines.
    /// </summary>
    public string Schema { get; set; } = DEFAULT_SCHEMA;
}
