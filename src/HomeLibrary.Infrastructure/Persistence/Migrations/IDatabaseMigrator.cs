namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Brings the database schema up to date.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Creates the library schema when it is missing and applies the migration scripts that have not been applied yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">A migration script failed.</exception>
    Task Migrate(CancellationToken cancellationToken);
}
