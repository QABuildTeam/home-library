namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Brings the database schema up to date.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Creates the library schema when it is missing, imports the journal of the former DbUp migrations
    /// and applies the migrations that have not been applied yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A migration failed (the FluentMigrator error, with the PostgreSQL error inside, is the inner exception)
    /// or the DbUp journal contains an unknown script.
    /// </exception>
    Task Migrate(CancellationToken cancellationToken);
}
