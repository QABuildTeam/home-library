using HomeLibrary.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

/// <summary>
/// Configuration of the database tests: the connection string comes from user secrets or the
/// <c>Database__ConnectionString</c> environment variable, the schema is a temporary one chosen by the test.
/// </summary>
internal static class TestConfiguration
{
    private const string SCHEMA_FORMAT = "N";

    public static string NewSchemaName(string prefix) => prefix + Guid.NewGuid().ToString(SCHEMA_FORMAT);

    public static IConfiguration Build(string schema) => new ConfigurationBuilder()
        .AddUserSecrets<DatabaseFixture>()
        .AddEnvironmentVariables()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DatabaseOptions.SECTION_NAME}:{nameof(DatabaseOptions.Schema)}"] = schema
        })
        .Build();

    public static DatabaseOptions BuildOptions(string schema)
    {
        var options = new DatabaseOptions();

        Build(schema).GetSection(DatabaseOptions.SECTION_NAME).Bind(options);

        return options;
    }
}
