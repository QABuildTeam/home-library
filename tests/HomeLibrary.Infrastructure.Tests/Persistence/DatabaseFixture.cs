using HomeLibrary.Infrastructure.Persistence;
using HomeLibrary.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

/// <summary>
/// Creates a temporary schema in the configured database, applies the migrations to it and drops it after the tests.
/// The connection string is read from user secrets or the <c>Database__ConnectionString</c> environment variable.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string SCHEMA_PREFIX = "library_test_";
    private const string SCHEMA_FORMAT = "N";

    private readonly string _schema = SCHEMA_PREFIX + Guid.NewGuid().ToString(SCHEMA_FORMAT);

    private ServiceProvider? _services;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("The fixture is not initialized.");

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DatabaseFixture>()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DatabaseOptions.SECTION_NAME}:{nameof(DatabaseOptions.Schema)}"] = _schema
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddInfrastructure(configuration);

        _services = services.BuildServiceProvider(validateScopes: true);

        await _services.GetRequiredService<IDatabaseMigrator>().Migrate(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (_services is null)
        {
            return;
        }

        var options = _services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        await using (var connection = new NpgsqlConnection(options.ConnectionString))
        {
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {_schema} CASCADE", connection);

            await command.ExecuteNonQueryAsync();
        }

        await _services.DisposeAsync();
    }
}
