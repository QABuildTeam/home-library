using FluentMigrator;
using Microsoft.Extensions.Options;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Base class of the library migrations: gives the SQL the configured schema name.
/// FluentMigrator creates migrations through dependency injection, so the options come from the migration container.
/// The migrations are public because FluentMigrator discovers only exported types of the scanned assembly.
/// </summary>
/// <param name="options">Database settings; the schema name is validated by <see cref="DatabaseOptionsValidator"/>.</param>
public abstract class SchemaMigration(IOptions<DatabaseOptions> options) : Migration
{
    /// <summary>
    /// Schema that holds the library tables and stored routines.
    /// </summary>
    protected string SchemaName { get; } = options.Value.Schema;
}
