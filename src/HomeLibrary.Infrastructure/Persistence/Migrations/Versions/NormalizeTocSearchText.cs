using FluentMigrator;
using Microsoft.Extensions.Options;

namespace HomeLibrary.Infrastructure.Persistence.Migrations.Versions;

/// <summary>
/// Phrase search in the table of contents (formerly the DbUp script 0003_NormalizeTocSearchText.sql).
/// The previous version joined the text nodes with a space, so "&lt;strong&gt;Chapter&lt;/strong&gt; 3" became
/// "Chapter  3" (two spaces) and the phrase "Chapter 3" was not found. Both the stored text and the search text are now
/// normalized: no-break spaces become ordinary spaces and whitespace runs collapse into one space. Text nodes are still
/// joined with a space, so that neighbouring blocks ("Part 1" and "Chapter 1") stay separate words.
/// </summary>
[Migration(MigrationVersions.NORMALIZE_TOC_SEARCH_TEXT)]
public sealed class NormalizeTocSearchText(IOptions<DatabaseOptions> options) : SchemaMigration(options)
{
    public override void Up()
    {
        Execute.Sql($"""
            -- Collapses whitespace and no-break spaces into single ordinary spaces.
            CREATE OR REPLACE FUNCTION {SchemaName}.normalize_search_text(p_text text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$
                SELECT btrim(regexp_replace(replace(p_text, chr(160), ' '), '\s+', ' ', 'g'));
            $$;

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

            CREATE OR REPLACE FUNCTION {SchemaName}.book_search(
                p_query     text,
                p_in_title  boolean,
                p_in_author boolean,
                p_in_toc    boolean,
                p_offset    integer,
                p_limit     integer)
            RETURNS TABLE
            (
                id               bigint,
                title            varchar,
                author           varchar,
                publication_year integer,
                genre            varchar,
                total_count      bigint
            )
            LANGUAGE sql
            STABLE
            AS $$
                WITH search AS
                (
                    -- Escape LIKE wildcards so that the user text is matched literally.
                    SELECT '%' || replace(replace(replace({SchemaName}.normalize_search_text(p_query),
                                                          '\', '\\'), '%', '\%'), '_', '\_') || '%' AS pattern
                )
                SELECT b.id, b.title, b.author, b.publication_year, b.genre, count(*) OVER () AS total_count
                  FROM {SchemaName}.book b
                 CROSS JOIN search s
                 WHERE coalesce({SchemaName}.normalize_search_text(p_query), '') = ''
                    OR (p_in_title AND b.title ILIKE s.pattern)
                    OR (p_in_author AND b.author ILIKE s.pattern)
                    OR (p_in_toc AND b.toc_text ILIKE s.pattern)
                 ORDER BY b.title, b.id
                OFFSET p_offset
                 LIMIT p_limit;
            $$;

            -- Recalculate the search text of the existing books.
            UPDATE {SchemaName}.book
               SET toc_text = {SchemaName}.book_toc_text(toc)
             WHERE toc IS NOT NULL;
            """);
    }

    public override void Down()
    {
        // Restores the routines of CreateBookRoutines and the search text they produce.
        Execute.Sql($"""
            CREATE OR REPLACE FUNCTION {SchemaName}.book_toc_text(p_toc xml)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$
                SELECT replace(replace(replace(
                           array_to_string(xpath('//text()', p_toc)::text[], ' '),
                           '&lt;', '<'), '&gt;', '>'), '&amp;', '&');
            $$;

            CREATE OR REPLACE FUNCTION {SchemaName}.book_search(
                p_query     text,
                p_in_title  boolean,
                p_in_author boolean,
                p_in_toc    boolean,
                p_offset    integer,
                p_limit     integer)
            RETURNS TABLE
            (
                id               bigint,
                title            varchar,
                author           varchar,
                publication_year integer,
                genre            varchar,
                total_count      bigint
            )
            LANGUAGE sql
            STABLE
            AS $$
                WITH search AS
                (
                    -- Escape LIKE wildcards so that the user text is matched literally.
                    SELECT '%' || replace(replace(replace(btrim(p_query), '\', '\\'), '%', '\%'), '_', '\_') || '%' AS pattern
                )
                SELECT b.id, b.title, b.author, b.publication_year, b.genre, count(*) OVER () AS total_count
                  FROM {SchemaName}.book b
                 CROSS JOIN search s
                 WHERE coalesce(btrim(p_query), '') = ''
                    OR (p_in_title AND b.title ILIKE s.pattern)
                    OR (p_in_author AND b.author ILIKE s.pattern)
                    OR (p_in_toc AND b.toc_text ILIKE s.pattern)
                 ORDER BY b.title, b.id
                OFFSET p_offset
                 LIMIT p_limit;
            $$;

            UPDATE {SchemaName}.book
               SET toc_text = {SchemaName}.book_toc_text(toc)
             WHERE toc IS NOT NULL;

            DROP FUNCTION {SchemaName}.normalize_search_text(text);
            """);
    }
}
