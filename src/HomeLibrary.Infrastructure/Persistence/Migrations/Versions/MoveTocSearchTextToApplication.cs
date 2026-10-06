using Dapper;
using FluentMigrator;
using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;
using HomeLibrary.Infrastructure.Persistence.Books;
using Microsoft.Extensions.Options;

namespace HomeLibrary.Infrastructure.Persistence.Migrations.Versions;

/// <summary>
/// The search text of the table of contents (<c>toc_text</c>) is built by the application instead of the database.
/// The SQL function book_toc_text joined all text nodes with a space, so a word split by formatting
/// ("Intro&lt;em&gt;duction&lt;/em&gt;") was not found; the application knows which elements are inline
/// (see TableOfContentsSearchText). book_insert and book_update take the ready text in a new p_toc_text parameter,
/// book_toc_text is dropped, and the search text of the existing books is rebuilt by the application code.
/// The application is the only valid source of toc_text from now on.
/// </summary>
[Migration(MigrationVersions.MOVE_TOC_SEARCH_TEXT_TO_APPLICATION)]
public sealed class MoveTocSearchTextToApplication(IOptions<DatabaseOptions> options) : SchemaMigration(options)
{
    public override void Up()
    {
        Execute.Sql($"""
            -- The signatures change, so the old procedures are dropped: CREATE OR REPLACE would add overloads.
            DROP PROCEDURE {SchemaName}.book_insert(varchar, varchar, integer, varchar, varchar, integer, varchar, varchar, xml,
                bigint);
            DROP PROCEDURE {SchemaName}.book_update(bigint, integer, varchar, varchar, integer, varchar, varchar, integer, varchar,
                varchar, xml);

            CREATE PROCEDURE {SchemaName}.book_insert(
                p_title            varchar,
                p_author           varchar,
                p_publication_year integer,
                p_isbn             varchar,
                p_publisher        varchar,
                p_page_count       integer,
                p_genre            varchar,
                p_notes            varchar,
                p_toc              xml,
                p_toc_text         text,
                INOUT p_id         bigint DEFAULT NULL)
            LANGUAGE plpgsql
            AS $$
            BEGIN
                INSERT INTO {SchemaName}.book
                    (title, author, publication_year, isbn, publisher, page_count, genre, notes, toc, toc_text)
                VALUES
                    (p_title, p_author, p_publication_year, p_isbn, p_publisher, p_page_count, p_genre, p_notes,
                     p_toc, p_toc_text)
                RETURNING id INTO p_id;
            END;
            $$;

            CREATE PROCEDURE {SchemaName}.book_update(
                p_id               bigint,
                p_expected_version integer,
                p_title            varchar,
                p_author           varchar,
                p_publication_year integer,
                p_isbn             varchar,
                p_publisher        varchar,
                p_page_count       integer,
                p_genre            varchar,
                p_notes            varchar,
                p_toc              xml,
                p_toc_text         text)
            LANGUAGE plpgsql
            AS $$
            BEGIN
                UPDATE {SchemaName}.book
                   SET title            = p_title,
                       author           = p_author,
                       publication_year = p_publication_year,
                       isbn             = p_isbn,
                       publisher        = p_publisher,
                       page_count       = p_page_count,
                       genre            = p_genre,
                       notes            = p_notes,
                       toc              = p_toc,
                       toc_text         = p_toc_text,
                       updated_at       = now(),
                       version          = version + 1
                 WHERE id = p_id
                   AND version = p_expected_version;

                IF NOT FOUND THEN
                    IF EXISTS (SELECT 1 FROM {SchemaName}.book WHERE id = p_id) THEN
                        RAISE EXCEPTION 'Book % was modified by another user', p_id USING ERRCODE = 'HL409';
                    END IF;

                    RAISE EXCEPTION 'Book % not found', p_id USING ERRCODE = 'HL404';
                END IF;
            END;
            $$;

            DROP FUNCTION {SchemaName}.book_toc_text(xml);

            COMMENT ON COLUMN {SchemaName}.book.toc_text
                IS 'Plain text of the table of contents for substring search; built by the application';
            """);

        // Rebuild the search text of the existing books in the same transaction, with the extraction code of the
        // application. A database migrated later gets the rules of the code at that time; databases that already
        // applied this migration need a new migration when the rules change.
        Execute.WithConnection((connection, transaction) =>
        {
            var books = connection.Query<(long Id, string Toc)>(
                $"SELECT id, toc::text FROM {SchemaName}.book WHERE toc IS NOT NULL",
                transaction: transaction);

            // Dapper executes the command once for every element of the parameter sequence.
            connection.Execute(
                $"UPDATE {SchemaName}.book SET toc_text = @TocText WHERE id = @Id",
                books.Select(book => new { book.Id, TocText = BuildSearchText(book.Id, book.Toc) }),
                transaction);
        });
    }

    public override void Down()
    {
        // Restores the routines of NormalizeTocSearchText and CreateBookRoutines and the search text they produce.
        Execute.Sql($"""
            CREATE OR REPLACE FUNCTION {SchemaName}.book_toc_text(p_toc xml)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$
                SELECT {SchemaName}.normalize_search_text(
                           replace(replace(replace(
                               array_to_string(xpath('//text()', p_toc)::text[], ' '),
                               '&lt;', '<'), '&gt;', '>'), '&amp;', '&'));
            $$;

            DROP PROCEDURE {SchemaName}.book_insert(varchar, varchar, integer, varchar, varchar, integer, varchar, varchar, xml,
                text, bigint);
            DROP PROCEDURE {SchemaName}.book_update(bigint, integer, varchar, varchar, integer, varchar, varchar, integer, varchar,
                varchar, xml, text);

            CREATE PROCEDURE {SchemaName}.book_insert(
                p_title            varchar,
                p_author           varchar,
                p_publication_year integer,
                p_isbn             varchar,
                p_publisher        varchar,
                p_page_count       integer,
                p_genre            varchar,
                p_notes            varchar,
                p_toc              xml,
                INOUT p_id         bigint DEFAULT NULL)
            LANGUAGE plpgsql
            AS $$
            BEGIN
                INSERT INTO {SchemaName}.book
                    (title, author, publication_year, isbn, publisher, page_count, genre, notes, toc, toc_text)
                VALUES
                    (p_title, p_author, p_publication_year, p_isbn, p_publisher, p_page_count, p_genre, p_notes,
                     p_toc, {SchemaName}.book_toc_text(p_toc))
                RETURNING id INTO p_id;
            END;
            $$;

            CREATE PROCEDURE {SchemaName}.book_update(
                p_id               bigint,
                p_expected_version integer,
                p_title            varchar,
                p_author           varchar,
                p_publication_year integer,
                p_isbn             varchar,
                p_publisher        varchar,
                p_page_count       integer,
                p_genre            varchar,
                p_notes            varchar,
                p_toc              xml)
            LANGUAGE plpgsql
            AS $$
            BEGIN
                UPDATE {SchemaName}.book
                   SET title            = p_title,
                       author           = p_author,
                       publication_year = p_publication_year,
                       isbn             = p_isbn,
                       publisher        = p_publisher,
                       page_count       = p_page_count,
                       genre            = p_genre,
                       notes            = p_notes,
                       toc              = p_toc,
                       toc_text         = {SchemaName}.book_toc_text(p_toc),
                       updated_at       = now(),
                       version          = version + 1
                 WHERE id = p_id
                   AND version = p_expected_version;

                IF NOT FOUND THEN
                    IF EXISTS (SELECT 1 FROM {SchemaName}.book WHERE id = p_id) THEN
                        RAISE EXCEPTION 'Book % was modified by another user', p_id USING ERRCODE = 'HL409';
                    END IF;

                    RAISE EXCEPTION 'Book % not found', p_id USING ERRCODE = 'HL404';
                END IF;
            END;
            $$;

            COMMENT ON COLUMN {SchemaName}.book.toc_text
                IS 'Plain text of the table of contents for substring search; maintained by the book_* procedures';

            UPDATE {SchemaName}.book
               SET toc_text = {SchemaName}.book_toc_text(toc)
             WHERE toc IS NOT NULL;
            """);
    }

    /// <summary>
    /// The xml column accepts fragments and any root element, so a row changed outside the application may not be
    /// a valid table of contents; the migration then stops and names the book.
    /// </summary>
    private static string? BuildSearchText(long bookId, string toc)
    {
        try
        {
            return TableOfContentsSearchText.Extract(new TableOfContents(toc));
        }
        catch (DomainValidationException exception)
        {
            throw new InvalidOperationException($"Cannot rebuild the table of contents search text of book {bookId}.", exception);
        }
    }
}
