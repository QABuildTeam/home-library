using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeLibrary.Infrastructure.Persistence.Migrations;

/// <summary>
/// Builds a separate service container for FluentMigrator, so its services do not end up in the application container.
/// The migrations of this assembly get the database options through their constructors.
/// </summary>
internal static class MigrationServices
{
    /// <summary>
    /// Creates the FluentMigrator container for the given database.
    /// </summary>
    /// <exception cref="OptionsValidationException">The database options are invalid (for example, an unsafe schema name).</exception>
    public static ServiceProvider Create(DatabaseOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        // The schema name goes into SQL text, so it is checked here too, not only by the application options pipeline.
        var validation = new DatabaseOptionsValidator().Validate(Options.DefaultName, options);

        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(DatabaseOptions), validation.Failures);
        }

        return new ServiceCollection()
            .AddSingleton(loggerFactory)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddSingleton(Options.Create(options))
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(options.ConnectionString)
                .WithVersionTable(new LibraryVersionTableMetaData(options.Schema))
                .ScanIn(typeof(MigrationServices).Assembly).For.Migrations())
            .BuildServiceProvider(validateScopes: true);
    }
}
