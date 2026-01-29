---
title: sql.fs
parent: Libraries
nav_order: 3
---

# sql.fs — SQLite Interface

Query databases via `sqlite3` CLI.

## Quick Count

```forth
s" db.sqlite" s" SELECT COUNT(*) FROM users" sql-count .
```

## Iterate Results

```forth
s" db.sqlite" s" SELECT name, email FROM users" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type ."  <"
    2dup 1 sql-field type ." >" cr
    2drop
  else 2drop then
repeat 2drop
sql-close
```

## Words

| Word | Effect | Description |
|------|--------|-------------|
| `sql-exec` | `( db sql -- )` | Execute query |
| `sql-open` | `( -- )` | Start iteration |
| `sql-row?` | `( -- addr u flag )` | Next row |
| `sql-field` | `( row n -- field )` | Get nth field |
| `sql-close` | `( -- )` | Clean up |
| `sql-count` | `( db sql -- n )` | Single number result |
