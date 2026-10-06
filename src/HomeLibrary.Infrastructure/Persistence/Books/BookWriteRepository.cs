using Dapper;
using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Domain.Books;
using Npgsql;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Changes books through the <c>book_insert</c>, <c>book_update</c> and <c>book_delete</c> stored procedures.
/// </summary>
/// <param name="dataSource">Connection source; its search path points to the library schema.</param>
internal sealed class BookWriteRepository(NpgsqlDataSource dataSource) : IBookWriteRepository
{
    private const string INSERT_SQL = """
        CALL book_insert(@Title, @Author, @PublicationYear, @Isbn, @Publisher, @PageCount, @Genre, @Notes,
                         @TableOfContents::xml, @TableOfContentsText, NULL)
        """;

    private const string UPDATE_SQL = """
        CALL book_update(@Id, @ExpectedVersion, @Title, @Author, @PublicationYear, @Isbn, @Publisher, @PageCount,
                         @Genre, @Notes, @TableOfContents::xml, @TableOfContentsText)
        """;

    private const string DELETE_SQL = "CALL book_delete(@Id)";

    public async Task<long> Add(BookDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(INSERT_SQL, ToParameters(details), cancellationToken: cancellationToken);

        // CALL returns a single row with the INOUT parameter p_id.
        return await connection.ExecuteScalarAsync<long>(command);
    }

    public async Task Update(long id, int expectedVersion, BookDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var parameters = new DynamicParameters(ToParameters(details));

        parameters.Add("Id", id);
        parameters.Add("ExpectedVersion", expectedVersion);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(UPDATE_SQL, parameters, cancellationToken: cancellationToken);

        await ExecuteMappingErrors(connection, command, id);
    }

    public async Task Delete(long id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(DELETE_SQL, new { Id = id }, cancellationToken: cancellationToken);

        await ExecuteMappingErrors(connection, command, id);
    }

    private static object ToParameters(BookDetails details) => new
    {
        details.Title,
        details.Author,
        details.PublicationYear,
        details.Isbn,
        details.Publisher,
        details.PageCount,
        details.Genre,
        details.Notes,
        TableOfContents = details.TableOfContents?.Xml,

        // The application is the only source of the search text (see TableOfContentsSearchText).
        TableOfContentsText = TableOfContentsSearchText.Extract(details.TableOfContents)
    };

    private static async Task ExecuteMappingErrors(NpgsqlConnection connection, CommandDefinition command, long bookId)
    {
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (PostgresException exception) when (exception.SqlState == DatabaseErrorCodes.BOOK_NOT_FOUND)
        {
            throw new BookNotFoundException(bookId);
        }
        catch (PostgresException exception) when (exception.SqlState == DatabaseErrorCodes.BOOK_CONCURRENCY_CONFLICT)
        {
            throw new BookConcurrencyException(bookId);
        }
    }
}
