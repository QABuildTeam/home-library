using FluentMigrator.Runner;
using HomeLibrary.Infrastructure.Persistence;
using HomeLibrary.Infrastructure.Persistence.Migrations;
using HomeLibrary.Infrastructure.Persistence.Migrations.Versions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Infrastructure.Tests.Persistence.Migrations;

/// <summary>
/// Every test migrates its own temporary schema and drops it afterwards.
/// </summary>
public sealed class DatabaseMigratorTests : IAsyncLifetime
{
    private const string SCHEMA_PREFIX = "library_migration_test_";
    private const string SCRIPT_PREFIX = "HomeLibrary.Infrastructure.Persistence.Migrations.Scripts.";
    private const string UNKNOWN_SCRIPT = SCRIPT_PREFIX + "0099_Unknown.sql";
    private const string DBUP_JOURNAL_TABLE = "schema_versions";
    private const string VERSION_TABLE = "version_info";
    private const string BOOK_TABLE = "book";
    private const string NORMALIZE_FUNCTION = "normalize_search_text";
    private const string TOC_TEXT_FUNCTION = "book_toc_text";
    private const string UNSAFE_SCHEMA = "library; DROP SCHEMA public";
    private const long NO_MIGRATIONS = 0L;
    private const string TOC_WITH_FORMATTED_WORD = "<toc><p><strong>Chapter</strong> 3</p></toc>";
    private const string NORMALIZED_TOC_TEXT = "Chapter 3";
    private const string TOC_TEXT_BEFORE_NORMALIZATION = "Chapter  3";
    private const string TOC_WITH_SPLIT_WORD = "<toc><p>Intro<em>duction</em></p></toc>";
    private const string SQL_TOC_TEXT_OF_SPLIT_WORD = "Intro duction";
    private const string APPLICATION_TOC_TEXT_OF_SPLIT_WORD = "Introduction";
    private const string INVALID_TOC_FRAGMENT = "<p>One</p><p>Two</p>";

    // DbUp journal rows: script, local time of application, expected FluentMigrator version and description.
    private static readonly (string Script, DateTime AppliedAt, long Version, string Description)[] _dbUpJournal =
    [
        (SCRIPT_PREFIX + "0001_CreateBookTable.sql", new DateTime(2026, 9, 25, 23, 51, 59),
            MigrationVersions.CREATE_BOOK_TABLE, nameof(CreateBookTable)),
        (SCRIPT_PREFIX + "0002_CreateBookRoutines.sql", new DateTime(2026, 9, 25, 23, 52, 0),
            MigrationVersions.CREATE_BOOK_ROUTINES, nameof(CreateBookRoutines)),
        (SCRIPT_PREFIX + "0003_NormalizeTocSearchText.sql", new DateTime(2026, 9, 26, 0, 15, 13),
            MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT, nameof(NormalizeTocSearchText))
    ];

    private static readonly long[] _allVersions =
    [
        MigrationVersions.CREATE_BOOK_TABLE,
        MigrationVersions.CREATE_BOOK_ROUTINES,
        MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT,
        MigrationVersions.MOVE_TOC_SEARCH_TEXT_TO_APPLICATION
    ];

    private readonly DatabaseOptions _options = TestConfiguration.BuildOptions(TestConfiguration.NewSchemaName(SCHEMA_PREFIX));

    public Task InitializeAsync() => CreateMigrator().Migrate(CancellationToken.None);

    public Task DisposeAsync() => Execute($"DROP SCHEMA IF EXISTS {_options.Schema} CASCADE");

    [Fact]
    public async Task Migrate_FreshSchema_RecordsAllMigrations()
    {
        Assert.Equal(_allVersions, (await ReadVersionTable()).Select(row => row.Version));
    }

    [Fact]
    public async Task Migrate_FullDbUpJournal_IsImportedWithDatesAndDescriptions()
    {
        await ReplaceVersionTableWithDbUpJournal(_dbUpJournal);

        await CreateMigrator().Migrate(CancellationToken.None);

        var expected = _dbUpJournal
            .Select(row => (row.Version, ToUtc(row.AppliedAt), row.Description))
            .ToList();

        var versionTable = await ReadVersionTable();

        // The DbUp scripts are imported; the migrations written after DbUp run as usual.
        Assert.Equal(expected, versionTable.Take(_dbUpJournal.Length));
        Assert.Equal(_allVersions, versionTable.Select(row => row.Version));
        Assert.False(await TableExists(DBUP_JOURNAL_TABLE));
    }

    [Fact]
    public async Task Migrate_PartialDbUpJournal_ImportsKnownScriptsAndRunsTheRest()
    {
        await ReplaceVersionTableWithDbUpJournal(_dbUpJournal[..^1]);

        // normalize_search_text is created only by the third migration, so its presence afterwards proves that the
        // migration ran (the other routines it replaces exist anyway).
        await Execute($"DROP FUNCTION {_options.Schema}.{NORMALIZE_FUNCTION}(text)");

        await CreateMigrator().Migrate(CancellationToken.None);

        var versionTable = await ReadVersionTable();

        Assert.Equal(_allVersions, versionTable.Select(row => row.Version));
        Assert.Equal(ToUtc(_dbUpJournal[1].AppliedAt), versionTable[1].AppliedOn);
        Assert.NotEqual(ToUtc(_dbUpJournal[2].AppliedAt), versionTable[2].AppliedOn);
        Assert.True(await FunctionExists(NORMALIZE_FUNCTION));
        Assert.False(await TableExists(DBUP_JOURNAL_TABLE));
    }

    [Fact]
    public async Task Migrate_UnknownScriptInDbUpJournal_ThrowsAndKeepsJournal()
    {
        await ReplaceVersionTableWithDbUpJournal([.. _dbUpJournal, (UNKNOWN_SCRIPT, DateTime.Now, 0L, string.Empty)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMigrator().Migrate(CancellationToken.None));

        Assert.Contains(UNKNOWN_SCRIPT, exception.Message);
        Assert.True(await TableExists(DBUP_JOURNAL_TABLE));
        Assert.Empty(await ReadVersionTable());
    }

    [Fact]
    public async Task MigrateDown_ToNoMigrations_ThenUp_RestoresSchema()
    {
        RunRunner(runner => runner.MigrateDown(NO_MIGRATIONS));

        Assert.False(await TableExists(BOOK_TABLE));
        Assert.Equal(0L, await CountRoutines());
        Assert.Empty(await ReadVersionTable());

        await CreateMigrator().Migrate(CancellationToken.None);

        Assert.True(await TableExists(BOOK_TABLE));
        Assert.Equal(_allVersions, (await ReadVersionTable()).Select(row => row.Version));
    }

    [Fact]
    public async Task MigrateDown_NormalizeTocSearchText_RestoresPreviousSearchText()
    {
        var bookId = await InsertBookDirectly(TOC_WITH_FORMATTED_WORD);

        // Down of MoveTocSearchTextToApplication rebuilds the text with the SQL function of NormalizeTocSearchText.
        RunRunner(runner => runner.MigrateDown(MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT));

        Assert.Equal(NORMALIZED_TOC_TEXT, await ReadTocText(bookId));

        RunRunner(runner => runner.MigrateDown(MigrationVersions.CREATE_BOOK_ROUTINES));

        Assert.Equal(TOC_TEXT_BEFORE_NORMALIZATION, await ReadTocText(bookId));
        Assert.False(await FunctionExists(NORMALIZE_FUNCTION));

        await CreateMigrator().Migrate(CancellationToken.None);

        Assert.Equal(NORMALIZED_TOC_TEXT, await ReadTocText(bookId));
    }

    [Fact]
    public async Task Migrate_MoveTocSearchTextToApplication_RebuildsSearchTextOfEachExistingBook()
    {
        RunRunner(runner => runner.MigrateDown(MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT));

        // Before the migration the database builds the text and splits a word formatted in the middle.
        var splitWordBook = await InsertBookDirectly(TOC_WITH_SPLIT_WORD);
        var formattedWordBook = await InsertBookDirectly(TOC_WITH_FORMATTED_WORD);
        var bookWithoutToc = await InsertBookDirectly(tocXml: null);

        await Execute($"UPDATE {_options.Schema}.{BOOK_TABLE} SET toc_text = {_options.Schema}.{TOC_TEXT_FUNCTION}(toc)");

        Assert.Equal(SQL_TOC_TEXT_OF_SPLIT_WORD, await ReadTocText(splitWordBook));

        await CreateMigrator().Migrate(CancellationToken.None);

        Assert.Equal(APPLICATION_TOC_TEXT_OF_SPLIT_WORD, await ReadTocText(splitWordBook));
        Assert.Equal(NORMALIZED_TOC_TEXT, await ReadTocText(formattedWordBook));
        Assert.Null(await ReadTocText(bookWithoutToc));
        Assert.False(await FunctionExists(TOC_TEXT_FUNCTION));
    }

    [Fact]
    public async Task Migrate_MoveTocSearchTextToApplication_InvalidStoredToc_NamesTheBook()
    {
        RunRunner(runner => runner.MigrateDown(MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT));

        // The xml column accepts fragments and other roots; such a row can only appear outside the application.
        var invalidBook = await InsertBookDirectly(INVALID_TOC_FRAGMENT);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMigrator().Migrate(CancellationToken.None));

        var messages = Unwrap(exception).Select(inner => inner.Message);

        Assert.Contains(messages, message => message.Contains($"book {invalidBook}", StringComparison.Ordinal));
        Assert.True(await FunctionExists(TOC_TEXT_FUNCTION));
    }

    [Fact]
    public async Task Migrate_FailingMigration_ThrowsInvalidOperationExceptionWithCause()
    {
        // An unregistered "book" table makes the first migration fail on CREATE TABLE.
        await Execute($"""
            DROP SCHEMA {_options.Schema} CASCADE;
            CREATE SCHEMA {_options.Schema};
            CREATE TABLE {_options.Schema}.{BOOK_TABLE} (id integer);
            """);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMigrator().Migrate(CancellationToken.None));

        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void CreateMigrationServices_UnsafeSchemaName_Throws()
    {
        var options = new DatabaseOptions { ConnectionString = _options.ConnectionString, Schema = UNSAFE_SCHEMA };

        Assert.Throws<OptionsValidationException>(() => MigrationServices.Create(options, NullLoggerFactory.Instance));
    }

    private DatabaseMigrator CreateMigrator() =>
        new(Options.Create(_options), NullLoggerFactory.Instance, NullLogger<DatabaseMigrator>.Instance);

    private void RunRunner(Action<IMigrationRunner> action)
    {
        using var services = MigrationServices.Create(_options, NullLoggerFactory.Instance);
        using var scope = services.CreateScope();

        action(scope.ServiceProvider.GetRequiredService<IMigrationRunner>());
    }

    /// <summary>
    /// The DbUp journal stores the local time of the application; FluentMigrator stores UTC.
    /// The expectation repeats the production formula, because both depend on TimeZoneInfo.Local: on a machine whose
    /// local time zone is UTC the conversion is the identity, and an import without conversion cannot be detected.
    /// </summary>
    private static DateTime ToUtc(DateTime localTime) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(localTime, TimeZoneInfo.Local), DateTimeKind.Unspecified);

    /// <summary>
    /// Simulates a database migrated by the former DbUp code: the objects of the DbUp scripts exist (the schema is
    /// rolled back to the last of them), the FluentMigrator version table does not, and only the DbUp journal knows
    /// about the applied scripts.
    /// </summary>
    private async Task ReplaceVersionTableWithDbUpJournal(
        IEnumerable<(string Script, DateTime AppliedAt, long Version, string Description)> journal)
    {
        RunRunner(runner => runner.MigrateDown(MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT));

        await Execute($"""
            DROP TABLE {_options.Schema}.{VERSION_TABLE};
            CREATE TABLE {_options.Schema}.{DBUP_JOURNAL_TABLE}
            (
                schemaversionsid serial PRIMARY KEY,
                scriptname       varchar(255) NOT NULL,
                applied          timestamp    NOT NULL
            );
            """);

        await using var connection = await Open();

        // foreach, not LINQ: every insert is awaited.
        foreach (var row in journal)
        {
            await using var command = new NpgsqlCommand(
                $"INSERT INTO {_options.Schema}.{DBUP_JOURNAL_TABLE} (scriptname, applied) VALUES (@script, @applied)",
                connection);

            command.Parameters.AddWithValue("script", row.Script);
            command.Parameters.AddWithValue("applied", DateTime.SpecifyKind(row.AppliedAt, DateTimeKind.Unspecified));

            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<List<(long Version, DateTime AppliedOn, string Description)>> ReadVersionTable()
    {
        if (!await TableExists(VERSION_TABLE))
        {
            return [];
        }

        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            $"SELECT version, applied_on, description FROM {_options.Schema}.{VERSION_TABLE} ORDER BY version",
            connection);
        await using var reader = await command.ExecuteReaderAsync();

        List<(long Version, DateTime AppliedOn, string Description)> rows = [];

        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetInt64(0), reader.GetDateTime(1), reader.GetString(2)));
        }

        return rows;
    }

    /// <summary>
    /// Inserts a book bypassing the procedures, whose signature differs between the migrations.
    /// </summary>
    private async Task<long> InsertBookDirectly(string? tocXml)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            $"INSERT INTO {_options.Schema}.{BOOK_TABLE} (title, author, toc) VALUES ('Title', 'Author', @toc::xml) RETURNING id",
            connection);

        command.Parameters.AddWithValue("toc", (object?)tocXml ?? DBNull.Value);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string?> ReadTocText(long bookId)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand($"SELECT toc_text FROM {_options.Schema}.{BOOK_TABLE} WHERE id = @id", connection);

        command.Parameters.AddWithValue("id", bookId);

        var value = await command.ExecuteScalarAsync();

        return value is DBNull ? null : (string?)value;
    }

    private static IEnumerable<Exception> Unwrap(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private async Task<bool> TableExists(string table)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);

        command.Parameters.AddWithValue("name", $"{_options.Schema}.{table}");

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> FunctionExists(string function)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace"
            + " WHERE n.nspname = @schema AND p.proname = @function)",
            connection);

        command.Parameters.AddWithValue("schema", _options.Schema);
        command.Parameters.AddWithValue("function", function);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> CountRoutines()
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace WHERE n.nspname = @schema",
            connection);

        command.Parameters.AddWithValue("schema", _options.Schema);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task Execute(string sql)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(sql, connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<NpgsqlConnection> Open()
    {
        var connection = new NpgsqlConnection(_options.ConnectionString);

        await connection.OpenAsync();

        return connection;
    }
}
