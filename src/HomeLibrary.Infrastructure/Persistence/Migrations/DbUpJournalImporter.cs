using HomeLibrary.Infrastructure.Persistence.Migrations.Versions;
using Npgsql;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Moves the journal of the former DbUp migrations (<c>schema_versions</c>) into the FluentMigrator version table,
/// so that the scripts already applied by DbUp are not run again, and drops the old journal.
/// Everything happens in one transaction; an unknown script in the old journal stops the migration.
/// </summary>
internal static class DbUpJournalImporter
{
    private const string LEGACY_JOURNAL_TABLE = "schema_versions";
    private const string SCRIPT_PREFIX = "HomeLibrary.Infrastructure.Persistence.Migrations.Scripts.";

    private static readonly Dictionary<string, (long Version, string Description)> _legacyScripts = new()
    {
        [SCRIPT_PREFIX + "0001_CreateBookTable.sql"] = (MigrationVersions.CREATE_BOOK_TABLE, nameof(CreateBookTable)),
        [SCRIPT_PREFIX + "0002_CreateBookRoutines.sql"] = (MigrationVersions.CREATE_BOOK_ROUTINES, nameof(CreateBookRoutines)),
        [SCRIPT_PREFIX + "0003_NormalizeTocSearchText.sql"] =
            (MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT, nameof(NormalizeTocSearchText))
    };

    /// <summary>
    /// Imports the old journal when it exists. The FluentMigrator version table must already exist.
    /// </summary>
    /// <returns>Number of migrations added to the version table; 0 when there is no old journal.</returns>
    /// <exception cref="InvalidOperationException">The old journal contains a script this application does not know.</exception>
    public static async Task<int> Import(string connectionString, string schema, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (!await JournalExists(connection, transaction, schema, cancellationToken))
        {
            return 0;
        }

        var appliedScripts = await ReadJournal(connection, transaction, schema, cancellationToken);
        var unknown = appliedScripts
            .Select(script => script.Name)
            .Where(name => !_legacyScripts.ContainsKey(name))
            .ToList();

        if (unknown.Count > 0)
        {
            throw new InvalidOperationException(
                $"The DbUp journal {schema}.{LEGACY_JOURNAL_TABLE} contains unknown scripts: {string.Join(", ", unknown)}.");
        }

        var imported = 0;

        // foreach, not LINQ: every insert is awaited.
        foreach (var (name, appliedAt) in appliedScripts)
        {
            var (version, description) = _legacyScripts[name];

            await using var insert = new NpgsqlCommand(
                $"""
                INSERT INTO {schema}.{LibraryVersionTableMetaData.TABLE_NAME}
                    ({LibraryVersionTableMetaData.VERSION_COLUMN}, {LibraryVersionTableMetaData.APPLIED_ON_COLUMN},
                     {LibraryVersionTableMetaData.DESCRIPTION_COLUMN})
                SELECT @version, @appliedAt, @description
                 WHERE NOT EXISTS (SELECT 1 FROM {schema}.{LibraryVersionTableMetaData.TABLE_NAME}
                                    WHERE {LibraryVersionTableMetaData.VERSION_COLUMN} = @version)
                """,
                connection,
                transaction);

            insert.Parameters.AddWithValue("version", version);
            insert.Parameters.AddWithValue("appliedAt", ToUtc(appliedAt));
            insert.Parameters.AddWithValue("description", description);

            imported += await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var drop = new NpgsqlCommand($"DROP TABLE {schema}.{LEGACY_JOURNAL_TABLE}", connection, transaction))
        {
            await drop.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return imported;
    }

    /// <summary>
    /// DbUp recorded the local time of the machine that ran the migration, FluentMigrator records UTC. The value is
    /// converted assuming the same time zone and sent with <see cref="DateTimeKind.Unspecified"/>: a UTC kind would make
    /// Npgsql send timestamptz, which PostgreSQL would convert back to the session time zone.
    /// </summary>
    private static DateTime ToUtc(DateTime localAppliedAt) => DateTime.SpecifyKind(
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localAppliedAt, DateTimeKind.Unspecified), TimeZoneInfo.Local),
        DateTimeKind.Unspecified);

    private static async Task<bool> JournalExists(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection, transaction);

        command.Parameters.AddWithValue("name", $"{schema}.{LEGACY_JOURNAL_TABLE}");

        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<List<(string Name, DateTime AppliedAt)>> ReadJournal(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT scriptname, applied FROM {schema}.{LEGACY_JOURNAL_TABLE} ORDER BY applied",
            connection,
            transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        List<(string Name, DateTime AppliedAt)> scripts = [];

        while (await reader.ReadAsync(cancellationToken))
        {
            scripts.Add((reader.GetString(0), reader.GetDateTime(1)));
        }

        return scripts;
    }
}
