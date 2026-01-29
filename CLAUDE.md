# CLAUDE.md - Fifth Project Context

## What Fifth Is

Fifth is a collection of practical Forth libraries for building real applications with Gforth. No external dependencies beyond Gforth and standard Unix tools (sqlite3).

**Name origin**: Forth -> Fifth (next generation). Bringing Forth into modern development.

## Project Structure

```
~/fifth/
├── lib/
│   ├── str.fs          String buffers, parsing, field extraction
│   ├── html.fs         HTML5 tags, escaping, document structure
│   ├── sql.fs          SQLite shell interface, result iteration
│   ├── template.fs     Slots, conditional rendering, layouts
│   ├── ui.fs           Cards, badges, tabs, grids, dashboards
│   └── core.fs         Loads str + html + sql + utilities
├── examples/
│   ├── db-viewer.fs    Dual-database HTML viewer
│   └── project-dashboard.fs  Tabbed dashboard with panels
├── README.md
└── CLAUDE.md
```

## Core Principles

1. **No dynamic allocation** - Use static buffers (`str-reset` / `str+` / `str$`), never `allocate`/`free`
2. **Shell-out pattern** - No C bindings. SQLite via `sqlite3` CLI, file open via `open` command
3. **HTML escaping by default** - `text` escapes, `raw` bypasses. Never use `raw` for user data
4. **Stack comments everywhere** - Every word needs `( before -- after )` documentation
5. **Composable words** - Small words that combine. No monolithic definitions

## Critical Forth Knowledge

### Things That Will Break You

- **Word spacing**: `</div>nl` is ONE undefined word. `</div> nl` is TWO words. Forth tokenizes on whitespace only.
- **`s"` has no escapes**: Use `s\"` for embedded quotes (`s\" ...\"...\"..."`). Standard `s"` treats backslash as literal.
- **No `popen` on macOS**: Gforth's `unix/pipe.fs` doesn't exist. Shell out to temp files instead.
- **`s+` crashes**: Dynamic string concatenation causes memory errors. Always use buffer pattern.
- **Stack errors = cryptic crashes**: "Invalid memory address" usually means a stack imbalance. Add `.s` calls to debug.
- **SQL single quotes**: Shell quoting uses single quotes around the SQL. SQL string literals inside conflict. Avoid `WHERE col='value'`; use numeric comparisons, ORDER BY, or parameter workarounds.

### Buffer System

Two independent buffers to avoid conflicts:

| Buffer | Words | Used By |
|--------|-------|---------|
| Primary (`str-buf`) | `str-reset` `str+` `str$` `str-char` | General string building, CSS classes, shell commands |
| Secondary (`str2-buf`) | `str2-reset` `str2+` `str2$` | `html-escape` (so escaping doesn't corrupt primary buffer) |

**Rule**: Never nest operations on the same buffer. If you need to build a string inside `html-escape`, use the primary buffer (html-escape uses secondary).

### Stack Discipline

```forth
\ WRONG - loses items
: bad  ( a b c -- ) swap drop ;  \ What happened to a?

\ RIGHT - document everything
: good ( addr u n -- addr u field-addr field-u )
  \ Extract nth field from pipe-delimited string
  ... ;
```

Common patterns:
- `2>r ... 2r>` - Save/restore string pair on return stack
- `2swap` - Exchange two string pairs: `( a1 u1 a2 u2 -- a2 u2 a1 u1 )`
- `2dup` - Copy top string pair: `( a u -- a u a u )`
- `2drop` - Discard string pair: `( a u -- )`
- `-rot` vs `swap` - Triple rotation vs pair swap. Getting these wrong causes null pointer crashes.

## Library Dependencies

```
str.fs          (standalone)
html.fs     --> str.fs
sql.fs      --> str.fs
template.fs --> html.fs --> str.fs
ui.fs       --> html.fs, template.fs
core.fs     --> str.fs, html.fs, sql.fs
```

## Commands

```bash
# Run examples
gforth ~/fifth/examples/db-viewer.fs
gforth ~/fifth/examples/project-dashboard.fs

# Test a library interactively
gforth ~/fifth/lib/core.fs
# Then type Forth words at the prompt

# Run with stack trace on error
gforth -e "include ~/fifth/examples/project-dashboard.fs"
```

## HTML Output Pattern

All examples follow this pattern:

```forth
s" /tmp/output.html" w/o create-file throw html>file

s" Page Title" html-head    \ Opens <!DOCTYPE>, <html>, <head>, <title>
  <style> ... </style>      \ CSS while head is still open
  ui-css                    \ Component styles
html-body                   \ Closes </head>, opens <body>

  \ ... page content ...

ui-js                       \ Tab switching JavaScript
html-end                    \ Closes </body></html>

html-fid @ close-file throw
```

**Key**: `html-head` leaves `<head>` open so you can inject `<style>` blocks. `html-body` closes it.

## SQL Query Pattern

```forth
s" path/to/db.db" s" SELECT col1, col2 FROM table" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type    \ first column
    2dup 1 sql-field type    \ second column
    2drop                    \ drop the row string
  else 2drop then
repeat 2drop
sql-close
```

Results are pipe-delimited. `sql-field` extracts by 0-based index.

## Conventions

- Library files go in `lib/`
- Example applications go in `examples/`
- Every `.fs` file starts with a comment block: `\ fifth/path/file.fs - Description`
- Use `require` not `include` (prevents double-loading)
- CSS class names use kebab-case: `stat-card`, `grid-auto`, `bg-primary`
- Word names follow Forth convention: `<tag>`, `</tag>`, `tag.` (dot = convenience with content)

## What NOT To Do

- Don't use `allocate`/`free` for strings
- Don't try to `include` the same file twice (use `require`)
- Don't put single-quoted SQL literals in shell commands
- Don't assume `s"` strings persist after the word returns (they're transient)
- Don't redefine standard Gforth words (`emit-file`, `type`, etc.)
- Don't create words with embedded whitespace (impossible in Forth)
