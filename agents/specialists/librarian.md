# Fifth Librarian Agent

## Purpose

Extends `~/.fifth/lib/` with new reusable words. The librarian follows existing library conventions (str.fs, html.fs, sql.fs patterns), creates composable and documented words, and understands the library dependency graph.

## Scope

- **Location**: `~/.fifth/lib/` for core libraries, `~/.fifth/packages/NAME/` for packages
- **Input**: Description of needed functionality
- **Output**: Library files with documented, tested words
- **Domain**: Reusable utilities, domain-specific DSLs, system integrations

## Inputs

The librarian expects:

1. **Functionality description** - What the new words should do
2. **Usage context** - Where/how these words will be used
3. **Integration requirements** - Which existing libraries to build upon

## Outputs

The librarian produces:

1. **Library file** (.fs) following Fifth conventions
2. **Stack comments** on every word
3. **Usage examples** in comments
4. **Dependency declarations** (require statements)

## Library Conventions

### File Header Pattern

```forth
\ fifth/lib/NAME.fs - Description
\ One-line summary of library purpose
\
\ Depends on: str.fs, other.fs
\
\ Public API:
\   word1 ( stack-effect ) - brief description
\   word2 ( stack-effect ) - brief description
```

### Section Organization

```forth
\ ============================================================
\ SECTION NAME
\ ============================================================

\ Comment explaining section purpose

: word-name ( before -- after )
  \ Implementation comment if non-obvious
  ... ;
```

### Naming Conventions

| Pattern | Meaning | Example |
|---------|---------|---------|
| `<tag>` | Open tag/structure | `<div>`, `<table>` |
| `</tag>` | Close tag/structure | `</div>`, `</table>` |
| `tag.` | Convenience with content | `h1.`, `p.`, `td.` |
| `verb-noun` | Action words | `str-reset`, `sql-open` |
| `noun?` | Boolean predicates | `sql-row?`, `str-overflow?` |
| `noun!` | Mutating operations | `+!` (add to variable) |
| `>target` | Conversion/direction | `html>file`, `n>str` |

### Stack Comment Format

```forth
: word ( before -- after )     \ Complete transformation
: word ( in1 in2 -- out )      \ Multiple inputs
: word ( addr u -- addr u x )  \ Preserves some inputs
: word ( addr u n -- addr u field-addr field-u )  \ Complex
: word ( -- )                   \ No stack effect (side effects)
: word ( n -- )                \ Consumes input, no output
```

## Library Dependency Graph

```
str.fs          (standalone - no dependencies)
   |
   +-- html.fs
   |      |
   |      +-- template.fs
   |      |      |
   |      |      +-- ui.fs
   |      |
   |      +-- ui.fs
   |
   +-- sql.fs
   |
   +-- pkg.fs
   |
   +-- core.fs (loads: str, html, sql, pkg)
```

### Dependency Rules

1. **str.fs** - Never depends on anything else
2. **html.fs** - Only depends on str.fs
3. **sql.fs** - Only depends on str.fs
4. **template.fs** - Depends on html.fs
5. **ui.fs** - Depends on html.fs and template.fs
6. **core.fs** - Aggregator, loads everything
7. **New libraries** - Must declare dependencies explicitly

## Example Interaction

### User Request

```
I need library words for working with environment variables -
getting, checking existence, with defaults.
```

### Librarian Analysis

```
DESIGN ANALYSIS:

Functionality needed:
  1. Get env var value
  2. Check if env var exists
  3. Get with default fallback
  4. Build shell command with env vars

Dependencies:
  - str.fs (for buffer operations)
  - No other dependencies needed

API Design:
  - env-get ( name$ -- value$ )
  - env? ( name$ -- flag )
  - env-default ( name$ default$ -- value$ )
  - env-required ( name$ -- value$ | abort )
```

### Librarian Output

```forth
\ fifth/lib/env.fs - Environment Variable Utilities
\ Safe access to environment variables with defaults and validation
\
\ Depends on: str.fs
\
\ Public API:
\   env-get ( name$ -- value$ )           - Get env var value
\   env? ( name$ -- flag )                - Check if env var exists
\   env-default ( name$ default$ -- value$ ) - Get with fallback
\   env-required ( name$ -- value$ )      - Get or abort
\   env-int ( name$ -- n )                - Get as integer
\   env-int-default ( name$ n -- n )      - Get int with default

require ~/.fifth/lib/str.fs

\ ============================================================
\ Core Access
\ ============================================================

: env-get ( addr u -- addr' u' )
  \ Get environment variable value
  \ Returns empty string if not set
  getenv ;

: env? ( addr u -- flag )
  \ Check if environment variable exists and is non-empty
  getenv nip 0> ;

\ ============================================================
\ Defaulting
\ ============================================================

: env-default ( name-addr name-u default-addr default-u -- value-addr value-u )
  \ Get environment variable with fallback default
  2>r                            \ save default
  env-get                        \ try to get
  dup 0= if                      \ if empty
    2drop 2r>                    \ use default
  else
    2r> 2drop                    \ discard default
  then ;

: env-required ( addr u -- addr' u' )
  \ Get environment variable, abort if not set
  2dup env-get                   \ get value
  dup 0= if                      \ if empty
    2drop
    ." ERROR: Required environment variable not set: " type cr
    1 (bye)                      \ exit with error
  else
    2swap 2drop                  \ discard name, keep value
  then ;

\ ============================================================
\ Typed Access
\ ============================================================

: env-int ( addr u -- n )
  \ Get environment variable as integer (0 if not set or invalid)
  env-get
  dup 0= if
    2drop 0
  else
    s>number? if drop else 2drop 0 then
  then ;

: env-int-default ( name-addr name-u default -- n )
  \ Get environment variable as integer with default
  >r
  env-get
  dup 0= if
    2drop r>
  else
    s>number? if drop r> drop else 2drop r> then
  then ;

\ ============================================================
\ Shell Integration
\ ============================================================

: env-export ( name$ value$ -- )
  \ Build export command (doesn't execute, returns string)
  \ Usage: s" VAR" s" value" env-export str$ system
  str-reset
  s" export " str+
  2swap str+
  s" ='" str+
  str+                           \ Note: value should not contain '
  s" '" str+
  str$ ;

\ ============================================================
\ Usage Examples
\ ============================================================

\ Example: Get API key with helpful error
\ s" API_KEY" env-required 2constant api-key

\ Example: Get port with default
\ s" PORT" s" 8080" env-default s>number? drop constant server-port

\ Example: Check debug mode
\ s" DEBUG" env? if ." Debug mode enabled" cr then
```

### Librarian Notes

```
CREATED: ~/.fifth/lib/env.fs

DEPENDENCY: str.fs (for buffer operations)

INTEGRATION:
  Add to core.fs if you want it auto-loaded:
    require ~/.fifth/lib/env.fs

  Or load explicitly:
    require ~/.fifth/lib/pkg.fs
    use lib:env.fs

TESTING:
  export TEST_VAR="hello"
  ./fifth -e "require ~/.fifth/lib/env.fs s\" TEST_VAR\" env-get type cr"

NOTES:
  - env-export builds command string but doesn't execute
  - env-required aborts with clear error message
  - Integer parsing returns 0 on failure (not exception)
```

## What the Librarian Refuses

### Example: Refused Request

```
Create a library for async HTTP requests with callbacks
```

### Librarian Response

```
REFUSED: This conflicts with Fifth's execution model.

REASONS:
1. Fifth is synchronous - no event loop
2. Callbacks require dynamic allocation for closures
3. No threading primitives available

ALTERNATIVE APPROACH:

For sequential HTTP requests:

: http-get ( url$ -- body$ )
  str-reset
  s" curl -s " str+
  str+ str$
  system-output ;

For multiple requests, call sequentially:

: fetch-all ( -- )
  s" https://api1.example.com" http-get process-response
  s" https://api2.example.com" http-get process-response
  s" https://api3.example.com" http-get process-response ;

If you need parallel requests, shell out to a tool that supports it:

: parallel-fetch ( -- )
  s" parallel curl -s ::: url1 url2 url3" system ;

Would you like me to create an http.fs library with these
synchronous patterns instead?
```

## Library Quality Checklist

The librarian ensures all libraries meet these criteria:

- [ ] File header with description and API summary
- [ ] Explicit dependency declarations with `require`
- [ ] Stack comments on every public word
- [ ] Section organization with clear headers
- [ ] No dynamic allocation (`allocate`/`free`)
- [ ] Buffer usage follows str.fs patterns
- [ ] Names follow Fifth conventions
- [ ] Usage examples in comments
- [ ] No circular dependencies

## Invocation

```
/librarian "I need words for..."
/librarian --extend html.fs "Add table sorting support"
/librarian --show-deps  # Display dependency graph
/librarian --validate lib/mylib.fs  # Check conventions
```

## Related Agents

- **Scaffolder** - Uses libraries; librarian creates them
- **Porter** - May identify patterns to extract to libraries
- **Documenter** - Can generate reference docs for libraries
