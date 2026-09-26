using HomeLibrary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Web.Tests;

/// <summary>
/// Runs the web application in memory against a temporary schema of the configured database.
/// The connection string comes from the user secrets of the web project (the Development environment loads them).
/// The schema is created by the application migrations at startup and dropped after the tests.
/// </summary>
public sealed class HomeLibraryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SCHEMA_PREFIX = "library_webtest_";
    private const string SCHEMA_FORMAT = "N";

    private readonly string _schema = SCHEMA_PREFIX + Guid.NewGuid().ToString(SCHEMA_FORMAT);

    /// <summary>
    /// Starts the application (and its migrations) and makes sure the tests will not touch the working schema.
    /// </summary>
    public Task InitializeAsync()
    {
        var configuredSchema = Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.Schema;

        if (configuredSchema != _schema)
        {
            throw new InvalidOperationException(
                $"The test schema override did not apply: the application uses '{configuredSchema}' instead of '{_schema}'.");
        }

        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        var options = Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        await using (var connection = new NpgsqlConnection(options.ConnectionString))
        {
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {_schema} CASCADE", connection);

            await command.ExecuteNonQueryAsync();
        }

        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        // Added last, so it overrides the schema from appsettings.json.
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{DatabaseOptions.SECTION_NAME}:{nameof(DatabaseOptions.Schema)}"] = _schema
            }));
    }
}
