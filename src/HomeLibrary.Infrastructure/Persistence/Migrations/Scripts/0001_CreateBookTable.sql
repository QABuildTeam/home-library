-- Book table and search indexes.
-- $schema$ is substituted by DbUp with the configured schema name.

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;

CREATE TABLE $schema$.book
(
    id               bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title            varchar(500)  NOT NULL,
    author           varchar(300)  NOT NULL,
    publication_year integer       NULL,
    isbn             varchar(20)   NULL,
    publisher        varchar(300)  NULL,
    page_count       integer       NULL,
    genre            varchar(100)  NULL,
    notes            varchar(4000) NULL,
    toc              xml           NULL,
    toc_text         text          NULL,
    created_at       timestamptz   NOT NULL DEFAULT now(),
    updated_at       timestamptz   NOT NULL DEFAULT now(),
    version          integer       NOT NULL DEFAULT 1,

    CONSTRAINT ck_book_title_not_blank CHECK (btrim(title) <> ''),
    CONSTRAINT ck_book_author_not_blank CHECK (btrim(author) <> ''),
    CONSTRAINT ck_book_publication_year CHECK (publication_year >= 1),
    CONSTRAINT ck_book_page_count CHECK (page_count BETWEEN 1 AND 100000),
    CONSTRAINT ck_book_version CHECK (version >= 1)
);

COMMENT ON TABLE $schema$.book IS 'Books of the home library';
COMMENT ON COLUMN $schema$.book.toc IS 'Table of contents: XHTML fragment wrapped into the <toc> root element';
COMMENT ON COLUMN $schema$.book.toc_text IS 'Plain text of the table of contents for substring search; maintained by the book_* procedures';
COMMENT ON COLUMN $schema$.book.version IS 'Row version for optimistic concurrency control';

-- Trigram indexes speed up case-insensitive substring search (ILIKE '%text%').
CREATE INDEX ix_book_title_trgm ON $schema$.book USING gin (title public.gin_trgm_ops);
CREATE INDEX ix_book_author_trgm ON $schema$.book USING gin (author public.gin_trgm_ops);
CREATE INDEX ix_book_toc_text_trgm ON $schema$.book USING gin (toc_text public.gin_trgm_ops);

-- Supports the default list order.
CREATE INDEX ix_book_title_id ON $schema$.book (title, id);
