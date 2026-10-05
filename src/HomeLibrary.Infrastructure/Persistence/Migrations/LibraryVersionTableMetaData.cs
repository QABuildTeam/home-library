using FluentMigrator.Runner.VersionTableInfo;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Table of applied FluentMigrator migrations: <c>version_info</c> in the library schema, snake_case names.
/// The schema itself is created by <see cref="DatabaseMigrator"/>, so FluentMigrator does not own it.
/// </summary>
/// <param name="schema">Library schema.</param>
internal sealed class LibraryVersionTableMetaData(string schema) : IVersionTableMetaData
{
    public const string TABLE_NAME = "version_info";
    public const string VERSION_COLUMN = "version";
    public const string APPLIED_ON_COLUMN = "applied_on";
    public const string DESCRIPTION_COLUMN = "description";

    private const string UNIQUE_INDEX_NAME = "uc_version_info_version";

    public bool OwnsSchema => false;

    public string SchemaName { get; } = schema;

    public string TableName => TABLE_NAME;

    public string ColumnName => VERSION_COLUMN;

    public string DescriptionColumnName => DESCRIPTION_COLUMN;

    public string UniqueIndexName => UNIQUE_INDEX_NAME;

    public string AppliedOnColumnName => APPLIED_ON_COLUMN;

    public bool CreateWithPrimaryKey => false;
}
