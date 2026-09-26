using Dapper;
using HomeLibrary.Application.Books;
using HomeLibrary.Application.Common;
using HomeLibrary.Domain.Books;
using Npgsql;

namespace HomeLibrary.Infrastructure.Persistence.Books;

/// <summary>
/// Reads books through the <c>book_get</c> and <c>book_search</c> stored functions.
/// </summary>
/// <param name="dataSource">Connection source; its search path points to the library schema.</param>
public sealed class BookReadRepository(NpgsqlDataSource dataSource) : IBookReadRepository
{
    // Column aliases match the row class properties, so Dapper needs no global naming convention.
    private const string GET_SQL = """
        SELECT id               AS "Id",
               title            AS "Title",
               author           AS "Author",
               publication_year AS "PublicationYear",
               isbn             AS "Isbn",
               publisher        AS "Publisher",
               page_count       AS "PageCount",
               genre            AS "Genre",
               notes            AS "Notes",
               toc              AS "Toc",
               created_at       AS "CreatedAt",
               updated_at       AS "UpdatedAt",
               version          AS "Version"
          FROM book_get(@Id)
        """;

    private const string SEARCH_SQL = """
        SELECT id               AS "Id",
               title            AS "Title",
               author           AS "Author",
               publication_year AS "PublicationYear",
               genre            AS "Genre",
               total_count      AS "TotalCount"
          FROM book_search(@Text, @InTitle, @InAuthor, @InTableOfContents, @Offset, @Limit)
        """;

    public async Task<Book?> Get(long id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(GET_SQL, new { Id = id }, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<BookRow>(command);

        return row?.ToBook();
    }

    public async Task<PagedResult<BookListItem>> Search(BookSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var parameters = new
        {
            criteria.Text,
            InTitle = criteria.Scope.HasFlag(BookSearchScope.Title),
            InAuthor = criteria.Scope.HasFlag(BookSearchScope.Author),
            InTableOfContents = criteria.Scope.HasFlag(BookSearchScope.TableOfContents),
            criteria.Offset,
            Limit = criteria.PageSize
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(SEARCH_SQL, parameters, cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<BookListRow>(command)).AsList();

        var totalCount = rows.Count > 0 ? (int)rows[0].TotalCount : 0;
        var items = rows
            .Select(row => row.ToListItem())
            .ToList();

        return new PagedResult<BookListItem>(items, totalCount, criteria.Page, criteria.PageSize);
    }
}
