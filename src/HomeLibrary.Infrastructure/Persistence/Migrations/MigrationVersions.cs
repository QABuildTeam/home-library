namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Versions of the FluentMigrator migrations in the <c>yyyyMMddHHmmss</c> format. The first three are labels assigned
/// when moving from DbUp: they keep the order of the DbUp scripts and approximately date them. New migrations use their
/// creation time. A version must never change once applied: it identifies the migration in the version table.
/// </summary>
internal static class MigrationVersions
{
    public const long CREATE_BOOK_TABLE = 20260925232000;
    public const long CREATE_BOOK_ROUTINES = 20260925232100;
    public const long NORMALIZE_TOC_SEARCH_TEXT = 20260926113000;
}
