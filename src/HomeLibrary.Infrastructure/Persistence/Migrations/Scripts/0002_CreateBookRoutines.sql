-- Stored routines: the application accesses the book table only through them.
-- Procedures change data, functions (RETURNS TABLE) read it: a PostgreSQL procedure cannot return a result set.
-- Error codes raised by the procedures:
--   HL404 - the book does not exist;
--   HL409 - the book was changed by another user (optimistic concurrency conflict).

-- Extracts the plain text of the table of contents for substring search.
CREATE OR REPLACE FUNCTION $schema$.book_toc_text(p_toc xml)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT replace(replace(replace(
               array_to_string(xpath('//text()', p_toc)::text[], ' '),
               '&lt;', '<'), '&gt;', '>'), '&amp;', '&');
$$;

CREATE OR REPLACE PROCEDURE $schema$.book_insert(
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
    INSERT INTO $schema$.book
        (title, author, publication_year, isbn, publisher, page_count, genre, notes, toc, toc_text)
    VALUES
        (p_title, p_author, p_publication_year, p_isbn, p_publisher, p_page_count, p_genre, p_notes,
         p_toc, $schema$.book_toc_text(p_toc))
    RETURNING id INTO p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE $schema$.book_update(
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
    UPDATE $schema$.book
       SET title            = p_title,
           author           = p_author,
           publication_year = p_publication_year,
           isbn             = p_isbn,
           publisher        = p_publisher,
           page_count       = p_page_count,
           genre            = p_genre,
           notes            = p_notes,
           toc              = p_toc,
           toc_text         = $schema$.book_toc_text(p_toc),
           updated_at       = now(),
           version          = version + 1
     WHERE id = p_id
       AND version = p_expected_version;

    IF NOT FOUND THEN
        IF EXISTS (SELECT 1 FROM $schema$.book WHERE id = p_id) THEN
            RAISE EXCEPTION 'Book % was modified by another user', p_id USING ERRCODE = 'HL409';
        END IF;

        RAISE EXCEPTION 'Book % not found', p_id USING ERRCODE = 'HL404';
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE $schema$.book_delete(p_id bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM $schema$.book WHERE id = p_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Book % not found', p_id USING ERRCODE = 'HL404';
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION $schema$.book_get(p_id bigint)
RETURNS TABLE
(
    id               bigint,
    title            varchar,
    author           varchar,
    publication_year integer,
    isbn             varchar,
    publisher        varchar,
    page_count       integer,
    genre            varchar,
    notes            varchar,
    toc              xml,
    created_at       timestamptz,
    updated_at       timestamptz,
    version          integer
)
LANGUAGE sql
STABLE
AS $$
    SELECT b.id, b.title, b.author, b.publication_year, b.isbn, b.publisher, b.page_count, b.genre, b.notes,
           b.toc, b.created_at, b.updated_at, b.version
      FROM $schema$.book b
     WHERE b.id = p_id;
$$;

-- Case-insensitive substring search in the selected attributes; an empty query returns all books.
-- total_count is the number of matching books on all pages.
CREATE OR REPLACE FUNCTION $schema$.book_search(
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
      FROM $schema$.book b
     CROSS JOIN search s
     WHERE coalesce(btrim(p_query), '') = ''
        OR (p_in_title AND b.title ILIKE s.pattern)
        OR (p_in_author AND b.author ILIKE s.pattern)
        OR (p_in_toc AND b.toc_text ILIKE s.pattern)
     ORDER BY b.title, b.id
    OFFSET p_offset
     LIMIT p_limit;
$$;
