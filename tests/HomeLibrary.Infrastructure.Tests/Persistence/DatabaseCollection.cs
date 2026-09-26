namespace HomeLibrary.Infrastructure.Tests.Persistence;

/// <summary>
/// Shares one temporary database schema between all database test classes.
/// </summary>
[CollectionDefinition(NAME)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string NAME = "Database";
}
