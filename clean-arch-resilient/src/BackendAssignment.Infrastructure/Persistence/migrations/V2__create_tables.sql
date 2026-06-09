-- Create Categories Table
CREATE TABLE IF NOT EXISTS library.categories
(
    id
    SERIAL
    PRIMARY
    KEY,
    name
    VARCHAR
(
    100
) NOT NULL UNIQUE CHECK
(
    name
    <>
    ''
),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
    );

-- Create Authors Table
CREATE TABLE IF NOT EXISTS library.authors
(
    id
    SERIAL
    PRIMARY
    KEY,
    first_name
    VARCHAR
(
    100
) NOT NULL CHECK
(
    first_name
    <>
    ''
),
    last_name VARCHAR
(
    100
) NOT NULL CHECK
(
    last_name
    <>
    ''
),
    country VARCHAR
(
    100
),
    books_published INT DEFAULT 0 CHECK
(
    books_published
    >=
    0
),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
    );

-- Index for Faster Search
CREATE INDEX IF NOT EXISTS idx_authors_last_name ON library.authors(last_name);

-- Create Books Table with category_id reference
CREATE TABLE IF NOT EXISTS library.books
(
    id
    SERIAL
    PRIMARY
    KEY,
    title
    VARCHAR
(
    255
) NOT NULL CHECK
(
    title
    <>
    ''
),
    publication_date DATE NOT NULL,
    author_id INT NOT NULL,
    category_id INT,
    pages INT CHECK
(
    pages >
    0
),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_author_id FOREIGN KEY
(
    author_id
) REFERENCES library.authors
(
    id
) ON DELETE CASCADE,
    CONSTRAINT fk_category_id FOREIGN KEY
(
    category_id
) REFERENCES library.categories
(
    id
)
  ON DELETE SET NULL
    );

-- Indexes for Faster Search
CREATE INDEX IF NOT EXISTS idx_books_title ON library.books(title);
CREATE INDEX IF NOT EXISTS idx_books_publication_date ON library.books(publication_date);
CREATE INDEX IF NOT EXISTS idx_categories_name ON library.categories(name);
