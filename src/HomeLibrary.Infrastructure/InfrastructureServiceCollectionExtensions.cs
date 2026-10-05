using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Infrastructure.Export;
using HomeLibrary.Infrastructure.Html;
using HomeLibrary.Infrastructure.Persistence;
using HomeLibrary.Infrastructure.Persistence.Books;
using HomeLibrary.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeLibrary.Infrastructure;

/// <summary>
/// Registers the infrastructure services: PostgreSQL access, repositories, migrations, the HTML converter
/// and the export file builder.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string PUBLIC_SCHEMA = "public";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services.AddSingleton(CreateDataSource);

        services.AddScoped<IBookReadRepository, BookReadRepository>();
        services.AddScoped<IBookWriteRepository, BookWriteRepository>();
        services.AddSingleton<ITableOfContentsConverter, HtmlTableOfContentsConverter>();
        services.AddSingleton<ITableOfContentsFileBuilder, XmlTableOfContentsFileBuilder>();
        services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();

        return services;
    }

    /// <summary>
    /// Creates the single data source of the application. The data source owns the connection pool, so repositories
    /// open a connection per call and return it to the pool on dispose.
    /// Pool defaults (override them in the connection string):
    /// Pooling=true; Minimum Pool Size=0; Maximum Pool Size=100; Connection Idle Lifetime=300 (seconds);
    /// Connection Pruning Interval=10 (seconds); Timeout=15 (seconds to wait for a connection, including from the pool).
    /// Keep the sum of Maximum Pool Size over all application instances below max_connections of the server.
    /// </summary>
    private static NpgsqlDataSource CreateDataSource(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var builder = new NpgsqlDataSourceBuilder(options.ConnectionString);

        // Stored routines are called without the schema prefix; pg_trgm lives in the public schema.
        builder.ConnectionStringBuilder.SearchPath = $"{options.Schema},{PUBLIC_SCHEMA}";
        builder.UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

        return builder.Build();
    }
}
