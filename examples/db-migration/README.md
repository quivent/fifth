# Database Migration Tool

Schema versioning and data migration for SQLite.

## Features

- Track applied migrations in metadata table
- Execute SQL files in order
- Generate rollback scripts
- Migration status reporting
- Dry-run mode

## Usage

```bash
# Run pending migrations
./fifth examples/db-migration/main.fs migrate

# Check status
./fifth examples/db-migration/main.fs status

# Rollback last migration
./fifth examples/db-migration/main.fs rollback
```

## Structure

```
db-migration/
├── main.fs              # Entry point
├── migrations/          # Migration files
│   ├── 001_init.sql
│   ├── 002_add_users.sql
│   └── 003_add_index.sql
└── app.db               # Target database
```

## Migration Format

Each migration is a SQL file with up/down sections:
```sql
-- UP
CREATE TABLE users (...);

-- DOWN
DROP TABLE users;
```
