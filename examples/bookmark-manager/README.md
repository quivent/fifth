# Bookmark Manager

Personal knowledge management with bookmarks and notes.

## Features

- Store links and notes in SQLite
- Full-text search via FTS5
- Tag-based organization
- Generate browsable HTML export
- Import/export bookmarks
- Markdown notes support

## Usage

```bash
# Add bookmark
./fifth examples/bookmark-manager/main.fs add "https://example.com" "Example Site" "reference,web"

# Search bookmarks
./fifth examples/bookmark-manager/main.fs search "programming"

# List by tag
./fifth examples/bookmark-manager/main.fs tag "reference"

# Export to HTML
./fifth examples/bookmark-manager/main.fs export
```

## Structure

```
bookmark-manager/
├── main.fs          # Entry point
├── search.fs        # FTS5 queries
├── export.fs        # HTML generation
├── bookmarks.db     # SQLite database
└── output/
    └── bookmarks.html
```

## Database Schema

```sql
CREATE TABLE bookmarks (id, url, title, description, tags, created_at);
CREATE VIRTUAL TABLE bookmarks_fts USING fts5(title, description, tags);
```
