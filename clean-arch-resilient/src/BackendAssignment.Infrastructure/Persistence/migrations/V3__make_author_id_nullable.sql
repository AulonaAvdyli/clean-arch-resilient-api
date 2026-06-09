-- Allow books to be created without an author initially
ALTER TABLE library.books
    ALTER COLUMN author_id DROP NOT NULL;
