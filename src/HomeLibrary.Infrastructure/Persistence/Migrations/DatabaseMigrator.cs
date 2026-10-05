using DbUp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies the embedded SQL scripts with DbUp. Applied scripts are recorded in the <c>schema_versions</c> journal table
/// of the library schema, so every script runs exactly once.
/// </summary>
internal sealed class DatabaseMigrator(IOptions<DatabaseOptions> options, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    private const string SCHEMA_VARIABLE = "schema";
    private const string JOURNAL_TABLE = "schema_versions";
    private const string SCRIPTS_PREFIX = "HomeLibrary.Infrastructure.Persistence.Migrations.Scripts.";

    public async Task Migrate(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await CreateSchema(settings, cancellationToken);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(settings.ConnectionString, settings.Schema)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly, IsMigrationScript)
            .WithVariable(SCHEMA_VARIABLE, settings.Schema)
            .JournalToPostgresqlTable(settings.Schema, JOURNAL_TABLE)
            .WithTransactionPerScript()
            .LogTo(new DbUpLogger(logger))
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException($"Database migration failed on script '{result.ErrorScript?.Name}'.", result.Error);
        }

        logger.LogInformation("Database schema '{Schema}' is up to date", settings.Schema);
    }

    private static bool IsMigrationScript(string resourceName) => resourceName.StartsWith(SCRIPTS_PREFIX, StringComparison.Ordinal);

    private static async Task CreateSchema(DatabaseOptions settings, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(settings.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        // The schema name is validated by DatabaseOptionsValidator, so it is safe to put it into the SQL text.
        await using var command = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS {settings.Schema}", connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
