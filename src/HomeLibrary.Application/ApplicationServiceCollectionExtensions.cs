using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Commands;
using HomeLibrary.Application.Books.Queries;
using HomeLibrary.Application.Common;
using HomeLibrary.Domain.Books;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeLibrary.Application;

/// <summary>
/// Registers the application layer services: use case handlers and domain services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IBookDetailsValidator, BookDetailsValidator>();
        services.AddScoped<IBookDetailsFactory, BookDetailsFactory>();

        services.AddScoped<ICommandHandler<CreateBookCommand, long>, CreateBookHandler>();
        services.AddScoped<ICommandHandler<UpdateBookCommand>, UpdateBookHandler>();
        services.AddScoped<ICommandHandler<DeleteBookCommand>, DeleteBookHandler>();

        services.AddScoped<IQueryHandler<GetBookQuery, BookView>, GetBookHandler>();
        services.AddScoped<IQueryHandler<SearchBooksQuery, PagedResult<BookListItem>>, SearchBooksHandler>();
        services.AddScoped<IQueryHandler<ExportTableOfContentsQuery, TableOfContentsFile>, ExportTableOfContentsHandler>();

        return services;
    }
}
