---
layout: default
title: sql.fs - SQLite Interface
---

# sql.fs — SQLite Interface

Query SQLite databases via the `sqlite3` CLI.

```forth
require ~/.fifth/lib/pkg.fs
use lib:sql.fs
```

## Quick Count

```forth
s" users.db" s" SELECT COUNT(*) FROM users" sql-count .
\ 42
```

## Query and Iterate

```forth
s" users.db" s" SELECT name, email FROM users" sql-exec
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

Output:
```
Alice <alice@example.com>
Bob <bob@example.com>
```

## Words

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-exec` | `( db-addr db-u sql-addr sql-u -- )` | Execute query, store result |
| `sql-open` | `( -- )` | Prepare to iterate results |
| `sql-row?` | `( -- addr u flag )` | Get next row |
| `sql-field` | `( addr u n -- field-addr field-u )` | Extract nth field (0-indexed) |
| `sql-close` | `( -- )` | Clean up |
| `sql-count` | `( db-addr db-u sql-addr sql-u -- n )` | Execute and return single number |

## How It Works

1. `sql-exec` runs `sqlite3 -separator '|' DB "SQL"` via shell
2. Results are captured as pipe-delimited text
3. `sql-field` parses pipe-delimited fields

## Gotchas

### Single Quotes

SQL uses single quotes. Shell commands use single quotes. Conflict:

```forth
\ PROBLEMATIC
s" SELECT * FROM users WHERE name='Alice'" 
```

Workarounds:
- Use numeric comparisons
- Use LIKE with double quotes
- Pass parameters via temp table

### Results Are Strings

All fields come back as strings. Parse numbers:

```forth
2dup 0 sql-field s>number? 2drop  \ Convert to number
```

## Example: Generate HTML Table

```forth
: user-table ( -- )
  <table>
    <tr> s" Name" th. s" Email" th. </tr>
    s" users.db" s" SELECT name, email FROM users" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if
        <tr>
          2dup 0 sql-field td.
          2dup 1 sql-field td.
        </tr>
        2drop
      else 2drop then
    repeat 2drop
    sql-close
  </table> ;
```

[Back to Wiki](../)
