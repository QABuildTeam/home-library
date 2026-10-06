using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies the FluentMigrator migrations of this assembly. Applied migrations are recorded in the <c>version_info</c>
/// table of the library schema, so every migration runs exactly once. A journal left by the former DbUp migrations
/// is imported into that table first.
/// </summary>
internal sealed class DatabaseMigrator(
    IOptions<DatabaseOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    public async Task Migrate(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await using var services = MigrationServices.Create(settings, loggerFactory);

        await CreateSchema(settings, cancellationToken);

        // Loading the version information creates the version table when it does not exist yet.
        using (var scope = services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMigrationRunner>().LoadVersionInfoIfRequired();
        }

        var imported = await DbUpJournalImporter.Import(settings.ConnectionString, settings.Schema, cancellationToken);

        if (imported > 0)
        {
            logger.LogInformation("Imported {Count} migrations from the DbUp journal of schema '{Schema}'", imported, settings.Schema);
        }

        // A new scope reads the version table again, including the imported migrations.
        using (var scope = services.CreateScope())
        {
            ApplyPendingMigrations(scope.ServiceProvider, settings.Schema);
        }

        logger.LogInformation("Database schema '{Schema}' is up to date", settings.Schema);
    }

    private void ApplyPendingMigrations(IServiceProvider services, string schema)
    {
        var runner = services.GetRequiredService<IMigrationRunner>();
        var versionLoader = services.GetRequiredService<IVersionLoader>();

        runner.LoadVersionInfoIfRequired();

        // FluentMigrator logs at the Information level, and appsettings.json filters its messages out below Warning,
        // so the migrations about to be applied are listed here.
        var pending = runner.MigrationLoader
            .LoadMigrations()
            .Where(migration => !versionLoader.VersionInfo.HasAppliedMigration(migration.Key))
            .Select(migration => $"{migration.Key} {migration.Value.Migration.GetType().Name}")
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        logger.LogInformation("Applying migrations to schema '{Schema}': {Migrations}", schema, string.Join(", ", pending));

        try
        {
            runner.MigrateUp();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Database migration of schema '{schema}' failed.", exception);
        }
    }

    private static async Task CreateSchema(DatabaseOptions settings, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(settings.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        // The schema name is validated by DatabaseOptionsValidator, so it is safe to put it into the SQL text.
        await using var command = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS {settings.Schema}", connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
