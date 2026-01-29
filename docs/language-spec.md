# FIFTH.md -- The Fifth Language Specification

**Version 0.1.0**

Fifth is a practical Forth ecosystem for building real applications. It includes its own interpreter, compiler, and standard libraries. The only external dependency is the `sqlite3` CLI for database features. Fifth is not a framework. It imposes no structure, requires no configuration files, and has no build system. It is six libraries totaling approximately 1,100 lines of code.

---

## 1. What Fifth Is

Fifth is a collection of practical Forth libraries that provide:

- **String buffers** without dynamic allocation (`str.fs`)
- **HTML5 generation** with automatic XSS-safe escaping (`html.fs`)
- **SQLite queries** via the CLI with result iteration (`sql.fs`)
- **Template composition** with deferred slots and conditional rendering (`template.fs`)
- **UI components** for dashboards with dark theme CSS (`ui.fs`)
- **A loader** with file, shell, and debug utilities (`core.fs`)

### What Fifth is not

- Not a framework. No application object, no router, no lifecycle, no plugin system.
- Not just libraries. Fifth has its own C interpreter and Rust compiler.
- Self-contained. No external Forth implementation required.

### Philosophy

1. **Minimal dependencies.** Shell out to existing Unix tools rather than require C bindings.
2. **Proper escaping.** Security by default. `text` escapes; `raw` bypasses.
3. **Stack-based DSLs.** Vocabularies that make intent clear.
4. **Composable.** Small words that combine into larger patterns.
5. **No dynamic allocation.** Static buffers, temp files, stack manipulation. No `allocate`/`free`.

### Requirements

- `sqlite3` CLI (ships with macOS; `apt install sqlite3` on Linux)
- C compiler (to build the interpreter)

---

## 2. Architecture

### 2.1 Library Dependency Graph

```
str.fs            (standalone -- no Fifth dependencies)
  ^
  |
html.fs           (requires str.fs)
  ^         ^
  |         |
sql.fs      |     (requires str.fs)
  |         |
  |    template.fs (requires html.fs)
  |         ^
  |         |
  |    ui.fs       (requires html.fs, template.fs)
  |
core.fs           (loads str.fs, html.fs, sql.fs)
```

`core.fs` does NOT load `template.fs` or `ui.fs`. Load them explicitly when needed.

### 2.2 Core Design Patterns

**Static Buffer Pattern.** All string operations use fixed-size buffers (`str-buf`, `str2-buf`). No dynamic allocation. No `allocate`/`free`. Trade-off: fixed maximum sizes (1024 bytes default).

**File Output Pattern.** All HTML generation writes to a file descriptor stored in `html-fid`. Set the target with `html>file` or `html>stdout`. All output flows through `h>>`.

**Shell-Out Pattern.** External tools (`sqlite3`, `open`) are accessed via `system`. Results are captured to temp files, then read back. No C bindings, no FFI.

**Escape-by-Default Pattern.** `text` escapes HTML entities. `raw` bypasses escaping. The security boundary is explicit in every output call.

**Iteration Pattern.** SQL results follow the `sql-exec` / `sql-open` / `sql-row?` / `sql-field` / `sql-close` cycle. Callback alternative via `sql-each`.

### 2.3 The Two-Buffer System

| Buffer | Storage | Length Variable | Size Constant | Purpose |
|--------|---------|-----------------|---------------|---------|
| Primary | `str-buf` | `str-len` | `str-max` (1024) | General purpose string building: CSS classes, shell commands, concatenation |
| Secondary | `str2-buf` | `str2-len` | `str2-max` (1024) | Reserved for `html-escape` so escaping never corrupts the primary buffer |

**Rule:** Never nest operations on the same buffer. If building a string that will be escaped, build in the primary buffer (`str+`). `html-escape` reads from its input and writes to the secondary buffer (`str2+`).

**Consequence:** Any word that calls `str-reset` internally (e.g., `badge` in `ui.fs`, `sql-cmd-query` in `sql.fs`) will destroy the primary buffer's contents. Finish one buffer operation before starting the next.

### 2.4 The Line Buffer

A third buffer, `line-buf` (512 bytes), is used exclusively for file I/O line reading. It is used by `sql-row?` and `sql-count` to read lines from query result files. Do not use it for general string building.

---

## 3. Library Specifications

### 3.1 str.fs -- String Utilities

**Purpose:** Buffer-based string operations without dynamic allocation.

**Dependencies:** None (standalone).

**Lines:** 148

#### Constants

| Word | Value | Description |
|------|-------|-------------|
| `str-max` | 1024 | Primary buffer capacity in bytes |
| `str2-max` | 1024 | Secondary buffer capacity in bytes |
| `line-max` | 512 | Line buffer capacity in bytes |

#### Storage

| Name | Type | Description |
|------|------|-------------|
| `str-buf` | `create ... allot` | Primary buffer (1024 bytes) |
| `str-len` | `variable` | Primary buffer current length |
| `str2-buf` | `create ... allot` | Secondary buffer (1024 bytes) |
| `str2-len` | `variable` | Secondary buffer current length |
| `line-buf` | `create ... allot` | Line buffer for file I/O (512 bytes) |

#### Primary Buffer Words

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str-reset` | `( -- )` | Clear primary buffer. Sets `str-len` to 0. |
| `str+` | `( addr u -- )` | Append string to primary buffer. Silently drops data if buffer would overflow. |
| `str$` | `( -- addr u )` | Return current contents of primary buffer as address and length. |
| `str-char` | `( c -- )` | Append single character to primary buffer. Silently drops if buffer full. |

**Example:**
```forth
str-reset
s" Hello, " str+
s" World!" str+
str$ type  \ prints: Hello, World!
```

#### Secondary Buffer Words

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str2-reset` | `( -- )` | Clear secondary buffer. Sets `str2-len` to 0. |
| `str2+` | `( addr u -- )` | Append string to secondary buffer. Silently drops on overflow. |
| `str2$` | `( -- addr u )` | Return current contents of secondary buffer. |
| `str2-char` | `( c -- )` | Append single character to secondary buffer. |

**Note:** The secondary buffer is used internally by `html-escape`. Avoid using it directly unless you know `html-escape` is not active.

#### Number Conversion

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `n>str` | `( n -- addr u )` | Convert number to string using pictured numeric output (`<# #s #>`). |

**Example:**
```forth
42 n>str type  \ prints: 42
```

#### String Comparison

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str=` | `( addr1 u1 addr2 u2 -- flag )` | Compare two strings for byte-exact equality. Returns true (-1) or false (0). |

**Example:**
```forth
s" hello" s" hello" str= .  \ prints: -1 (true)
s" hello" s" world" str= .  \ prints: 0 (false)
```

#### String Search

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str-find-char` | `( addr u c -- addr' u' \| 0 0 )` | Find first occurrence of character `c` in string. Returns position as remaining string, or `0 0` if not found. |

**Example:**
```forth
s" hello.world" [char] . str-find-char type  \ prints: .world
```

#### Field Parsing

These words extract fields from delimited strings. Fields are 0-based.

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `skip-to-delim` | `( addr u delim -- addr' u' )` | Skip forward to first occurrence of delimiter character, consuming the delimiter. Returns remaining string after delimiter, or empty string if not found. |
| `field-length` | `( addr u delim -- len )` | Count characters from current position until delimiter or end of string. |
| `parse-delim` | `( addr u n delim -- addr u field-addr field-u )` | Extract the nth field (0-based) from a delimited string. The original string remains on the stack below the field. |
| `parse-pipe` | `( addr u n -- addr u field-addr field-u )` | Extract nth pipe-delimited (`|`) field. Shorthand for `[char] \| parse-delim`. |
| `parse-tab` | `( addr u n -- addr u field-addr field-u )` | Extract nth tab-delimited field. Shorthand for `9 parse-delim`. |
| `parse-comma` | `( addr u n -- addr u field-addr field-u )` | Extract nth comma-delimited field. Shorthand for `[char] , parse-delim`. |

**Example:**
```forth
s" apple|banana|cherry" 0 parse-pipe type  \ prints: apple
\ original string remains on stack -- 2drop to clean up
2drop

s" one,two,three" 2 parse-comma type  \ prints: three
2drop
```

**Caveat:** Empty fields between consecutive delimiters (e.g., `"a||c"`) may not parse correctly. See ENHANCEMENTS.md section 1.5.

---

### 3.2 html.fs -- HTML Generation

**Purpose:** Full HTML5 tag vocabulary with automatic XSS-safe escaping and file-based output.

**Dependencies:** `str.fs`

**Lines:** 337

#### Output Target

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `html-fid` | variable | File descriptor for HTML output. Must be set before emitting HTML. |
| `html>file` | `( fid -- )` | Set `html-fid` to the given file descriptor. |
| `html>stdout` | `( -- )` | Set `html-fid` to stdout. |
| `h>>` | `( addr u -- )` | Write string to `html-fid`. Throws on write error. |
| `h>>nl` | `( -- )` | Write a newline character (byte 10) to `html-fid`. |
| `h>>line` | `( addr u -- )` | Write string followed by newline to `html-fid`. Equivalent to `h>> h>>nl`. |

**Warning:** `html-fid` is uninitialized (0) by default. Calling `h>>` before setting it will throw an exception. Always call `html>file` or `html>stdout` first.

#### HTML Escaping

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `html-escape` | `( addr u -- addr' u' )` | Escape HTML special characters. Converts: `<` to `&lt;`, `>` to `&gt;`, `&` to `&amp;`, `'` to `&#39;`, `"` to `&quot;`. Returns escaped string from `str2-buf`. **Clobbers the secondary buffer.** |

#### Core Output Words

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `raw` | `( addr u -- )` | Output string directly to `html-fid` without escaping. Use only for trusted HTML. |
| `text` | `( addr u -- )` | Output string with HTML escaping. Use for all user-facing content. Calls `html-escape` then `h>>`. |
| `nl` | `( -- )` | Output a newline. Alias for `h>>nl`. |
| `rawln` | `( addr u -- )` | Output raw string with trailing newline. |

#### Tag Building Primitives

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `</` | `( -- )` | Output the string `</`. |
| `/>` | `( -- )` | Output the string `/>`. |
| `>t` | `( -- )` | Output `>` (close an opening tag). |
| `>tnl` | `( -- )` | Output `>` followed by newline. |
| `tag/` | `( name$ -- )` | Self-closing tag: `<name/>`. |
| `<tag>` | `( name$ -- )` | Open tag: `<name>`. |
| `<tag>nl` | `( name$ -- )` | Open tag with newline: `<name>\n`. |
| `</tag>` | `( name$ -- )` | Close tag: `</name>`. |
| `</tag>nl` | `( name$ -- )` | Close tag with newline: `</name>\n`. |
| `<tag.>` | `( class$ name$ -- )` | Open tag with class: `<name class='...'>`  . **Class value is NOT escaped.** |
| `<tag.>nl` | `( class$ name$ -- )` | Open tag with class and newline. |
| `<tag#>` | `( id$ name$ -- )` | Open tag with id: `<name id='...'>`. **Id value is NOT escaped.** |
| `<tag#.>` | `( id$ class$ name$ -- )` | Open tag with id and class: `<name id='...' class='...'>`. |

#### Document Structure

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<!doctype>` | `( -- )` | Output `<!DOCTYPE html>\n`. |
| `<html>` | `( -- )` | Output `<html>\n`. |
| `</html>` | `( -- )` | Output `</html>\n`. |
| `<head>` | `( -- )` | Output `<head>\n`. |
| `</head>` | `( -- )` | Output `</head>\n`. |
| `<body>` | `( -- )` | Output `<body>\n`. |
| `</body>` | `( -- )` | Output `</body>\n`. |
| `<title>` | `( -- )` | Output `<title>`. |
| `</title>` | `( -- )` | Output `</title>\n`. |
| `<meta` | `( -- )` | Output `<meta ` (with trailing space, open for attributes). |
| `meta>` | `( -- )` | Output `>` to close a meta tag. |
| `<link` | `( -- )` | Output `<link ` (with trailing space, open for attributes). |

#### Headings

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<h1>` | `( -- )` | Open `<h1>` tag. |
| `</h1>` | `( -- )` | Close `</h1>\n`. |
| `<h2>` | `( -- )` | Open `<h2>` tag. |
| `</h2>` | `( -- )` | Close `</h2>\n`. |
| `<h3>` | `( -- )` | Open `<h3>` tag. |
| `</h3>` | `( -- )` | Close `</h3>\n`. |
| `<h4>` | `( -- )` | Open `<h4>` tag. |
| `</h4>` | `( -- )` | Close `</h4>\n`. |

#### Containers

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<div>` | `( -- )` | Output `<div>`. |
| `<div.>` | `( class$ -- )` | Output `<div class='...'>`. |
| `<div.>nl` | `( class$ -- )` | Output `<div class='...'>\n`. |
| `<div#>` | `( id$ -- )` | Output `<div id='...'>`. |
| `<div#.>` | `( id$ class$ -- )` | Output `<div id='...' class='...'>`. |
| `</div>` | `( -- )` | Output `</div>`. |
| `</div>nl` | `( -- )` | Output `</div>\n`. |
| `<span>` | `( -- )` | Output `<span>`. |
| `<span.>` | `( class$ -- )` | Output `<span class='...'>`. |
| `</span>` | `( -- )` | Output `</span>`. |
| `<section>` | `( -- )` | Output `<section>\n`. |
| `<section.>` | `( class$ -- )` | Output `<section class='...'>\n`. |
| `</section>` | `( -- )` | Output `</section>\n`. |
| `<article>` | `( -- )` | Output `<article>\n`. |
| `</article>` | `( -- )` | Output `</article>\n`. |
| `<header>` | `( -- )` | Output `<header>\n`. |
| `<header.>` | `( class$ -- )` | Output `<header class='...'>\n`. |
| `</header>` | `( -- )` | Output `</header>\n`. |
| `<footer>` | `( -- )` | Output `<footer>\n`. |
| `</footer>` | `( -- )` | Output `</footer>\n`. |
| `<nav>` | `( -- )` | Output `<nav>\n`. |
| `</nav>` | `( -- )` | Output `</nav>\n`. |
| `<main>` | `( -- )` | Output `<main>\n`. |
| `<main.>` | `( class$ -- )` | Output `<main class='...'>\n`. |
| `</main>` | `( -- )` | Output `</main>\n`. |
| `<aside>` | `( -- )` | Output `<aside>\n`. |
| `<aside.>` | `( class$ -- )` | Output `<aside class='...'>\n`. |
| `</aside>` | `( -- )` | Output `</aside>\n`. |

#### Text Elements

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<p>` | `( -- )` | Output `<p>`. |
| `<p.>` | `( class$ -- )` | Output `<p class='...'>`. |
| `</p>` | `( -- )` | Output `</p>`. |
| `</p>nl` | `( -- )` | Output `</p>\n`. |
| `<strong>` | `( -- )` | Output `<strong>`. |
| `</strong>` | `( -- )` | Output `</strong>`. |
| `<em>` | `( -- )` | Output `<em>`. |
| `</em>` | `( -- )` | Output `</em>`. |
| `<code>` | `( -- )` | Output `<code>`. |
| `</code>` | `( -- )` | Output `</code>`. |
| `<pre>` | `( -- )` | Output `<pre>`. |
| `</pre>` | `( -- )` | Output `</pre>`. |
| `<blockquote>` | `( -- )` | Output `<blockquote>\n`. |
| `</blockquote>` | `( -- )` | Output `</blockquote>\n`. |
| `<br/>` | `( -- )` | Output `<br/>`. Self-closing. |

#### Lists

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<ul>` | `( -- )` | Output `<ul>\n`. |
| `<ul.>` | `( class$ -- )` | Output `<ul class='...'>\n`. |
| `</ul>` | `( -- )` | Output `</ul>\n`. |
| `<ol>` | `( -- )` | Output `<ol>\n`. |
| `</ol>` | `( -- )` | Output `</ol>\n`. |
| `<li>` | `( -- )` | Output `<li>`. |
| `<li.>` | `( class$ -- )` | Output `<li class='...'>`. |
| `</li>` | `( -- )` | Output `</li>`. |
| `</li>nl` | `( -- )` | Output `</li>\n`. |

#### Tables

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<table>` | `( -- )` | Output `<table>\n`. |
| `<table.>` | `( class$ -- )` | Output `<table class='...'>\n`. |
| `</table>` | `( -- )` | Output `</table>\n`. |
| `<thead>` | `( -- )` | Output `<thead>\n`. |
| `</thead>` | `( -- )` | Output `</thead>\n`. |
| `<tbody>` | `( -- )` | Output `<tbody>\n`. |
| `</tbody>` | `( -- )` | Output `</tbody>\n`. |
| `<tr>` | `( -- )` | Output `<tr>`. |
| `<tr.>` | `( class$ -- )` | Output `<tr class='...'>`. |
| `</tr>` | `( -- )` | Output `</tr>\n`. |
| `<th>` | `( -- )` | Output `<th>`. |
| `</th>` | `( -- )` | Output `</th>`. |
| `<td>` | `( -- )` | Output `<td>`. |
| `<td.>` | `( class$ -- )` | Output `<td class='...'>`. |
| `</td>` | `( -- )` | Output `</td>`. |

#### Forms

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<form>` | `( -- )` | Output `<form>\n`. |
| `</form>` | `( -- )` | Output `</form>\n`. |
| `<input` | `( -- )` | Output `<input ` (open for attributes). |
| `input>` | `( -- )` | Output `>` to close input tag. |
| `<button` | `( -- )` | Output `<button` (open for attributes, no trailing space). |
| `<button>` | `( -- )` | Output `<button>`. |
| `<button.>` | `( class$ -- )` | Output `<button class='...'>`. |
| `</button>` | `( -- )` | Output `</button>`. |
| `<label>` | `( -- )` | Output `<label>`. |
| `</label>` | `( -- )` | Output `</label>`. |
| `<textarea>` | `( -- )` | Output `<textarea>`. |
| `</textarea>` | `( -- )` | Output `</textarea>`. |
| `<select>` | `( -- )` | Output `<select>\n`. |
| `</select>` | `( -- )` | Output `</select>\n`. |
| `<option>` | `( -- )` | Output `<option>`. |
| `</option>` | `( -- )` | Output `</option>\n`. |

#### Links and Media

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<a` | `( -- )` | Output `<a ` (open for attributes). |
| `a>` | `( -- )` | Output `>` to close the anchor opening tag. |
| `</a>` | `( -- )` | Output `</a>`. |
| `<img` | `( -- )` | Output `<img ` (open for attributes). |
| `img>` | `( -- )` | Output `>` to close the img tag. |

#### Style and Script

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `<style>` | `( -- )` | Output `<style>\n`. |
| `</style>` | `( -- )` | Output `</style>\n`. |
| `<script>` | `( -- )` | Output `<script>`. |
| `</script>` | `( -- )` | Output `</script>\n`. |

#### Attribute Helpers

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `attr=` | `( name$ value$ -- )` | Output ` name='value'`. Value is NOT escaped. Use for trusted attribute values. |
| `attr-text=` | `( name$ value$ -- )` | Output ` name='escaped-value'`. Value IS escaped via `html-escape`. |
| `href=` | `( url$ -- )` | Output ` href='url'`. Shorthand for `s" href" 2swap attr=`. |
| `src=` | `( url$ -- )` | Output ` src='url'`. |
| `type=` | `( type$ -- )` | Output ` type='type'`. |
| `name=` | `( name$ -- )` | Output ` name='name'`. |
| `value=` | `( value$ -- )` | Output ` value='escaped-value'`. Uses `attr-text=`. |
| `placeholder=` | `( text$ -- )` | Output ` placeholder='escaped-text'`. Uses `attr-text=`. |

#### Convenience Words

These words combine open tag, escaped text content, and close tag into a single call.

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `h1.` | `( text$ -- )` | `<h1>escaped-text</h1>\n` |
| `h2.` | `( text$ -- )` | `<h2>escaped-text</h2>\n` |
| `h3.` | `( text$ -- )` | `<h3>escaped-text</h3>\n` |
| `h4.` | `( text$ -- )` | `<h4>escaped-text</h4>\n` |
| `p.` | `( text$ -- )` | `<p>escaped-text</p>\n` |
| `li.` | `( text$ -- )` | `<li>escaped-text</li>\n` |
| `th.` | `( text$ -- )` | `<th>escaped-text</th>` |
| `td.` | `( text$ -- )` | `<td>escaped-text</td>` |
| `td-code.` | `( text$ -- )` | `<td><code>escaped-text</code></td>` |
| `td-raw.` | `( html$ -- )` | `<td>raw-html</td>` (no escaping). |
| `a.` | `( text$ url$ -- )` | `<a href='url'>escaped-text</a>`. URL is not escaped. |
| `img.` | `( alt$ src$ -- )` | `<img src='src' alt='escaped-alt'>`. Alt text is escaped. |

#### Document Patterns

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `html-head` | `( title$ -- )` | Output `<!DOCTYPE html>`, open `<html>`, open `<head>`, emit charset and viewport meta tags, emit `<title>`. **Leaves `<head>` open** so you can inject `<style>` blocks. |
| `html-body` | `( -- )` | Close `</head>`, open `<body>`. Call after `html-head` and any style injections. |
| `html-begin` | `( title$ -- )` | Shorthand for `html-head html-body`. Opens a complete document with head closed and body open. |
| `html-end` | `( -- )` | Output `</body></html>`. |

**Typical document pattern:**
```forth
s" /tmp/output.html" w/o create-file throw html>file

s" Page Title" html-head       \ leaves <head> open
  <style> ... </style>         \ inject CSS
  ui-css                       \ component styles
html-body                      \ close head, open body

  \ ... page content ...

ui-js                          \ tab switching JS (before </body>)
html-end                       \ close body, html

html-fid @ close-file throw
```

#### CSS Helpers

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `css` | `( css-string$ -- )` | Wrap CSS in `<style>` tags: `<style>\n...css...\n</style>`. |
| `css-rule` | `( selector$ properties$ -- )` | Output a CSS rule: `selector{properties}\n`. |

**Example:**
```forth
s" .card" s" background:#18181b;padding:1rem" css-rule
\ outputs: .card{background:#18181b;padding:1rem}
```

---

### 3.3 sql.fs -- SQLite Interface

**Purpose:** Query SQLite databases via the `sqlite3` CLI. No C bindings.

**Dependencies:** `str.fs`

**Lines:** 153

#### Architecture

Every query builds a shell command of the form:

```
sqlite3 -separator '|' 'dbpath' 'sql' > /tmp/fifth-query.txt
```

Results are written to a temp file as pipe-delimited text. Iteration reads this file line by line.

#### Constants and Variables

| Name | Type | Description |
|------|------|-------------|
| `sql-output` | `2constant` | Path to query result file: `/tmp/fifth-query.txt` |
| `sql-count-output` | `2constant` | Path to count result file: `/tmp/fifth-count.txt` |
| `sql-fid` | `variable` | File descriptor for reading query results |
| `sql-count-fid` | `variable` | File descriptor for reading count results |

#### Command Building (internal)

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-cmd-query` | `( db$ sql$ -- )` | Build the shell command string in `str-buf` for a pipe-delimited query. **Clobbers primary buffer.** |
| `sql-cmd-count` | `( db$ sql$ -- )` | Build the shell command string in `str-buf` for a count query. **Clobbers primary buffer.** |

#### Query Execution

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-exec` | `( db$ sql$ -- )` | Execute query. Results written to `sql-output`. Calls `system`. |
| `sql-count` | `( db$ sql$ -- n )` | Execute query expected to return a single number. Reads first line of output, converts to number. Returns 0 if conversion fails. |

#### Result Iteration

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-open` | `( -- )` | Open `sql-output` for reading. |
| `sql-close` | `( -- )` | Close the result file. |
| `sql-row?` | `( -- addr u flag )` | Read next line from results. Returns line contents and a flag (true if data was read, false at EOF). The string is in `line-buf` and is valid only until the next `sql-row?` call. |
| `sql-field` | `( addr u n -- addr u field$ )` | Extract nth field (0-based) from a pipe-delimited row. The original row string remains on the stack below the field. Alias for `parse-pipe`. |

**Standard iteration pattern:**
```forth
s" path.db" s" SELECT a, b FROM t" sql-exec
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

#### Table Queries

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-tables` | `( db$ -- )` | List all tables. Executes `SELECT name FROM sqlite_master WHERE type='table'`. Results in `sql-output`. |
| `sql-schema` | `( db$ table$ -- )` | Get CREATE statement for a table. Results in `sql-output`. **Clobbers primary buffer.** |
| `sql-table-count` | `( db$ table$ -- n )` | Count rows in a named table. **Clobbers primary buffer.** |

#### High-Level Iteration

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-each` | `( db$ sql$ xt -- )` | Execute query and call `xt` for each non-empty row. `xt` receives `( addr u -- )` with the pipe-delimited row string. Handles open/close automatically. |

**Example:**
```forth
: print-row ( row$ -- ) 2 .sql-fields cr ;
s" mydb.db" s" SELECT name, email FROM users" ['] print-row sql-each
```

#### Debug Helpers

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sql-dump` | `( db$ sql$ -- )` | Execute query and print all non-empty rows to stdout. |
| `.sql-field` | `( addr u n -- addr u )` | Print the nth field of a row to stdout. Row string remains on stack. |
| `.sql-fields` | `( addr u n -- )` | Print the first n fields (0 through n-1) of a row to stdout, tab-separated. Consumes the row string. |

#### Known Limitations

1. **Single quotes in SQL conflict with shell quoting.** The query is wrapped in single quotes for the shell. SQL string literals (e.g., `WHERE name='Alice'`) break the command. Use numeric comparisons, ORDER BY, or double-quote workarounds.
2. **Single shared temp file.** Only one query can be open at a time. Process results fully before starting another query.
3. **No error detection.** If `sqlite3` fails, the result file contains the error message, which is then parsed as data. No exit code checking.
4. **No parameterized queries.** All SQL is string-concatenated. Injection risk for user-controlled input.
5. **Temp files persist.** `/tmp/fifth-query.txt` and `/tmp/fifth-count.txt` are not cleaned up.

---

### 3.4 template.fs -- Template System

**Purpose:** Deferred slots for template inheritance, conditional rendering, content capture, and layout composition.

**Dependencies:** `html.fs` (which requires `str.fs`)

**Lines:** 124

#### Deferred Slots

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `slot:` | `( "name" -- )` | Define a named slot. Creates a word that executes `noop` by default. The slot can be filled later with `->slot`. |
| `->slot` | `( xt "name" -- )` | Fill a previously defined slot with an execution token. The named slot will now execute `xt` when called. |

**Example:**
```forth
slot: @header
slot: @main

: my-header s" Welcome" h1. ;
' my-header ->slot @header

@header  \ outputs: <h1>Welcome</h1>
```

#### Content Capture

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `capture-mode` | variable | Stores the previous `html-fid` during capture. |
| `capture-fid` | variable | File descriptor for the capture temp file. |
| `capture-file` | `2constant` | Path: `/tmp/fifth-capture.html`. |
| `begin-capture` | `( -- )` | Redirect HTML output to a temp file. Saves current `html-fid`. |
| `end-capture` | `( -- addr u )` | Stop capturing, restore previous `html-fid`, return captured content as a string. **Uses `slurp-file` which dynamically allocates memory** -- the one exception to Fifth's no-allocation rule. |

**Caveat:** Captures do not nest. The capture file path is fixed.

#### Conditional Rendering

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `?render` | `( flag xt -- )` | Execute `xt` only if `flag` is true. Otherwise drops `xt`. |
| `?text` | `( flag addr u -- )` | Output escaped text only if `flag` is true. Otherwise drops the string. |
| `?raw` | `( flag addr u -- )` | Output raw HTML only if `flag` is true. Otherwise drops the string. |

**Example:**
```forth
true s" This is shown" ?text
false s" This is hidden" ?text
```

#### Iteration

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `times` | `( n xt -- )` | Execute `xt` n times. Each execution receives the current index (0 to n-1) on the stack. `xt` signature: `( index -- )`. |

**Example:**
```forth
5 ['] . times  \ prints: 0 1 2 3 4
```

#### Data-Driven Rendering

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `join-render` | `( n xt sep$ -- )` | Render `n` items using `xt`, with `sep$` output as raw HTML between items. `xt` receives `( xt index -- )`. |

#### Fragment Helper

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `fragment:` | `( "name" -- )` | Define a named fragment (reusable HTML snippet). Syntactic sugar -- equivalent to `: name ... ;`. |

#### Component and Layout Conventions

Components are ordinary words that output HTML. By convention:

- Component names end with `-c` (e.g., `card-c`)
- Layout names end with `-layout` (e.g., `page-layout`)
- Layouts use slots (`slot:`, `->slot`) for injectable content

---

### 3.5 ui.fs -- UI Components

**Purpose:** Pre-built dashboard components with dark theme CSS and tab-switching JavaScript.

**Dependencies:** `html.fs`, `template.fs`

**Lines:** 262

#### Badges

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `badge` | `( text$ class$ -- )` | Render `<span class='badge CLASS'>escaped-text</span>`. **Clobbers the primary string buffer** (calls `str-reset` internally to build the class string). |
| `badge-primary` | `( text$ -- )` | Badge with class `bg-primary` (blue). |
| `badge-success` | `( text$ -- )` | Badge with class `bg-success` (green). |
| `badge-warning` | `( text$ -- )` | Badge with class `bg-warning` (amber). |
| `badge-danger` | `( text$ -- )` | Badge with class `bg-danger` (red). |
| `badge-info` | `( text$ -- )` | Badge with class `bg-info` (cyan). |

**Warning:** `badge` calls `str-reset` and `str+` internally. If you are building a string in the primary buffer, calling `badge` will destroy your buffer contents. Finish buffer operations before calling badge words.

#### Cards

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `card-begin` | `( -- )` | Open `<div class='card'>\n`. |
| `card-end` | `( -- )` | Close `</div>\n`. |
| `card-header` | `( title$ -- )` | Render card header with escaped title in `<h3>`. |
| `card-body-begin` | `( -- )` | Open `<div class='card-body'>\n`. |
| `card-body-end` | `( -- )` | Close `</div>\n`. |
| `card` | `( title$ body-xt -- )` | Complete card: opens card div, renders title in `<h3>`, executes `body-xt`, closes card. |
| `card-with-badge` | `( title$ badge$ badge-class$ body-xt -- )` | Card with a badge after the title in the `<h3>`. Renders title text, then badge, then executes `body-xt`. |

#### Stat Cards

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `stat-card` | `( value$ label$ -- )` | Render a stat card with a large value and small label. Uses `stat-card`, `stat-value`, and `stat-label` CSS classes. |
| `stat-card-n` | `( n label$ -- )` | Stat card with numeric value. Converts `n` to string via `n>str`, then calls `stat-card`. |

#### Navigation

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `nav-begin` | `( -- )` | Open `<nav>\n`. |
| `nav-end` | `( -- )` | Close `</nav>\n`. |
| `nav-item` | `( text$ href$ active? -- )` | Render a navigation link. If `active?` is true, adds `active` CSS class. |
| `nav-item-js` | `( text$ panel-id$ active? -- )` | Navigation item that calls `showPanel('id')` on click instead of following a link. |

#### Sidebar

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `sidebar-begin` | `( -- )` | Open `<aside class='sidebar'>`. |
| `sidebar-end` | `( -- )` | Close `</aside>\n`. |
| `sidebar-section` | `( title$ -- )` | Render a sidebar section heading. |

#### Tabs

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `current-tab` | variable | (Defined but not used in the current implementation.) |
| `tabs-begin` | `( -- )` | Open `<div class='tabs'>\n`. |
| `tabs-end` | `( -- )` | Close `</div>\n`. |
| `tab` | `( text$ id$ active? -- )` | Render a tab button. If `active?` is true, adds `active` CSS class. Emits `onclick="showPanel('id')"`. |
| `panel-begin` | `( id$ active? -- )` | Open a tab panel. If `active?` is true, the panel is visible (`class='panel active'`). Otherwise hidden (`class='panel'`). |
| `panel-end` | `( -- )` | Close `</div>\n`. |

**Tab/Panel pattern:**
```forth
tabs-begin
  s" Label" s" panel-id" true tab       \ active tab
  s" Label2" s" panel-id2" false tab
tabs-end

s" panel-id" true panel-begin
  \ ... content ...
panel-end

s" panel-id2" false panel-begin
  \ ... content ...
panel-end
```

#### Tables

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `table-begin` | `( -- )` | Open `<table class='table'>\n`. |
| `table-end` | `( -- )` | Close `</table>\n`. |
| `table-head-begin` | `( -- )` | Open `<thead><tr>`. |
| `table-head-end` | `( -- )` | Close `</tr></thead>\n`. |
| `table-body-begin` | `( -- )` | Open `<tbody>\n`. |
| `table-body-end` | `( -- )` | Close `</tbody>\n`. |
| `th-list` | `( n addr -- )` | Output `n` header cells from a counted-string array at `addr`. |

#### Grid Layout

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `grid-begin` | `( -- )` | Open `<div class='grid'>\n`. |
| `grid-end` | `( -- )` | Close `</div>\n`. |
| `grid-2` | `( -- )` | Open `<div class='grid grid-2'>\n` (2-column grid). |
| `grid-3` | `( -- )` | Open `<div class='grid grid-3'>\n` (3-column grid). |
| `grid-4` | `( -- )` | Open `<div class='grid grid-4'>\n` (4-column grid). |

#### Dashboard Layout

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `dashboard-begin` | `( -- )` | Open `<div class='dashboard'>\n`. Flex container. |
| `dashboard-end` | `( -- )` | Close `</div>\n`. |
| `dashboard-header` | `( title$ subtitle$ -- )` | Render dashboard header with `<h1>` title and `<p class='subtitle'>` subtitle. |
| `dashboard-main-begin` | `( -- )` | Open `<main class='dashboard-main'>\n`. |
| `dashboard-main-end` | `( -- )` | Close `</main>\n`. |

**Dashboard pattern:**
```forth
dashboard-begin
  s" Title" s" Subtitle" dashboard-header
  dashboard-main-begin
    \ ... stat cards, tabs, panels ...
  dashboard-main-end
dashboard-end
```

#### CSS

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `ui-css` | `( -- )` | Emit all UI component CSS rules inside `<style>` tags. Includes styles for: dashboard layout, sidebar, navigation, tabs, panels, cards, stat cards, badges, grid, tables, terms, and auto-fill grid. Also includes a responsive media query that collapses grids to single column at 768px. |

**CSS classes defined by `ui-css`:**

| Class | Purpose |
|-------|---------|
| `.dashboard` | Flex container, full viewport height |
| `.dashboard-header` | Header with bottom border |
| `.dashboard-main` | Flex-grow main content with padding |
| `.subtitle` | Muted text for subtitles |
| `.sidebar` | Fixed-width left sidebar |
| `.sidebar-section` | Sidebar section heading |
| `.nav-item` | Navigation link |
| `.nav-item:hover` | Navigation hover state |
| `.nav-item.active` | Active navigation item (purple) |
| `.tabs` | Tab container (flex with gap) |
| `.tab` | Tab button |
| `.tab:hover` | Tab hover state |
| `.tab.active` | Active tab (gradient background) |
| `.panel` | Tab panel (hidden by default) |
| `.panel.active` | Visible tab panel |
| `.card` | Card container (dark background, rounded) |
| `.card h3` | Card title (purple) |
| `.card p` | Card text (muted) |
| `.card-header` | Card header with bottom border |
| `.stat-card` | Stat card with gradient background |
| `.stat-value` | Large numeric value |
| `.stat-label` | Small label text |
| `.badge` | Inline badge pill |
| `.bg-primary` | Blue badge |
| `.bg-success` | Green badge |
| `.bg-warning` | Amber badge |
| `.bg-danger` | Red badge |
| `.bg-info` | Cyan badge |
| `.grid` | CSS grid container |
| `.grid-2` | 2-column grid |
| `.grid-3` | 3-column grid |
| `.grid-4` | 4-column grid |
| `.table` | Full-width table |
| `.table th` | Table header cell |
| `.table td` | Table data cell |
| `.table code` | Code within tables |
| `.term` | Term highlight (purple, bold) |
| `.grid-auto` | Auto-fill grid (min 280px columns) |

#### JavaScript

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `ui-js` | `( -- )` | Emit the `showPanel(id)` JavaScript function inside `<script>` tags. This function hides all `.panel` elements, removes `active` from all `.nav-item` and `.tab` elements, then shows the panel with the given `id` and marks `event.target` as active. |

---

### 3.6 core.fs -- Loader and Utilities

**Purpose:** Load the core libraries (`str.fs`, `html.fs`, `sql.fs`) and provide common utilities.

**Dependencies:** Loads `str.fs`, `html.fs`, `sql.fs`.

**Lines:** 68

**Note:** `core.fs` does NOT load `template.fs` or `ui.fs`.

#### Version

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `.fifth` | `( -- )` | Print the Fifth version banner to stdout: name, version (0.1.0), and URL. |

#### File Utilities

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `file-exists?` | `( addr u -- flag )` | Test if a file exists by attempting to open it read-only. Returns true if the file can be opened. |
| `with-file` | `( addr u xt -- )` | Open a file for writing, execute `xt` with the file descriptor on the stack, then close the file. `xt` signature: `( fid -- )`. Throws on file errors. |

#### Shell Utilities

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `$system` | `( addr u -- )` | Execute a shell command. Alias for `system`. |
| `open-file-cmd` | `( addr u -- )` | Open a file with the system default application. **macOS only** -- uses the `open` command. **Clobbers the primary buffer.** |
| `open-url` | `( addr u -- )` | Open a URL in the browser. Alias for `open-file-cmd`. **macOS only.** |

#### Debug Utilities

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `.s.` | `( -- )` | Print `Stack: ` followed by the current stack contents (via `.s`), then a newline. Non-destructive. |
| `??` | `( flag -- )` | Assert. If `flag` is false (0), print `ASSERTION FAILED` and abort. Otherwise continue. No context is provided on failure. |

#### Common Constants

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `KB` | `( n -- n*1024 )` | Multiply by 1024. |
| `MB` | `( n -- n*1024*1024 )` | Multiply by 1024 twice. |

---

## 4. Naming Conventions

| Pattern | Examples | Meaning |
|---------|----------|---------|
| `<tag>` / `</tag>` | `<div>` `</div>` `<h1>` `</h1>` | HTML open/close tags |
| `<tag.>` | `<div.>` `<span.>` `<header.>` | Tag with `class` attribute |
| `<tag#>` | `<div#>` | Tag with `id` attribute |
| `<tag#.>` | `<div#.>` | Tag with both `id` and `class` |
| `tag.` (trailing dot) | `h1.` `p.` `td.` `li.` `a.` `img.` | Convenience word: open + escaped content + close |
| `</tag>nl` | `</div>nl` `</p>nl` `</li>nl` | Close tag with trailing newline (single word) |
| `str-` prefix | `str-reset` `str+` `str$` `str-char` | Primary buffer operations |
| `str2-` prefix | `str2-reset` `str2+` `str2$` `str2-char` | Secondary buffer operations |
| `sql-` prefix | `sql-exec` `sql-open` `sql-row?` `sql-field` | SQLite operations |
| `-begin` / `-end` | `card-begin` `card-end` `tabs-begin` `tabs-end` | Container open/close pairs |
| `?` prefix | `?render` `?text` `?raw` | Conditional operations |
| `?` suffix | `sql-row?` `file-exists?` | Predicate/query words |
| `ui-` prefix | `ui-css` `ui-js` | CSS/JS emission |
| `html-` prefix | `html-head` `html-body` `html-begin` `html-end` `html-escape` `html-fid` | Document-level patterns |
| `.word` (leading dot) | `.fifth` `.s.` `.sql-field` `.sql-fields` | Print/display words |
| `>` suffix | `html>file` `html>stdout` `>t` `>tnl` | Output target setters / closers |
| `=` suffix | `attr=` `attr-text=` `href=` `src=` `type=` `name=` `value=` `placeholder=` | Attribute emission |
| `n>str` | `n>str` | Conversion words |

---

## 5. Security Model

### Escaping Rules

| Word | Escaping | Use for |
|------|----------|---------|
| `text` | Escapes `< > & ' "` to HTML entities | All user-facing content |
| `raw` | No escaping | Trusted HTML that you construct yourself |
| `attr-text=` | Escapes attribute values via `html-escape` | User-provided attribute content |
| `attr=` | No escaping | Trusted attribute values |
| `value=` | Escapes via `attr-text=` | Form input values |
| `placeholder=` | Escapes via `attr-text=` | Form placeholder text |

### Known Security Issues

1. **Attribute values in `<tag.>`, `<tag#>`, `<tag#.>` are NOT escaped.** Class and id values are output raw. If user data enters these attributes, it is an XSS vector. Only pass trusted, application-controlled strings.

2. **SQL injection.** No parameterized queries. SQL is string-concatenated and passed to the shell. Single quotes in SQL conflict with shell quoting. Database paths are not shell-escaped.

3. **Shell injection.** `system` calls are unescaped. Paths with spaces or special characters will break or can be exploited if user-controlled.

4. **Temp file exposure.** Query results written to world-readable temp files in `/tmp/`. Not cleaned up on exit.

### Security Rules

- Use `text` for ALL user-visible content. Never use `raw` for data that originates from user input or database queries.
- Use `attr-text=` or `value=` for attribute values that may contain user data.
- If using `raw`, add a comment explaining why the content is safe.
- Do not pass user-controlled strings to `system`, `sql-exec`, or path arguments without sanitization.

---

## 6. Output Patterns

### 6.1 Complete HTML Document

```forth
s" /tmp/output.html" w/o create-file throw html>file

s" Page Title" html-head        \ opens DOCTYPE, html, head, title -- leaves head OPEN
  <style> ... </style>          \ inject CSS while head is open
  ui-css                        \ component styles
html-body                       \ closes head, opens body

  \ ... page content ...

ui-js                           \ tab switching JavaScript (before </body>)
html-end                        \ closes body, html

html-fid @ close-file throw
```

### 6.2 SQL Query Iteration

```forth
s" path.db" s" SELECT a, b FROM t" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type       \ first column
    2dup 1 sql-field type       \ second column
    2drop
  else 2drop then
repeat 2drop
sql-close
```

### 6.3 Tabbed Interface

```forth
tabs-begin
  s" Label" s" panel-id" true tab       \ active tab
  s" Label2" s" panel-id2" false tab
tabs-end

s" panel-id" true panel-begin
  \ ... content ...
panel-end

s" panel-id2" false panel-begin
  \ ... content ...
panel-end
```

### 6.4 Dashboard Layout

```forth
dashboard-begin
  s" Title" s" Subtitle" dashboard-header
  dashboard-main-begin
    \ ... stat cards, tabs, panels ...
  dashboard-main-end
dashboard-end
```

### 6.5 Stat Cards in a Grid

```forth
grid-4
  42 s" Users" stat-card-n
  17 s" Active" stat-card-n
  3 s" Errors" stat-card-n
  99 s" Uptime %" stat-card-n
grid-end
```

### 6.6 SQL Results as a Table

```forth
table-begin
  <thead> <tr> s" Name" th. s" Email" th. </tr> </thead>
  <tbody>
  s" db.db" s" SELECT name, email FROM users" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      <tr>
        2dup 0 sql-field td.
        2dup 1 sql-field td.
        2drop
      </tr>
    else 2drop then
  repeat 2drop
  sql-close
  </tbody>
table-end
```

### 6.7 Conditional Rendering

```forth
\ Render only if condition is true
has-sidebar ['] emit-sidebar ?render

\ Conditional text
show-warning s" Warning: check your settings" ?text
```

### 6.8 Deferred Slots

```forth
slot: @header
slot: @sidebar
slot: @main

: my-header s" Dashboard" h1. ;
: my-main s" Hello World" p. ;

' my-header ->slot @header
' my-main ->slot @main

\ Now calling @header executes my-header
@header
@main
```

---

## 7. Version

**Fifth 0.1.0**

- 6 libraries
- ~1,100 lines of library code
- 2 example applications (~500 lines)
- Requires: `sqlite3` CLI

---

## 8. Complete Word Index

Every word Fifth defines, organized alphabetically. Standard Forth words are not listed.

| Word | Library | Stack Effect | Description |
|------|---------|-------------|-------------|
| `$system` | core.fs | `( addr u -- )` | Execute shell command |
| `.fifth` | core.fs | `( -- )` | Print Fifth version banner |
| `.s.` | core.fs | `( -- )` | Pretty print stack |
| `.sql-field` | sql.fs | `( addr u n -- addr u )` | Print nth field, keep row |
| `.sql-fields` | sql.fs | `( addr u n -- )` | Print first n fields, tab-separated |
| `</` | html.fs | `( -- )` | Output `</` |
| `</a>` | html.fs | `( -- )` | Close anchor tag |
| `</article>` | html.fs | `( -- )` | Close article tag |
| `</aside>` | html.fs | `( -- )` | Close aside tag |
| `</blockquote>` | html.fs | `( -- )` | Close blockquote tag |
| `</body>` | html.fs | `( -- )` | Close body tag |
| `</button>` | html.fs | `( -- )` | Close button tag |
| `</code>` | html.fs | `( -- )` | Close code tag |
| `</div>` | html.fs | `( -- )` | Close div tag |
| `</div>nl` | html.fs | `( -- )` | Close div tag with newline |
| `</em>` | html.fs | `( -- )` | Close em tag |
| `</footer>` | html.fs | `( -- )` | Close footer tag |
| `</form>` | html.fs | `( -- )` | Close form tag |
| `</h1>` | html.fs | `( -- )` | Close h1 tag |
| `</h2>` | html.fs | `( -- )` | Close h2 tag |
| `</h3>` | html.fs | `( -- )` | Close h3 tag |
| `</h4>` | html.fs | `( -- )` | Close h4 tag |
| `</head>` | html.fs | `( -- )` | Close head tag |
| `</header>` | html.fs | `( -- )` | Close header tag |
| `</html>` | html.fs | `( -- )` | Close html tag |
| `</label>` | html.fs | `( -- )` | Close label tag |
| `</li>` | html.fs | `( -- )` | Close li tag |
| `</li>nl` | html.fs | `( -- )` | Close li tag with newline |
| `</main>` | html.fs | `( -- )` | Close main tag |
| `</nav>` | html.fs | `( -- )` | Close nav tag |
| `</ol>` | html.fs | `( -- )` | Close ol tag |
| `</option>` | html.fs | `( -- )` | Close option tag |
| `</p>` | html.fs | `( -- )` | Close p tag |
| `</p>nl` | html.fs | `( -- )` | Close p tag with newline |
| `</pre>` | html.fs | `( -- )` | Close pre tag |
| `</script>` | html.fs | `( -- )` | Close script tag |
| `</section>` | html.fs | `( -- )` | Close section tag |
| `</select>` | html.fs | `( -- )` | Close select tag |
| `</span>` | html.fs | `( -- )` | Close span tag |
| `</strong>` | html.fs | `( -- )` | Close strong tag |
| `</style>` | html.fs | `( -- )` | Close style tag |
| `</table>` | html.fs | `( -- )` | Close table tag |
| `</tag>` | html.fs | `( name$ -- )` | Close named tag |
| `</tag>nl` | html.fs | `( name$ -- )` | Close named tag with newline |
| `</tbody>` | html.fs | `( -- )` | Close tbody tag |
| `</td>` | html.fs | `( -- )` | Close td tag |
| `</textarea>` | html.fs | `( -- )` | Close textarea tag |
| `</th>` | html.fs | `( -- )` | Close th tag |
| `</thead>` | html.fs | `( -- )` | Close thead tag |
| `</title>` | html.fs | `( -- )` | Close title tag |
| `</tr>` | html.fs | `( -- )` | Close tr tag |
| `</ul>` | html.fs | `( -- )` | Close ul tag |
| `<!doctype>` | html.fs | `( -- )` | Output DOCTYPE declaration |
| `<a` | html.fs | `( -- )` | Open anchor (for attributes) |
| `<article>` | html.fs | `( -- )` | Open article tag |
| `<aside>` | html.fs | `( -- )` | Open aside tag |
| `<aside.>` | html.fs | `( class$ -- )` | Open aside with class |
| `<blockquote>` | html.fs | `( -- )` | Open blockquote tag |
| `<body>` | html.fs | `( -- )` | Open body tag |
| `<br/>` | html.fs | `( -- )` | Self-closing line break |
| `<button` | html.fs | `( -- )` | Open button (for attributes) |
| `<button.>` | html.fs | `( class$ -- )` | Open button with class |
| `<button>` | html.fs | `( -- )` | Open button tag |
| `<code>` | html.fs | `( -- )` | Open code tag |
| `<div.>` | html.fs | `( class$ -- )` | Open div with class |
| `<div.>nl` | html.fs | `( class$ -- )` | Open div with class and newline |
| `<div#.>` | html.fs | `( id$ class$ -- )` | Open div with id and class |
| `<div#>` | html.fs | `( id$ -- )` | Open div with id |
| `<div>` | html.fs | `( -- )` | Open div tag |
| `<em>` | html.fs | `( -- )` | Open em tag |
| `<footer>` | html.fs | `( -- )` | Open footer tag |
| `<form>` | html.fs | `( -- )` | Open form tag |
| `<h1>` | html.fs | `( -- )` | Open h1 tag |
| `<h2>` | html.fs | `( -- )` | Open h2 tag |
| `<h3>` | html.fs | `( -- )` | Open h3 tag |
| `<h4>` | html.fs | `( -- )` | Open h4 tag |
| `<head>` | html.fs | `( -- )` | Open head tag |
| `<header.>` | html.fs | `( class$ -- )` | Open header with class |
| `<header>` | html.fs | `( -- )` | Open header tag |
| `<html>` | html.fs | `( -- )` | Open html tag |
| `<img` | html.fs | `( -- )` | Open img (for attributes) |
| `<input` | html.fs | `( -- )` | Open input (for attributes) |
| `<label>` | html.fs | `( -- )` | Open label tag |
| `<li.>` | html.fs | `( class$ -- )` | Open li with class |
| `<li>` | html.fs | `( -- )` | Open li tag |
| `<link` | html.fs | `( -- )` | Open link (for attributes) |
| `<main.>` | html.fs | `( class$ -- )` | Open main with class |
| `<main>` | html.fs | `( -- )` | Open main tag |
| `<meta` | html.fs | `( -- )` | Open meta (for attributes) |
| `<nav>` | html.fs | `( -- )` | Open nav tag |
| `<ol>` | html.fs | `( -- )` | Open ol tag |
| `<option>` | html.fs | `( -- )` | Open option tag |
| `<p.>` | html.fs | `( class$ -- )` | Open p with class |
| `<p>` | html.fs | `( -- )` | Open p tag |
| `<pre>` | html.fs | `( -- )` | Open pre tag |
| `<script>` | html.fs | `( -- )` | Open script tag |
| `<section.>` | html.fs | `( class$ -- )` | Open section with class |
| `<section>` | html.fs | `( -- )` | Open section tag |
| `<select>` | html.fs | `( -- )` | Open select tag |
| `<span.>` | html.fs | `( class$ -- )` | Open span with class |
| `<span>` | html.fs | `( -- )` | Open span tag |
| `<strong>` | html.fs | `( -- )` | Open strong tag |
| `<style>` | html.fs | `( -- )` | Open style tag |
| `<table.>` | html.fs | `( class$ -- )` | Open table with class |
| `<table>` | html.fs | `( -- )` | Open table tag |
| `<tag#.>` | html.fs | `( id$ class$ name$ -- )` | Open named tag with id and class |
| `<tag#>` | html.fs | `( id$ name$ -- )` | Open named tag with id |
| `<tag.>` | html.fs | `( class$ name$ -- )` | Open named tag with class |
| `<tag.>nl` | html.fs | `( class$ name$ -- )` | Open named tag with class, newline |
| `<tag>` | html.fs | `( name$ -- )` | Open named tag |
| `<tag>nl` | html.fs | `( name$ -- )` | Open named tag with newline |
| `<tbody>` | html.fs | `( -- )` | Open tbody tag |
| `<td.>` | html.fs | `( class$ -- )` | Open td with class |
| `<td>` | html.fs | `( -- )` | Open td tag |
| `<textarea>` | html.fs | `( -- )` | Open textarea tag |
| `<th>` | html.fs | `( -- )` | Open th tag |
| `<thead>` | html.fs | `( -- )` | Open thead tag |
| `<title>` | html.fs | `( -- )` | Open title tag |
| `<tr.>` | html.fs | `( class$ -- )` | Open tr with class |
| `<tr>` | html.fs | `( -- )` | Open tr tag |
| `<ul.>` | html.fs | `( class$ -- )` | Open ul with class |
| `<ul>` | html.fs | `( -- )` | Open ul tag |
| `/>` | html.fs | `( -- )` | Output `/>` |
| `>t` | html.fs | `( -- )` | Output `>` |
| `>tnl` | html.fs | `( -- )` | Output `>` with newline |
| `??` | core.fs | `( flag -- )` | Assert: abort if false |
| `?raw` | template.fs | `( flag addr u -- )` | Conditional raw output |
| `?render` | template.fs | `( flag xt -- )` | Conditional execution |
| `?text` | template.fs | `( flag addr u -- )` | Conditional escaped output |
| `->slot` | template.fs | `( xt "name" -- )` | Fill a named slot |
| `KB` | core.fs | `( n -- n*1024 )` | Kilobytes |
| `MB` | core.fs | `( n -- n*1024*1024 )` | Megabytes |
| `a.` | html.fs | `( text$ url$ -- )` | Link convenience |
| `a>` | html.fs | `( -- )` | Close anchor opening tag |
| `attr-text=` | html.fs | `( name$ value$ -- )` | Escaped attribute |
| `attr=` | html.fs | `( name$ value$ -- )` | Raw attribute |
| `badge` | ui.fs | `( text$ class$ -- )` | Render badge span |
| `badge-danger` | ui.fs | `( text$ -- )` | Red badge |
| `badge-info` | ui.fs | `( text$ -- )` | Cyan badge |
| `badge-primary` | ui.fs | `( text$ -- )` | Blue badge |
| `badge-success` | ui.fs | `( text$ -- )` | Green badge |
| `badge-warning` | ui.fs | `( text$ -- )` | Amber badge |
| `begin-capture` | template.fs | `( -- )` | Start capturing HTML output |
| `capture-fid` | template.fs | variable | Capture file descriptor |
| `capture-file` | template.fs | `2constant` | Capture temp file path |
| `capture-mode` | template.fs | variable | Saved html-fid during capture |
| `card` | ui.fs | `( title$ body-xt -- )` | Complete card with title and body |
| `card-begin` | ui.fs | `( -- )` | Open card container |
| `card-body-begin` | ui.fs | `( -- )` | Open card body |
| `card-body-end` | ui.fs | `( -- )` | Close card body |
| `card-end` | ui.fs | `( -- )` | Close card container |
| `card-header` | ui.fs | `( title$ -- )` | Card header with title |
| `card-with-badge` | ui.fs | `( title$ badge$ badge-class$ body-xt -- )` | Card with badge |
| `css` | html.fs | `( css-string$ -- )` | CSS in style tags |
| `css-rule` | html.fs | `( selector$ properties$ -- )` | CSS rule output |
| `current-tab` | ui.fs | variable | (Unused) current tab state |
| `dashboard-begin` | ui.fs | `( -- )` | Open dashboard container |
| `dashboard-end` | ui.fs | `( -- )` | Close dashboard container |
| `dashboard-header` | ui.fs | `( title$ subtitle$ -- )` | Dashboard header |
| `dashboard-main-begin` | ui.fs | `( -- )` | Open dashboard main area |
| `dashboard-main-end` | ui.fs | `( -- )` | Close dashboard main area |
| `end-capture` | template.fs | `( -- addr u )` | Stop capture, return content |
| `field-length` | str.fs | `( addr u delim -- len )` | Length until delimiter |
| `file-exists?` | core.fs | `( addr u -- flag )` | Test file existence |
| `fragment:` | template.fs | `( "name" -- )` | Define reusable fragment |
| `grid-2` | ui.fs | `( -- )` | 2-column grid |
| `grid-3` | ui.fs | `( -- )` | 3-column grid |
| `grid-4` | ui.fs | `( -- )` | 4-column grid |
| `grid-begin` | ui.fs | `( -- )` | Open grid container |
| `grid-end` | ui.fs | `( -- )` | Close grid container |
| `h>>` | html.fs | `( addr u -- )` | Write to HTML output |
| `h>>line` | html.fs | `( addr u -- )` | Write line to HTML output |
| `h>>nl` | html.fs | `( -- )` | Write newline to HTML output |
| `h1.` | html.fs | `( text$ -- )` | H1 with escaped text |
| `h2.` | html.fs | `( text$ -- )` | H2 with escaped text |
| `h3.` | html.fs | `( text$ -- )` | H3 with escaped text |
| `h4.` | html.fs | `( text$ -- )` | H4 with escaped text |
| `href=` | html.fs | `( url$ -- )` | Href attribute |
| `html-begin` | html.fs | `( title$ -- )` | Start document (head+body) |
| `html-body` | html.fs | `( -- )` | Close head, open body |
| `html-end` | html.fs | `( -- )` | Close body and html |
| `html-escape` | html.fs | `( addr u -- addr' u' )` | Escape HTML entities |
| `html-fid` | html.fs | variable | HTML output file descriptor |
| `html-head` | html.fs | `( title$ -- )` | Start document, leave head open |
| `html>file` | html.fs | `( fid -- )` | Set output to file |
| `html>stdout` | html.fs | `( -- )` | Set output to stdout |
| `img.` | html.fs | `( alt$ src$ -- )` | Image convenience |
| `img>` | html.fs | `( -- )` | Close img opening tag |
| `input>` | html.fs | `( -- )` | Close input opening tag |
| `join-render` | template.fs | `( n xt sep$ -- )` | Render with separator |
| `li.` | html.fs | `( text$ -- )` | Li with escaped text |
| `line-buf` | str.fs | storage | Line buffer (512 bytes) |
| `line-max` | str.fs | 512 | Line buffer capacity |
| `meta>` | html.fs | `( -- )` | Close meta tag |
| `n>str` | str.fs | `( n -- addr u )` | Number to string |
| `name=` | html.fs | `( name$ -- )` | Name attribute |
| `nav-begin` | ui.fs | `( -- )` | Open nav container |
| `nav-end` | ui.fs | `( -- )` | Close nav container |
| `nav-item` | ui.fs | `( text$ href$ active? -- )` | Navigation link |
| `nav-item-js` | ui.fs | `( text$ panel-id$ active? -- )` | JS navigation item |
| `nl` | html.fs | `( -- )` | Output newline |
| `open-file-cmd` | core.fs | `( addr u -- )` | Open file with OS (macOS) |
| `open-url` | core.fs | `( addr u -- )` | Open URL in browser |
| `p.` | html.fs | `( text$ -- )` | P with escaped text |
| `panel-begin` | ui.fs | `( id$ active? -- )` | Open tab panel |
| `panel-end` | ui.fs | `( -- )` | Close tab panel |
| `parse-comma` | str.fs | `( addr u n -- addr u field$ )` | Parse comma field |
| `parse-delim` | str.fs | `( addr u n delim -- addr u field$ )` | Parse delimited field |
| `parse-pipe` | str.fs | `( addr u n -- addr u field$ )` | Parse pipe field |
| `parse-tab` | str.fs | `( addr u n -- addr u field$ )` | Parse tab field |
| `placeholder=` | html.fs | `( text$ -- )` | Placeholder attribute (escaped) |
| `raw` | html.fs | `( addr u -- )` | Output raw HTML |
| `rawln` | html.fs | `( addr u -- )` | Output raw HTML with newline |
| `sidebar-begin` | ui.fs | `( -- )` | Open sidebar |
| `sidebar-end` | ui.fs | `( -- )` | Close sidebar |
| `sidebar-section` | ui.fs | `( title$ -- )` | Sidebar section heading |
| `skip-to-delim` | str.fs | `( addr u delim -- addr' u' )` | Skip to delimiter |
| `slot:` | template.fs | `( "name" -- )` | Define deferred slot |
| `sql-close` | sql.fs | `( -- )` | Close result file |
| `sql-cmd-count` | sql.fs | `( db$ sql$ -- )` | Build count command |
| `sql-cmd-query` | sql.fs | `( db$ sql$ -- )` | Build query command |
| `sql-count` | sql.fs | `( db$ sql$ -- n )` | Execute COUNT query |
| `sql-count-fid` | sql.fs | variable | Count result file descriptor |
| `sql-count-output` | sql.fs | `2constant` | Count result file path |
| `sql-dump` | sql.fs | `( db$ sql$ -- )` | Dump query to stdout |
| `sql-each` | sql.fs | `( db$ sql$ xt -- )` | Iterate with callback |
| `sql-exec` | sql.fs | `( db$ sql$ -- )` | Execute query to file |
| `sql-fid` | sql.fs | variable | Query result file descriptor |
| `sql-field` | sql.fs | `( addr u n -- addr u field$ )` | Extract nth field |
| `sql-open` | sql.fs | `( -- )` | Open result file |
| `sql-output` | sql.fs | `2constant` | Query result file path |
| `sql-row?` | sql.fs | `( -- addr u flag )` | Read next row |
| `sql-schema` | sql.fs | `( db$ table$ -- )` | Get table schema |
| `sql-table-count` | sql.fs | `( db$ table$ -- n )` | Count table rows |
| `sql-tables` | sql.fs | `( db$ -- )` | List all tables |
| `src=` | html.fs | `( url$ -- )` | Src attribute |
| `stat-card` | ui.fs | `( value$ label$ -- )` | Stat card with string value |
| `stat-card-n` | ui.fs | `( n label$ -- )` | Stat card with numeric value |
| `str$` | str.fs | `( -- addr u )` | Get primary buffer contents |
| `str+` | str.fs | `( addr u -- )` | Append to primary buffer |
| `str-buf` | str.fs | storage | Primary buffer (1024 bytes) |
| `str-char` | str.fs | `( c -- )` | Append char to primary buffer |
| `str-find-char` | str.fs | `( addr u c -- addr' u' \| 0 0 )` | Find character |
| `str-len` | str.fs | variable | Primary buffer length |
| `str-max` | str.fs | 1024 | Primary buffer capacity |
| `str-reset` | str.fs | `( -- )` | Clear primary buffer |
| `str2$` | str.fs | `( -- addr u )` | Get secondary buffer contents |
| `str2+` | str.fs | `( addr u -- )` | Append to secondary buffer |
| `str2-buf` | str.fs | storage | Secondary buffer (1024 bytes) |
| `str2-char` | str.fs | `( c -- )` | Append char to secondary buffer |
| `str2-len` | str.fs | variable | Secondary buffer length |
| `str2-max` | str.fs | 1024 | Secondary buffer capacity |
| `str2-reset` | str.fs | `( -- )` | Clear secondary buffer |
| `str=` | str.fs | `( addr1 u1 addr2 u2 -- flag )` | String equality |
| `tab` | ui.fs | `( text$ id$ active? -- )` | Tab button |
| `table-begin` | ui.fs | `( -- )` | Open styled table |
| `table-body-begin` | ui.fs | `( -- )` | Open table body |
| `table-body-end` | ui.fs | `( -- )` | Close table body |
| `table-end` | ui.fs | `( -- )` | Close styled table |
| `table-head-begin` | ui.fs | `( -- )` | Open table head + row |
| `table-head-end` | ui.fs | `( -- )` | Close table head row |
| `tabs-begin` | ui.fs | `( -- )` | Open tabs container |
| `tabs-end` | ui.fs | `( -- )` | Close tabs container |
| `tag/` | html.fs | `( name$ -- )` | Self-closing tag |
| `td-code.` | html.fs | `( text$ -- )` | Td with code, escaped |
| `td-raw.` | html.fs | `( html$ -- )` | Td with raw HTML |
| `td.` | html.fs | `( text$ -- )` | Td with escaped text |
| `text` | html.fs | `( addr u -- )` | Output escaped text |
| `th-list` | ui.fs | `( n addr -- )` | Output n header cells |
| `th.` | html.fs | `( text$ -- )` | Th with escaped text |
| `times` | template.fs | `( n xt -- )` | Execute xt n times |
| `type=` | html.fs | `( type$ -- )` | Type attribute |
| `ui-css` | ui.fs | `( -- )` | Emit all component CSS |
| `ui-js` | ui.fs | `( -- )` | Emit tab-switching JS |
| `value=` | html.fs | `( value$ -- )` | Value attribute (escaped) |
| `with-file` | core.fs | `( addr u xt -- )` | Execute with file open |

**Total: 206 words** across 6 libraries.

---

*Fifth: Forth Libraries for the Fifth Age*
*Version 0.1.0 -- 6 libraries, ~1,100 lines, 206 words*
*Requires: sqlite3 CLI*
