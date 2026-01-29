# Fifth

**Forth Libraries for the Fifth Age**

A collection of practical Forth libraries for building real applications. No external dependencies beyond Gforth and standard Unix tools.

## Philosophy

- **Minimal dependencies**: Shell out to existing tools rather than require C bindings
- **Proper escaping**: Security by default (HTML entities, SQL quoting)
- **Stack-based DSLs**: Build vocabularies that make intent clear
- **Composable**: Small words that combine into larger patterns
- **16x lighter**: 4.6 MB runtime vs 76 MB for Python

## Requirements

[Gforth](https://gforth.org/) and `sqlite3` CLI.

```bash
# macOS
brew install gforth sqlite

# Linux
apt install gforth sqlite3
```

## Project Structure

```
~/fifth/
├── lib/
│   ├── str.fs          147 lines  String buffers, parsing, field extraction
│   ├── html.fs         336 lines  HTML5 tags, escaping, document structure
│   ├── sql.fs          152 lines  SQLite shell interface, result iteration
│   ├── template.fs     123 lines  Slots, conditional rendering, layouts
│   ├── ui.fs           261 lines  Cards, badges, tabs, grids, dashboards
│   └── core.fs          67 lines  Loads all libraries + utilities
├── examples/
│   ├── db-viewer.fs    213 lines  Dual-database HTML viewer
│   └── project-dashboard.fs
│                       283 lines  Tabbed dashboard with panels
└── README.md
    Total:             1,582 lines
```

## Libraries

### str.fs - String Utilities

Buffer-based string operations without dynamic allocation.

```forth
require ~/fifth/lib/str.fs

\ Build strings in a static buffer (no malloc)
str-reset
s" Hello, " str+
s" World!" str+
str$ type  \ prints: Hello, World!

\ Second buffer for nested operations
str2-reset
s" nested" str2+
str2$ type

\ Parse delimited data (pipe, tab, comma)
s" apple|banana|cherry" 1 parse-pipe type  \ prints: banana
s" one,two,three" 2 parse-comma type       \ prints: three

\ Number to string
42 n>str type  \ prints: 42

\ String comparison
s" hello" s" hello" str= .  \ prints: -1 (true)
```

**Key words:**

| Word | Stack | Description |
|------|-------|-------------|
| `str-reset` | ( -- ) | Clear primary buffer |
| `str+` | ( addr u -- ) | Append to buffer |
| `str$` | ( -- addr u ) | Get buffer contents |
| `str-char` | ( c -- ) | Append single character |
| `str2-reset` | ( -- ) | Clear secondary buffer |
| `str2+` | ( addr u -- ) | Append to secondary |
| `parse-pipe` | ( addr u n -- addr u field$ ) | Extract nth pipe-delimited field |
| `parse-comma` | ( addr u n -- addr u field$ ) | Extract nth comma-delimited field |
| `parse-tab` | ( addr u n -- addr u field$ ) | Extract nth tab-delimited field |
| `n>str` | ( n -- addr u ) | Number to string |
| `str=` | ( a1 u1 a2 u2 -- flag ) | String equality |

---

### html.fs - HTML Generation

Full HTML5 tag vocabulary with automatic XSS-safe escaping.

```forth
require ~/fifth/lib/html.fs

\ Set output target
s" /tmp/page.html" w/o create-file throw html>file

\ Document with styles
s" My Page" html-head       \ opens <!DOCTYPE>, <html>, <head>, <title>
  <style> s" body" s" background:#000;color:#fff" css-rule </style>
html-body                    \ closes </head>, opens <body>

  s" Hello World" h1.
  s" container" <div.>
    s" This <script> is escaped & safe" p.    \ auto-escaped!
    s" Click me" s" https://example.com" a.   \ link helper
  </div>nl

html-end                     \ closes </body></html>
html-fid @ close-file throw
```

**Escaping**: `text` automatically converts `< > & ' "` to HTML entities. Use `raw` to output trusted HTML.

**Tag patterns:**

```forth
\ Simple tags
<div> ... </div>              \ basic open/close
s" card" <div.> ... </div>nl  \ with class attribute
s" my-id" <div#> ... </div>   \ with id attribute
s" id" s" class" <div#.>      \ with both id and class

\ Convenience words (escaped content, self-closing)
s" Hello" h1.                 \ <h1>Hello</h1>
s" Paragraph" p.              \ <p>Paragraph</p>
s" Cell" td.                  \ <td>Cell</td>
s" Code" td-code.             \ <td><code>Code</code></td>

\ CSS rules
s" .card" s" background:#18181b;padding:1rem" css-rule
\ outputs: .card{background:#18181b;padding:1rem}
```

**Key words:**

| Word | Stack | Description |
|------|-------|-------------|
| `html>file` | ( fid -- ) | Set output file descriptor |
| `text` | ( addr u -- ) | Output escaped text |
| `raw` | ( addr u -- ) | Output raw HTML |
| `nl` | ( -- ) | Newline |
| `html-head` | ( title$ -- ) | Start document, leave head open |
| `html-body` | ( -- ) | Close head, open body |
| `html-begin` | ( title$ -- ) | Start document (head+body) |
| `html-end` | ( -- ) | Close body and html |
| `<div.>` | ( class$ -- ) | Open div with class |
| `<div#.>` | ( id$ class$ -- ) | Open div with id and class |
| `h1.` `h2.` `h3.` | ( text$ -- ) | Heading with escaped text |
| `p.` | ( text$ -- ) | Paragraph with escaped text |
| `th.` `td.` | ( text$ -- ) | Table cell with escaped text |
| `a.` | ( text$ url$ -- ) | Link element |
| `css-rule` | ( sel$ props$ -- ) | CSS rule: sel{props} |
| `html-escape` | ( addr u -- addr' u' ) | Escape HTML entities |

Full HTML5 vocabulary: `<div>`, `<span>`, `<section>`, `<article>`, `<header>`, `<footer>`, `<nav>`, `<main>`, `<aside>`, `<p>`, `<strong>`, `<em>`, `<code>`, `<pre>`, `<ul>`, `<ol>`, `<li>`, `<table>`, `<thead>`, `<tbody>`, `<tr>`, `<th>`, `<td>`, `<form>`, `<input`, `<button>`, `<label>`, `<textarea>`, `<select>`, `<option>`, `<a`, `<img`, `<style>`, `<script>`, `<br/>`.

---

### sql.fs - SQLite Interface

Query SQLite databases via the CLI. No C bindings needed.

```forth
require ~/fifth/lib/sql.fs

\ Count rows
s" mydb.db" s" SELECT COUNT(*) FROM users" sql-count .  \ prints: 42

\ Iterate results (pipe-delimited)
s" mydb.db" s" SELECT name, email FROM users" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type ." : "    \ first field
    2dup 1 sql-field type cr        \ second field
    2drop
  else 2drop then
repeat 2drop
sql-close

\ Execute with callback
: print-row ( row$ -- ) 2 .sql-fields cr ;
s" mydb.db" s" SELECT name, email FROM users" ['] print-row sql-each

\ Debug: dump query results to stdout
s" mydb.db" s" SELECT * FROM users LIMIT 5" sql-dump
```

**Key words:**

| Word | Stack | Description |
|------|-------|-------------|
| `sql-exec` | ( db$ sql$ -- ) | Run query, results to temp file |
| `sql-count` | ( db$ sql$ -- n ) | Run COUNT query, return number |
| `sql-open` | ( -- ) | Open result file for reading |
| `sql-row?` | ( -- addr u flag ) | Read next row |
| `sql-field` | ( addr u n -- addr u field$ ) | Extract nth field (0-based) |
| `sql-close` | ( -- ) | Close result file |
| `sql-each` | ( db$ sql$ xt -- ) | Execute xt for each row |
| `sql-dump` | ( db$ sql$ -- ) | Print results to stdout |
| `sql-tables` | ( db$ -- ) | List all tables |
| `sql-table-count` | ( db$ table$ -- n ) | Count rows in table |

**Note**: SQL strings must not contain single quotes (they're used for shell quoting). Use double quotes inside SQL or avoid string literals in WHERE clauses.

---

### template.fs - Template System

Deferred slots, conditional rendering, and layout composition.

```forth
require ~/fifth/lib/template.fs

\ Conditional rendering
true ['] emit-sidebar ?render   \ only renders if flag is true

\ Deferred slots (template inheritance)
slot: @header
slot: @main

: my-header s" Welcome" h1. ;
' my-header ->slot @header

\ Now @header executes my-header
@header  \ outputs: <h1>Welcome</h1>
```

---

### ui.fs - UI Components

Pre-built dashboard components with dark theme CSS.

```forth
require ~/fifth/lib/ui.fs

\ Stat cards
42 s" Users" stat-card-n

\ Badges
s" Active" s" bg-success" badge
s" Critical" badge-danger

\ Cards
card-begin
  s" Card Title" card-header
  card-body-begin
    s" Card content here" p.
  card-body-end
card-end

\ Tabbed interface
tabs-begin
  s" Overview" s" overview" true tab     \ active tab
  s" Settings" s" settings" false tab
tabs-end

s" overview" true panel-begin
  s" Overview content" p.
panel-end

s" settings" false panel-begin
  s" Settings content" p.
panel-end

\ Grid layouts
grid-3                          \ 3-column grid
  \ ... grid items ...
grid-end

\ Dashboard layout
dashboard-begin
  s" Title" s" subtitle" dashboard-header
  dashboard-main-begin
    \ ... dashboard content ...
  dashboard-main-end
dashboard-end

\ Include CSS and JS (call once)
ui-css    \ all component styles
ui-js     \ tab switching JavaScript
```

**Component words:**

| Word | Stack | Description |
|------|-------|-------------|
| `stat-card-n` | ( n label$ -- ) | Stat card with number |
| `badge` | ( text$ class$ -- ) | Colored badge |
| `badge-danger` | ( text$ -- ) | Red badge |
| `badge-success` | ( text$ -- ) | Green badge |
| `card-begin` / `card-end` | ( -- ) | Card container |
| `tab` | ( text$ id$ active? -- ) | Tab button |
| `panel-begin` | ( id$ active? -- ) | Tab panel |
| `panel-end` | ( -- ) | Close panel |
| `grid-2` `grid-3` `grid-4` | ( -- ) | Grid layouts |
| `table-begin` / `table-end` | ( -- ) | Styled table |
| `dashboard-begin` | ( -- ) | Dashboard container |
| `dashboard-header` | ( title$ sub$ -- ) | Dashboard header |
| `ui-css` | ( -- ) | Emit all component CSS |
| `ui-js` | ( -- ) | Emit tab-switching JS |

---

### core.fs - Load Everything

```forth
require ~/fifth/lib/core.fs

\ Loads: str.fs → html.fs → sql.fs → template.fs (not ui.fs)
\ Also provides:
.fifth              \ print version
s" file.html" open-file-cmd  \ open with system default app (macOS)
42 ??               \ assert (aborts if false)
```

## Examples

### db-viewer.fs - Dual Database Viewer

Flat HTML viewer for both `agents.db` and `projects.db`:

```bash
gforth ~/fifth/examples/db-viewer.fs
```

Shows: stats row, agent cards, project cards, constraints, navigation, commands, glossary. Uses `core.fs` (str + html + sql).

### project-dashboard.fs - Tabbed Dashboard

Interactive dashboard for `projects.db` with tab navigation:

```bash
gforth ~/fifth/examples/project-dashboard.fs
```

Features:
- 8 stat cards (projects, constraints, navigation, glossary, commands, conventions, integrations, personas)
- 6 tabbed panels: Overview, Constraints, Navigation, Commands, Glossary, Personas
- Dark theme with gradient stat cards and colored badges
- JavaScript tab switching
- Proper HTML escaping throughout

Uses `core.fs` + `ui.fs` (all libraries).

## Runtime Comparison

| | Fifth + Gforth | Python 3.14 |
|--|----------------|-------------|
| **Runtime size** | **4.6 MB** | 76 MB |
| **Startup** | ~10ms | ~50ms |
| **HTML escaping** | `html-escape` | `html.escape()` |
| **SQLite access** | Shell to CLI | C binding |
| **Dependencies** | sqlite3 (preinstalled) | stdlib |
| **Learning curve** | Steep (stack) | Gentle |
| **Security** | Manual `text` vs `raw` | Manual `escape()` |

## Lessons Learned

Building Fifth required solving real Forth problems:

1. **No `popen`**: Gforth lacks pipe libraries on macOS. Solved by redirecting to temp files.
2. **No dynamic strings**: `s+` (string concat) causes memory errors. Solved with static buffers (`str-reset` / `str+` / `str$`).
3. **Stack discipline**: A missing `-rot` vs `swap` caused null pointer dereferences. Stack comments are essential.
4. **`s"` vs `s\"`**: Standard `s"` has no escapes. Need `s\"` for embedded quotes.
5. **SQL quoting**: Single quotes in SQL conflict with shell quoting. Avoid `WHERE col='value'`; use ORDER BY, numeric comparisons, or double-quote workarounds.
6. **Word spacing**: `</div>nl` is one word; `</div> nl` is two. Forth's parser is ruthlessly literal.

## Why "Fifth"?

1. **Forth → Fifth**: Next generation
2. **The Fifth Age**: Bringing Forth into modern development
3. **Five principles**: Minimal, secure, composable, fast, fun

## License

MIT

## Contributing

Build the vocabulary. Submit words that solve real problems.
