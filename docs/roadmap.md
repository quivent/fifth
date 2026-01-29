# Roadmap

Vision, planned improvements, and priorities for Fifth.

---

## Vision

### The Core Thesis

The software industry's complexity is largely artificial. Most applications can be built with far less — and increasingly, they'll be built by AI agents that benefit from minimal, verifiable systems. A vocabulary of 150 words and a 4.6MB runtime can produce applications that actually work.

### The Numbers

| Component | Fifth | Typical |
|-----------|-------|---------|
| Runtime | 4.6 MB | 300+ MB |
| Dependencies | 0 | 1,200+ |
| Startup | <10ms | 2-5 sec |
| Lines to audit | 1,582 | Unknowable |

### Why This Works

1. **Vocabulary density**: Each word maps to one unit of output
2. **No frameworks**: Words compose, no lifecycle hooks
3. **No configuration**: The program IS the composition
4. **Immediate testability**: Push, execute, check

### Destinations

| Goal | Definition of Done |
|------|-------------------|
| Complete vocabulary | Build any dashboard without raw HTML |
| Forms vocabulary | CRUD app in <500 lines |
| Live system | Edit, save, see update in 200ms |
| Network vocabulary | REST API to HTML in 50 lines |
| Portable documents | Same program, Markdown/LaTeX/SVG output |

### Agent-Specific Goals

| Goal | Definition of Done |
|------|-------------------|
| Stack verification | Built-in stack effect checker for compile-time validation |
| Error taxonomy | Structured error codes agents can parse and fix programmatically |
| Literate mode | Auto-generate docs from stack comments |
| Example corpus | 100+ tested examples for LLM training and few-shot prompting |

---

## Priority: Critical

### 1. Silent Buffer Overflow (str.fs)

`str+` silently drops data on overflow. Add overflow flag and optional abort.

```forth
variable str-overflow
: str+ ( addr u -- )
  dup str-len @ + str-max < if
    str-buf str-len @ + swap dup str-len +! move
  else 2drop true str-overflow ! then ;
```

### 2. Empty Field Parsing (str.fs)

`parse-delim` breaks on consecutive delimiters (`a||c`). Rewrite to count delimiters explicitly.

### 3. Unescaped Attributes (html.fs)

Tag attributes use single quotes but don't escape. XSS vector.

```forth
: <tag.> ( class$ name$ -- )
  s" <" h>> h>> s"  class='" h>> html-escape h>> s" '" h>> >t ;
```

### 4. SQL Injection (sql.fs)

Shell quoting makes injection trivial via database path. Write SQL to temp file, use `sqlite3 < file`.

### 5. No Error Handling (sql.fs)

`sqlite3` errors produce garbage parsed as pipe-delimited data. Check exit code or scan for `Error:`.

### 6. Badge Clobbers Buffer (ui.fs)

`badge` calls `str-reset` internally, destroying caller's buffer. Emit attributes directly without buffer.

### 7. No Tests

Zero unit tests. Add minimal test framework:

```forth
: test: ( "name" -- ) 1 test-count +! bl word count type ."  ... " ;
: expect= ( actual expected -- ) = if ." OK" else ." FAIL" then cr ;
```

---

## Priority: Important

### Buffer Size (str.fs)
1024 bytes is too small. Make configurable, default to 4096.

### String Operations (str.fs)
Add `str-starts?`, `str-ends?`, `str-contains?`, `str-trim`.

### Escape Buffer (html.fs)
`html-escape` clobbers `str2-buf`. Add dedicated escape buffer.

### Data Attributes (html.fs)
Add `data=` for `data-*` attributes used by modern JS.

### HTML-FID Init (html.fs)
Default to `stdout` so forgetting `html>file` produces output, not crash.

### Temp File Cleanup (sql.fs)
Delete `/tmp/fifth-query.txt` on close. Use unique names per session.

### SQL Count Flag (sql.fs)
`sql-count` silently returns 0 on failure. Return success flag.

### Named Assert (core.fs)
`??` gives no context. Add `assert ( flag msg$ -- )`.

### Debug Mode
Global `fifth-debug` flag for SQL commands, buffer states.

### Error Codes
Define error constants, use `catch`/`throw` for recovery.

---

## Priority: Nice-to-Have

### HTML Elements
Add `<figure>`, `<details>`, `<summary>`, `<dl>`, `<hr/>`.

### CSS Media Queries
Add `@media` and `@media-end` for responsive breakpoints.

### Slot Defaults
Allow slots with default content.

### Empty State Component
Standardized "No data found" display.

### Progress Bar
Dashboard progress indicators.

### Cross-Platform Open
Detect `xdg-open` vs `open` for Linux support.

### Version Constants
`fifth-major`, `fifth-minor`, `fifth-patch` for version checks.

---

## Summary Table

| Area | Critical | Important | Nice |
|------|----------|-----------|------|
| str.fs | 2 | 3 | 1 |
| html.fs | 1 | 3 | 2 |
| sql.fs | 2 | 3 | 0 |
| ui.fs | 1 | 0 | 4 |
| core.fs | 0 | 2 | 3 |
| Cross-cutting | 1 | 3 | 1 |
| **Total** | **7** | **14** | **11** |

---

*Consolidated from ENHANCEMENTS.md and VISIONS.md*
*~200 lines vs ~1,560 original*
