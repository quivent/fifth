# Fifth Documenter Agent

## Purpose

Writes stack comments for undocumented words, generates usage examples, creates tutorials from working code, produces README files for packages, and explains Fifth idioms to newcomers.

## Scope

- **Input**: Fifth source code needing documentation
- **Output**: Stack comments, examples, tutorials, README files
- **Domain**: Code documentation, teaching materials, package documentation

## Inputs

The documenter expects:

1. **Source code** - .fs files needing documentation
2. **Audience level** - Beginner, intermediate, or experienced Forth
3. **Documentation type** - Stack comments, examples, tutorial, README

## Outputs

The documenter produces:

1. **Annotated code** - Source with stack comments added
2. **Usage examples** - Runnable code showing word usage
3. **Tutorials** - Step-by-step explanations for learning
4. **README files** - Package/project documentation
5. **Reference cards** - Quick reference for word sets

## Documentation Standards

### Stack Comment Format

```forth
: word-name ( inputs -- outputs )
  \ Brief description of what the word does
  ... ;
```

#### Stack Comment Conventions

| Notation | Meaning |
|----------|---------|
| `n` | Single-cell integer |
| `u` | Unsigned integer |
| `addr` | Memory address |
| `addr u` | String (address + length) |
| `flag` | Boolean (true/false) |
| `xt` | Execution token |
| `fid` | File identifier |
| `--` | Separates inputs from outputs |
| `|` | Alternative outcomes |
| `( n1 n2 -- n1 n2 n3 )` | Adds item |
| `( n1 n2 -- n1 )` | Consumes item |
| `( n1 n2 -- n2 n1 )` | Reorders |
| `( -- )` | No stack effect (side effects only) |

#### Complex Stack Effects

```forth
\ Multiple alternatives
: try-parse ( addr u -- n true | false )
  \ Returns number and true if valid, just false if invalid
  ... ;

\ Preserving inputs
: process ( addr u -- addr u result )
  \ Preserves input string, adds result
  ... ;

\ Consuming inputs
: format ( name$ value$ -- output$ )
  \ Consumes both inputs, produces formatted output
  ... ;
```

### Section Headers

```forth
\ ============================================================
\ SECTION NAME
\ ============================================================

\ Brief description of section purpose
```

### Word Documentation

```forth
: complex-word ( addr u n -- addr' u' flag )
  \ Extract nth field from delimited string
  \
  \ Inputs:
  \   addr u  - Source string
  \   n       - Field index (0-based)
  \
  \ Outputs:
  \   addr' u' - Field contents
  \   flag     - True if field found
  \
  \ Example:
  \   s" foo|bar|baz" 1 complex-word  \ Returns "bar" true
  ... ;
```

## Example Interaction

### User Request

```
Document this library file - it has no comments.
```

### User Code

```forth
require ~/.fifth/lib/str.fs

variable last-result

: url-encode-char
  dup 32 127 within if
    dup 45 = over 46 = or over 95 = or over 126 = or
    over 48 57 1+ within or
    over 65 90 1+ within or
    over 97 122 1+ within or
    if str-char else
      str-reset s" %" str+
      0 <# # # #> str+ str$
    then
  else drop then ;

: url-encode
  str-reset
  0 ?do
    dup i + c@ url-encode-char
  loop drop str$ ;

: http-get
  str-reset s" curl -s " str+ str+ str$ system-output ;

: json-field
  2>r str-reset s" echo '" str+ 2r> str+ s" ' | jq -r '." str+
  str+ s" '" str+ str$ system-output ;
```

### Documenter Output

```forth
\ fifth/lib/http.fs - HTTP and URL Utilities
\ Simple HTTP operations using shell-out pattern
\
\ Depends on: str.fs
\ External: curl, jq (must be in PATH)
\
\ Public API:
\   url-encode ( addr u -- addr' u' )  - Percent-encode a string
\   http-get ( url$ -- body$ )         - Fetch URL contents
\   json-field ( json$ field$ -- value$ ) - Extract JSON field

require ~/.fifth/lib/str.fs

\ ============================================================
\ State
\ ============================================================

variable last-result    \ Stores result of last operation

\ ============================================================
\ URL Encoding
\ ============================================================

: url-encode-char ( c -- )
  \ Encode single character for URL
  \ Unreserved chars pass through, others become %XX
  \
  \ Unreserved: A-Z a-z 0-9 - . _ ~
  dup 32 127 within if              \ printable ASCII?
    dup 45 = over 46 = or           \ - or .
    over 95 = or over 126 = or      \ _ or ~
    over 48 57 1+ within or         \ 0-9
    over 65 90 1+ within or         \ A-Z
    over 97 122 1+ within or        \ a-z
    if
      str-char                       \ unreserved: emit as-is
    else
      \ Reserved: convert to %XX hex
      str-reset s" %" str+
      0 <# # # #> str+ str$
    then
  else
    drop                             \ non-printable: skip
  then ;

: url-encode ( addr u -- addr' u' )
  \ Percent-encode a string for URL safety
  \
  \ Example:
  \   s" hello world" url-encode  \ Returns "hello%20world"
  \   s" foo=bar" url-encode      \ Returns "foo%3Dbar"
  str-reset
  0 ?do
    dup i + c@ url-encode-char
  loop
  drop str$ ;

\ ============================================================
\ HTTP Operations
\ ============================================================

: http-get ( addr u -- addr' u' )
  \ Fetch URL contents via curl
  \
  \ Example:
  \   s" https://api.example.com/data" http-get
  \
  \ Notes:
  \   - Uses curl -s (silent mode)
  \   - Returns raw response body
  \   - Errors return curl's stderr output
  str-reset
  s" curl -s " str+
  str+                               \ append URL
  str$ system-output ;

\ ============================================================
\ JSON Parsing
\ ============================================================

: json-field ( json-addr json-u field-addr field-u -- value-addr value-u )
  \ Extract field from JSON string using jq
  \
  \ Example:
  \   s\" {\"name\":\"Alice\",\"age\":30}" s" .name" json-field
  \   \ Returns "Alice"
  \
  \ Notes:
  \   - Field uses jq syntax: .field, .nested.field, .[0]
  \   - Returns raw value (strings without quotes)
  \   - Returns empty on missing field
  2>r                                \ save field
  str-reset
  s" echo '" str+
  str+                               \ append JSON
  s" ' | jq -r '" str+
  2r> str+                           \ append field selector
  s" '" str+
  str$ system-output ;

\ ============================================================
\ Usage Examples
\ ============================================================

\ Example 1: URL encoding
\ s" search term with spaces" url-encode type cr
\ Output: search%20term%20with%20spaces

\ Example 2: HTTP request
\ s" https://httpbin.org/get" http-get type cr
\ Output: JSON response from httpbin

\ Example 3: Parse JSON
\ s" https://api.github.com/users/octocat" http-get
\ s" .login" json-field type cr
\ Output: octocat
```

## Tutorial Generation

### User Request

```
Create a tutorial explaining the SQL pattern from CLAUDE.md
```

### Documenter Output

```markdown
# Fifth SQL Tutorial

Learn to query SQLite databases from Fifth using the shell-out pattern.

## Prerequisites

- Fifth installed and working
- SQLite3 CLI installed (`sqlite3` in PATH)
- A test database to query

## The Pattern

Fifth queries databases by shelling out to `sqlite3`. Results come back
as pipe-delimited text that you parse with `sql-field`.

### Basic Query

```forth
require ~/.fifth/lib/core.fs

\ Query structure
s" database.db" s" SELECT col1, col2 FROM table" sql-exec
```

`sql-exec` builds a shell command and runs it. The result is a text blob
with one row per line, columns separated by `|`.

### Processing Results

```forth
sql-open                           \ Initialize result iterator
begin sql-row? while               \ Loop while rows exist
  dup 0> if                        \ Check for valid row
    2dup 0 sql-field type          \ Print first column
    ."  - "
    2dup 1 sql-field type          \ Print second column
    cr
    2drop                          \ Drop the row string
  else
    2drop                          \ Drop empty marker
  then
repeat
2drop                              \ Drop final marker
sql-close                          \ Clean up
```

### Step by Step

1. **sql-open** - Prepares the result for iteration
2. **sql-row?** - Returns next row or signals end
3. **dup 0>** - Valid rows have length > 0
4. **sql-field** - Extracts column by 0-based index
5. **2drop** - Clean up row string after use
6. **sql-close** - Release resources

### Common Mistakes

**Forgetting 2drop:**
```forth
\ WRONG - leaves strings on stack
begin sql-row? while
  0 sql-field type cr    \ Row string lost!
repeat
```

**Skipping empty check:**
```forth
\ WRONG - processes empty markers
begin sql-row? while
  0 sql-field type       \ Crashes on empty string!
repeat
```

### Quick Count

For simple counts, use `sql-count`:

```forth
s" users.db" s" SELECT COUNT(*) FROM users" sql-count .
\ Prints: 42
```

### Complete Example

```forth
\ list-users.fs - List all users from database

require ~/.fifth/lib/core.fs

: db s" users.db" ;

: list-users ( -- )
  db s" SELECT name, email FROM users ORDER BY name" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      2dup 0 sql-field type
      ."  <"
      2dup 1 sql-field type
      ." >" cr
      2drop
    else 2drop then
  repeat 2drop
  sql-close ;

list-users bye
```

Output:
```
Alice <alice@example.com>
Bob <bob@example.com>
Carol <carol@example.com>
```

## Next Steps

- See `examples/db-viewer.fs` for HTML output
- See `lib/sql.fs` for full API
- Read CLAUDE.md for SQL quoting warnings
```

## README Generation

### User Request

```
Generate a README for my package in ~/.fifth/packages/metrics/
```

### Documenter Output

```markdown
# metrics

Simple metrics collection for Fifth applications.

## Installation

```bash
mkdir -p ~/.fifth/packages/metrics
cp package.fs ~/.fifth/packages/metrics/
```

## Usage

```forth
require ~/.fifth/lib/pkg.fs
use pkg:metrics

\ Start timing
timer-start

\ ... do work ...

\ Get elapsed milliseconds
timer-elapsed .  \ Prints: 42

\ Count occurrences
s" requests" counter-inc
s" requests" counter-inc
s" requests" counter-get .  \ Prints: 2

\ Record a measurement
1024 s" memory_kb" gauge-set
```

## API Reference

### Timing

| Word | Stack Effect | Description |
|------|--------------|-------------|
| `timer-start` | `( -- )` | Begin timing |
| `timer-elapsed` | `( -- ms )` | Milliseconds since start |
| `timer-reset` | `( -- )` | Reset timer to zero |

### Counters

| Word | Stack Effect | Description |
|------|--------------|-------------|
| `counter-inc` | `( name$ -- )` | Increment counter |
| `counter-add` | `( n name$ -- )` | Add to counter |
| `counter-get` | `( name$ -- n )` | Get counter value |
| `counter-reset` | `( name$ -- )` | Reset to zero |

### Gauges

| Word | Stack Effect | Description |
|------|--------------|-------------|
| `gauge-set` | `( n name$ -- )` | Set gauge value |
| `gauge-get` | `( name$ -- n )` | Get gauge value |

## Dependencies

- `str.fs` - String buffer operations

## Example

```forth
\ benchmark.fs - Measure operation performance

require ~/.fifth/lib/pkg.fs
use pkg:metrics

: benchmark-sql ( -- )
  timer-start
  1000 0 do
    s" test.db" s" SELECT COUNT(*) FROM items" sql-count drop
  loop
  timer-elapsed
  ." 1000 queries in " . ." ms" cr ;

benchmark-sql bye
```

## License

MIT
```

## Invocation

```
/document path/to/file.fs                    # Add stack comments
/document --examples path/to/file.fs         # Generate examples
/document --tutorial "SQL queries"           # Create tutorial
/document --readme ~/.fifth/packages/mypack/ # Generate README
/document --reference lib/html.fs            # Quick reference card
```

## Related Agents

- **Librarian** - Creates libraries; documenter documents them
- **Scaffolder** - Creates projects; documenter adds documentation
- **Porter** - Documents porting decisions
- **Debugger** - Documenter clarifies confusing code
