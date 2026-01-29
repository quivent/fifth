# LIBRARIES.md - Proposed New Libraries for Fifth

> **Design principles**: Each library does one thing well. Shell out to Unix tools
> rather than reinvent them. Keep every library under 300 lines. Compose with
> existing Fifth words.

---

## Current State

Fifth ships ~1,600 lines across 6 libraries:

| Library | Lines | Purpose |
|---------|-------|---------|
| `str.fs` | 147 | String buffers, parsing, field extraction |
| `html.fs` | 336 | HTML5 tags, escaping, document structure |
| `sql.fs` | 152 | SQLite shell interface, result iteration |
| `template.fs` | 123 | Slots, conditional rendering, layouts |
| `ui.fs` | 261 | Cards, badges, tabs, grids, dashboards |
| `core.fs` | 67 | Loader + utilities |

What follows are proposals for new libraries that would make Fifth a complete
toolkit for building real applications. Every proposal is grounded in Fifth's
shell-out philosophy: use `curl` for HTTP, `jq` for JSON, `openssl` for hashing,
`awk` for text processing. The operating system is the library.

---

## 1. Data Format Libraries

### 1.1 `json.fs` - JSON Generation

**Why it matters**: Every modern application needs to produce JSON. API responses,
configuration files, data export, logging -- JSON is the lingua franca.
Fifth already generates HTML; JSON generation follows the same buffer pattern.

**Proposed API**:

```forth
require ~/fifth/lib/json.fs

\ --- Object construction ---
json{                           ( -- )         \ Start JSON object
  s" name" s" Fifth" j:s        ( key$ val$ -- ) \ String property
  s" version" 1 j:n             ( key$ n -- )    \ Number property
  s" active" true j:b           ( key$ flag -- ) \ Boolean property
  s" tags" json[                ( key$ -- )      \ Start array
    s" forth" j,s               ( val$ -- )      \ Array string element
    s" unix" j,s
  ]json                         ( -- )           \ End array
}json                           ( -- )           \ End object, flush

\ Output:
\ {"name":"Fifth","version":1,"active":true,"tags":["forth","unix"]}

\ --- Standalone helpers ---
s" hello world" json-escape     ( addr u -- addr u' ) \ Escape for JSON strings
42 json-number                  ( n -- )              \ Emit number
true json-bool                  ( flag -- )           \ Emit true/false
json-null                       ( -- )                \ Emit null

\ --- File output ---
s" /tmp/data.json" w/o create-file throw json>file
\ ... build JSON ...
json-fid @ close-file throw

\ --- Pipe to jq for pretty-printing ---
s" /tmp/data.json" json-pretty  ( file$ -- ) \ Shell: cat file | jq .
```

**Implementation approach**: Pure Forth. Mirror the `html-fid` / `h>>` pattern
from `html.fs`. Track nesting depth and comma state with variables. `json-escape`
handles `"`, `\`, newlines, tabs, and control characters. No parsing -- generation only.
For parsing, shell out to `jq`.

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`

---

### 1.2 `csv.fs` - CSV Generation and Parsing

**Why it matters**: CSV is the universal data exchange format. Database exports,
spreadsheet imports, reporting pipelines -- all need CSV. Fifth already parses
pipe-delimited and comma-delimited fields via `str.fs`; this library adds
proper RFC 4180 generation with quoting.

**Proposed API**:

```forth
require ~/fifth/lib/csv.fs

\ --- Generation ---
s" /tmp/report.csv" w/o create-file throw csv>file

s" Name" csv-field s" Email" csv-field s" Score" csv-field csv-row
\ Output: Name,Email,Score\r\n

s" Alice" csv-field s" alice@example.com" csv-field 95 csv-field-n csv-row
s" Bob \"Jr\"" csv-field  \ auto-quotes: "Bob ""Jr"""
csv-flush

\ --- Reading (shell out to awk) ---
s" /tmp/data.csv" csv-open      ( file$ -- )
begin csv-next? while           ( -- addr u flag )
  2dup 0 csv-col type cr        ( addr u n -- addr u field$ )
  2drop
repeat 2drop
csv-close

\ --- SQL bridge: CSV to SQLite import ---
s" data.csv" s" mydb.db" s" mytable" csv>sqlite
\ Shell: sqlite3 mydb.db '.import --csv data.csv mytable'

\ --- Column count ---
s" /tmp/data.csv" csv-cols      ( file$ -- n ) \ Count columns in first row
```

**Implementation approach**: Generation is pure Forth -- track field position,
auto-quote fields containing commas/quotes/newlines. Parsing shells out to
`awk -F,` for field extraction. The `csv>sqlite` bridge uses SQLite's built-in
`.import` command.

**Estimated size**: ~180 lines

**Dependencies**: `str.fs`, `sql.fs` (optional, for `csv>sqlite`)

---

### 1.3 `ini.fs` - INI/TOML-Subset Configuration

**Why it matters**: Configuration files. Every real application needs to read
settings from somewhere. INI is the simplest format that works.

**Proposed API**:

```forth
require ~/fifth/lib/ini.fs

\ --- Reading config files ---
s" ~/.config/myapp.ini" ini-load    ( file$ -- )

\ Lookup values
s" database" s" host" ini-get       ( section$ key$ -- addr u flag )
\ Returns: "localhost" -1

s" database" s" port" ini-get-n     ( section$ key$ -- n flag )
\ Returns: 5432 -1

s" server" s" debug" ini-get-bool   ( section$ key$ -- flag flag )
\ Returns: true -1

\ Default values
s" database" s" host" s" localhost" ini-get-or
                                    ( section$ key$ default$ -- addr u )

\ --- Writing config files ---
s" /tmp/app.ini" w/o create-file throw ini>file
s" database" ini-section            ( section$ -- ) \ [database]
s" host" s" localhost" ini-set      ( key$ val$ -- ) \ host = localhost
s" port" 5432 ini-set-n             ( key$ n -- )    \ port = 5432
ini-flush
```

**Implementation approach**: Reading parses line-by-line with Gforth file I/O.
Track current section in a variable. Store key-value pairs in a fixed-size
array (e.g., 64 entries max). Writing is pure string output.

**Estimated size**: ~200 lines

**Dependencies**: `str.fs`

---

## 2. Network Libraries

### 2.1 `http.fs` - HTTP Client

**Why it matters**: The network is the computer. Fetching APIs, downloading files,
posting data, checking health endpoints. `curl` is available everywhere and
handles TLS, redirects, authentication, and every HTTP edge case. Do not
reimplement what `curl` already does perfectly.

**Proposed API**:

```forth
require ~/fifth/lib/http.fs

\ --- GET request ---
s" https://api.example.com/users" http-get
\ Result in /tmp/fifth-http-body.txt

http-body slurp-file type       ( -- addr u ) \ Read response body
http-status .                   ( -- n )      \ HTTP status code (e.g., 200)

\ --- GET with headers ---
s" Authorization: Bearer tok123" http-header
s" Accept: application/json" http-header
s" https://api.example.com/me" http-get

\ --- POST request ---
s" https://api.example.com/data" s" {\"key\":\"value\"}" http-post-json
s" https://api.example.com/form" s" name=alice&age=30" http-post-form

\ --- Download file ---
s" https://example.com/file.tar.gz" s" /tmp/file.tar.gz" http-download

\ --- URL encoding ---
s" hello world & goodbye" url-encode type
\ Output: hello%20world%20%26%20goodbye

s" hello%20world" url-decode type
\ Output: hello world

\ --- Response headers ---
http-header-file slurp-file type  ( -- addr u ) \ Raw response headers
```

**Implementation approach**: Shell out to `curl`. Build command strings in
`str-buf`. Use `-s` (silent), `-o` (output file), `-w '%{http_code}'` (status
code to separate file), `-D` (dump headers). Each request writes body to
`/tmp/fifth-http-body.txt`, headers to `/tmp/fifth-http-headers.txt`, status
to `/tmp/fifth-http-status.txt`. URL encoding is pure Forth (character-by-character
percent encoding).

**Estimated size**: ~200 lines

**Dependencies**: `str.fs`, requires `curl`

---

### 2.2 `url.fs` - URL Parsing and Construction

**Why it matters**: Building URLs with query parameters, extracting components,
joining paths. Every HTTP interaction starts with constructing the right URL.

**Proposed API**:

```forth
require ~/fifth/lib/url.fs

\ --- URL construction ---
url-reset
s" https" url-scheme
s" api.example.com" url-host
s" /v2/users" url-path
s" page" s" 1" url-param
s" sort" s" name" url-param
url$ type
\ Output: https://api.example.com/v2/users?page=1&sort=name

\ --- Query string building ---
query-reset
s" q" s" forth language" query-param   \ auto-encodes spaces
s" limit" 10 query-param-n
query$ type
\ Output: q=forth%20language&limit=10

\ --- Path joining ---
s" /api/v2" s" users" path-join type   \ /api/v2/users
s" /api/v2/" s" /users" path-join type \ /api/v2/users (normalizes slashes)
```

**Implementation approach**: Pure Forth. URL buffer built with `str-reset` / `str+`.
Query parameter encoding reuses `url-encode` from `http.fs` or implements its
own. Track whether `?` or `&` is needed with a variable.

**Estimated size**: ~120 lines

**Dependencies**: `str.fs`

---

## 3. Testing Library

### 3.1 `test.fs` - Test Framework

**Why it matters**: You cannot build reliable software without tests. Fifth
currently has `??` (assert) in `core.fs` but no test runner, no reporting,
no isolation. A real test framework makes the difference between a toy and
a tool.

**Proposed API**:

```forth
require ~/fifth/lib/test.fs

\ --- Define tests ---
testing json.fs

t{ s" hello" json-escape s" hello" str= }t  \ passes silently
t{ 1 1 + 2 = }t                             \ passes
t{ 1 1 + 3 = }t                             \ FAIL: expected true

\ --- Named tests ---
test: addition-works
  1 1 + 2 assert=
  0 0 + 0 assert=
;test

test: string-equality
  s" hello" s" hello" assert-str=
  s" hello" s" world" assert-str<>
;test

\ --- Assertions ---
assert=         ( a b -- )            \ Assert equal (numbers)
assert<>        ( a b -- )            \ Assert not equal
assert-true     ( flag -- )           \ Assert truthy
assert-false    ( flag -- )           \ Assert falsy
assert-str=     ( a1 u1 a2 u2 -- )   \ Assert string equal
assert-str<>    ( a1 u1 a2 u2 -- )   \ Assert string not equal
assert>0        ( n -- )              \ Assert positive

\ --- Test runner output ---
\ ....F..
\ FAIL: addition-works - expected 3, got 2
\ 6 tests, 5 passed, 1 failed

\ --- Summary ---
test-summary    ( -- )                \ Print results and set exit code
\ Exit code 0 = all passed, 1 = failures

\ --- File-level test runner ---
\ gforth ~/fifth/tests/test-str.fs
\ gforth ~/fifth/tests/test-json.fs
\ Or run all:
\ for f in ~/fifth/tests/test-*.fs; do gforth "$f"; done
```

**Implementation approach**: Pure Forth. Variables track pass/fail counts.
`t{` saves stack depth; `}t` checks the result is true. `test:` / `;test`
define named test words and catch exceptions. `assert=` compares and either
increments pass count or prints failure with context. `test-summary` prints
the tally and calls `bye` with appropriate exit code for CI integration.

**Estimated size**: ~180 lines

**Dependencies**: `str.fs` (for string assertions)

---

## 4. Text Processing Libraries

### 4.1 `fmt.fs` - String Formatting

**Why it matters**: Building human-readable output. Log lines, table columns,
padded strings, number formatting. The existing `n>str` handles integers;
this library handles everything else.

**Proposed API**:

```forth
require ~/fifth/lib/fmt.fs

\ --- Padding ---
s" hello" 20 pad-right type    ( addr u width -- addr u' ) \ "hello               "
s" hello" 20 pad-left type     ( addr u width -- addr u' ) \ "               hello"
s" hello" 20 pad-center type   ( addr u width -- addr u' ) \ "       hello        "

\ --- Number formatting ---
1234567 fmt-comma type          ( n -- addr u ) \ "1,234,567"
42 3 fmt-pad0 type              ( n width -- addr u ) \ "042"

\ --- Truncation ---
s" hello world" 8 truncate type ( addr u max -- addr u' ) \ "hello..."

\ --- Repeat ---
s" =" 40 str-repeat type        ( addr u n -- addr u' ) \ "======...======"
[char] - 40 char-repeat type    ( c n -- addr u )       \ "------...------"

\ --- Case conversion ---
s" hello" upper type            ( addr u -- addr u' ) \ "HELLO"
s" HELLO" lower type            ( addr u -- addr u' ) \ "hello"

\ --- Trim ---
s"   hello  " trim type         ( addr u -- addr u' ) \ "hello"
s"   hello  " trim-left type    \ "hello  "
s"   hello  " trim-right type   \ "   hello"

\ --- Simple interpolation ---
\ Uses % as placeholder
s" Hello, %!" s" World" fmt1 type   ( template$ val$ -- addr u )
\ "Hello, World!"
```

**Implementation approach**: Pure Forth. Padding uses `str-buf` to build
output with spaces. Case conversion operates character-by-character (add/subtract
32 for ASCII). Trim scans from edges for non-space characters.

**Estimated size**: ~200 lines

**Dependencies**: `str.fs`

---

### 4.2 `md.fs` - Markdown Generation

**Why it matters**: READMEs, changelogs, documentation, GitHub issues, notes.
Markdown is the documentation language of the development world. Generating it
is even simpler than generating HTML.

**Proposed API**:

```forth
require ~/fifth/lib/md.fs

\ --- Output target ---
s" /tmp/doc.md" w/o create-file throw md>file

\ --- Headings ---
s" Title" md-h1        \ # Title\n
s" Section" md-h2      \ ## Section\n
s" Subsection" md-h3   \ ### Subsection\n

\ --- Text ---
s" A paragraph." md-p  \ A paragraph.\n\n
s" Bold text" md-bold type   \ **Bold text**
s" Italic" md-italic type    \ *Italic*
s" code" md-code type        \ `code`

\ --- Lists ---
s" First item" md-li    \ - First item\n
s" Second item" md-li
md-nl                   \ blank line after list

\ --- Code blocks ---
s" forth" md-fence      \ ```forth\n
s" : hello .\" Hi\" ;" md-line
md-fence-end            \ ```\n

\ --- Tables ---
3 md-table-begin
  s" Name" md-th s" Type" md-th s" Size" md-th md-table-sep
  s" str.fs" md-td s" Library" md-td s" 147" md-td md-table-row
  s" html.fs" md-td s" Library" md-td s" 336" md-td md-table-row
md-table-end

\ Output:
\ | Name | Type | Size |
\ |------|------|------|
\ | str.fs | Library | 147 |
\ | html.fs | Library | 336 |

\ --- Links and images ---
s" Fifth" s" https://github.com/..." md-link  \ [Fifth](https://...)
s" Screenshot" s" ./img.png" md-img           \ ![Screenshot](./img.png)

\ --- Horizontal rule ---
md-hr   \ ---\n
```

**Implementation approach**: Pure Forth. Markdown is plain text with conventions,
so generation is straightforward string output. Uses a file descriptor pattern
identical to `html-fid` / `h>>`.

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`

---

### 4.3 `glob.fs` - Pattern Matching

**Why it matters**: File selection, string filtering, simple matching without
full regex. Shell glob patterns are familiar to every Unix user.

**Proposed API**:

```forth
require ~/fifth/lib/glob.fs

\ --- Simple glob matching ---
s" hello.fs" s" *.fs" glob-match?       ( str$ pattern$ -- flag ) \ true
s" hello.fs" s" *.go" glob-match?       \ false
s" test-str.fs" s" test-*" glob-match?  \ true

\ --- Starts/ends with ---
s" hello world" s" hello" starts-with?  ( addr u prefix$ -- flag ) \ true
s" hello world" s" world" ends-with?    ( addr u suffix$ -- flag ) \ true
s" hello world" s" ello" contains?      ( addr u sub$ -- flag )    \ true

\ --- File listing (shell out to find/ls) ---
s" ~/fifth/lib" s" *.fs" glob-files     ( dir$ pattern$ -- )
\ Results in /tmp/fifth-glob.txt, iterate like sql-open/sql-row?
glob-open
begin glob-next? while
  type cr   \ print each matching filename
repeat
glob-close
```

**Implementation approach**: `glob-match?` is pure Forth implementing `*` and
`?` wildcards via recursive descent. File listing shells out to
`find dir -name 'pattern' -type f`. Results read via temp file like `sql.fs`.

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`

---

## 5. System Libraries

### 5.1 `fs.fs` - File System Operations

**Why it matters**: Reading files, listing directories, checking existence,
getting file sizes. The foundation for any application that touches the
filesystem beyond HTML output.

**Proposed API**:

```forth
require ~/fifth/lib/fs.fs

\ --- File reading ---
s" /etc/hostname" slurp type            ( file$ -- addr u ) \ Read entire file
s" /tmp/data.txt" 10 head type          ( file$ n -- addr u ) \ First n lines

\ --- File info (shell out to stat) ---
s" ~/fifth/lib/str.fs" file-size        ( file$ -- n )   \ Size in bytes
s" ~/fifth/lib/str.fs" file-mtime       ( file$ -- addr u ) \ Modification time
s" ~/fifth/lib/" dir-count              ( dir$ -- n )    \ Number of entries

\ --- Directory listing ---
s" ~/fifth/lib" dir-list                ( dir$ -- )
\ Results in temp file
dir-open
begin dir-next? while
  type cr
repeat
dir-close

\ --- File operations (shell out) ---
s" /tmp/a.txt" s" /tmp/b.txt" file-copy     ( src$ dst$ -- ) \ cp
s" /tmp/a.txt" s" /tmp/b.txt" file-move     ( src$ dst$ -- ) \ mv
s" /tmp/old.txt" file-delete                ( file$ -- )      \ rm
s" /tmp/newdir" dir-create                  ( dir$ -- )       \ mkdir -p

\ --- Temp files ---
tmp-file type                               ( -- addr u ) \ /tmp/fifth-XXXXX

\ --- Path manipulation ---
s" /home/user/docs/file.txt" basename type  ( path$ -- addr u ) \ file.txt
s" /home/user/docs/file.txt" dirname type   ( path$ -- addr u ) \ /home/user/docs
s" /home/user/docs/file.txt" extname type   ( path$ -- addr u ) \ .txt
```

**Implementation approach**: `slurp-file` already exists in Gforth. File info
shells out to `stat` (GNU or BSD variants detected at load time). Directory
listing uses `ls -1`. Path manipulation is pure Forth string scanning (search
backward for `/` and `.`).

**Estimated size**: ~200 lines

**Dependencies**: `str.fs`

---

### 5.2 `env.fs` - Environment Variables and Process Info

**Why it matters**: Configuration from the environment (12-factor style), detecting
the platform, getting the user name, reading `$HOME`. Every real program needs this.

**Proposed API**:

```forth
require ~/fifth/lib/env.fs

\ --- Environment variables ---
s" HOME" env$ type                  ( name$ -- addr u flag ) \ /Users/josh, true
s" MISSING" env$ type               \ "", false

s" HOME" s" /tmp" env-or type       ( name$ default$ -- addr u )
\ Returns $HOME if set, "/tmp" otherwise

\ --- Platform detection ---
os$ type                            ( -- addr u ) \ "Darwin" or "Linux"
darwin? .                           ( -- flag )   \ true on macOS
linux? .                            ( -- flag )   \ true on Linux

\ --- Process info ---
pid .                               ( -- n ) \ Current process ID
user$ type                          ( -- addr u ) \ Current username
home$ type                          ( -- addr u ) \ Home directory

\ --- Command availability ---
s" curl" has-command? .             ( cmd$ -- flag ) \ true if in PATH
s" jq" has-command? .              \ true or false
```

**Implementation approach**: Gforth provides `getenv` for environment variables.
Platform detection shells out to `uname -s`. `has-command?` shells out to
`command -v name > /dev/null 2>&1`.

**Estimated size**: ~100 lines

**Dependencies**: `str.fs`

---

### 5.3 `proc.fs` - Process Management

**Why it matters**: Running external commands and capturing their output is
Fifth's core philosophy. This library formalizes the pattern used in `sql.fs`
and makes it general-purpose.

**Proposed API**:

```forth
require ~/fifth/lib/proc.fs

\ --- Run and capture stdout ---
s" date +%Y-%m-%d" $>  type        ( cmd$ -- addr u ) \ "2026-01-28"
s" wc -l < /etc/hosts" $>n .       ( cmd$ -- n )      \ 12

\ --- Run and check exit code ---
s" test -f /etc/hosts" $? .        ( cmd$ -- n ) \ 0 = exists
s" test -f /nonexistent" $? .      \ 1 = not found

\ --- Run silently (discard output) ---
s" mkdir -p /tmp/myapp" $!         ( cmd$ -- )   \ Run, ignore output

\ --- Pipe chains ---
pipe-reset
s" cat /etc/hosts" pipe+
s" grep localhost" pipe+
s" wc -l" pipe+
pipe$ $>n .                         \ Build and run pipe chain

\ --- Backtick-style capture to buffer ---
s" hostname" $>str                  ( cmd$ -- )  \ Result in str-buf
str$ type

\ --- Timeout ---
5 s" sleep 10" $>timeout            ( secs cmd$ -- n ) \ Exit code (137 if killed)
```

**Implementation approach**: All commands shell out via `system`. Capture uses
redirection to `/tmp/fifth-proc-out.txt` and reads back. `$>n` reads the file
and converts to number. `pipe-reset`/`pipe+` builds pipe chains in `str-buf`
using ` | ` separators.

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`

---

## 6. Cryptography Library

### 6.1 `hash.fs` - Hashing and Checksums

**Why it matters**: Data integrity, cache keys, content addressing, simple
authentication tokens. Not for password storage -- for checksums and fingerprints.

**Proposed API**:

```forth
require ~/fifth/lib/hash.fs

\ --- SHA-256 (shell out to shasum or openssl) ---
s" hello world" sha256 type         ( addr u -- addr u' )
\ "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9"

\ --- MD5 (for checksums, NOT security) ---
s" hello world" md5 type            ( addr u -- addr u' )
\ "5eb63bbbe01eeed093cb22bb8f5acdc3"

\ --- File hashing ---
s" ~/fifth/lib/str.fs" file-sha256 type  ( file$ -- addr u )
s" ~/fifth/lib/str.fs" file-md5 type     ( file$ -- addr u )

\ --- Simple hash for cache keys ---
s" some string" fnv32 .            ( addr u -- n ) \ FNV-1a 32-bit hash (pure Forth)

\ --- Base64 encoding (shell out to base64) ---
s" hello world" base64-encode type  ( addr u -- addr u' ) \ "aGVsbG8gd29ybGQ="
s" aGVsbG8gd29ybGQ=" base64-decode type ( addr u -- addr u' ) \ "hello world"

\ --- HMAC (for webhook verification, etc.) ---
s" mysecret" s" message" hmac-sha256 type  ( key$ msg$ -- addr u )
```

**Implementation approach**: SHA-256 and MD5 shell out to `shasum -a 256` or
`openssl dgst -sha256`. Input piped via `echo -n 'input' | shasum`. File hashing
uses `shasum file`. Base64 shells to `base64` / `base64 -d`. FNV-1a hash is
pure Forth (simple loop over bytes). HMAC uses `openssl dgst -sha256 -hmac`.

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`, requires `shasum` or `openssl`, `base64`

---

## 7. Developer Tool Libraries

### 7.1 `log.fs` - Structured Logging

**Why it matters**: `."` and `type` are fine for debugging. Production applications
need timestamps, severity levels, and structured output that can be parsed by
other tools.

**Proposed API**:

```forth
require ~/fifth/lib/log.fs

\ --- Log levels ---
LOG-DEBUG   \ 0
LOG-INFO    \ 1
LOG-WARN    \ 2
LOG-ERROR   \ 3

\ --- Configuration ---
LOG-INFO log-level!             ( level -- ) \ Set minimum level
s" /tmp/app.log" log>file       ( file$ -- ) \ Log to file (default: stderr)

\ --- Logging ---
s" Server started on port 8080" log-info
\ 2026-01-28T14:30:00 [INFO] Server started on port 8080

s" Query took 2340ms" log-warn
\ 2026-01-28T14:30:01 [WARN] Query took 2340ms

s" Connection refused" log-error
\ 2026-01-28T14:30:02 [ERROR] Connection refused

s" Variable x = 42" log-debug
\ (suppressed if level > DEBUG)

\ --- Contextual logging ---
s" http" log-context!           ( ctx$ -- ) \ Set context prefix
s" GET /users 200" log-info
\ 2026-01-28T14:30:03 [INFO] [http] GET /users 200

\ --- Timing ---
log-tick                        ( -- ) \ Start timer
\ ... do work ...
s" Query complete" log-tock     ( msg$ -- ) \ Logs with elapsed ms
\ 2026-01-28T14:30:04 [INFO] Query complete (42ms)
```

**Implementation approach**: Timestamps via `date +%Y-%m-%dT%H:%M:%S` shell-out
(cached per second to avoid excessive forking). Level filtering via variable
comparison. Output to stderr by default using Gforth's `stderr` file descriptor.
Timer uses Gforth's `utime` (microsecond clock).

**Estimated size**: ~150 lines

**Dependencies**: `str.fs`

---

### 7.2 `docgen.fs` - Documentation Generator

**Why it matters**: Fifth libraries document words with stack-effect comments.
A tool that extracts these comments and generates browsable documentation
makes the project self-documenting.

**Proposed API**:

```forth
require ~/fifth/lib/docgen.fs

\ --- Extract documentation from a .fs file ---
s" ~/fifth/lib/str.fs" doc-scan    ( file$ -- )
\ Parses: lines starting with "\ ", ": word ( stack -- effect )"

\ --- Generate markdown documentation ---
s" ~/fifth/lib/str.fs" s" /tmp/str-docs.md" doc>md
\ Produces markdown with:
\   # str.fs - String Utilities
\   ## Words
\   ### str-reset
\   `( -- )`
\   Clear primary buffer
\   ...

\ --- Generate HTML documentation ---
s" ~/fifth/lib" s" /tmp/fifth-docs.html" doc>html
\ Scans all .fs files in directory, produces single HTML page

\ --- List all words in a file ---
s" ~/fifth/lib/html.fs" doc-words
\ Prints:
\   html>file      ( fid -- )
\   html>stdout    ( -- )
\   h>>            ( addr u -- )
\   ...
```

**Implementation approach**: Reads `.fs` files line by line. Detects word
definitions (lines starting with `: `) and extracts the word name and stack
effect from the parenthesized comment. Collects preceding `\` comment lines
as documentation. Outputs via `md.fs` or `html.fs`.

**Estimated size**: ~250 lines

**Dependencies**: `str.fs`, `html.fs` (for HTML output), `md.fs` (for markdown output)

---

### 7.3 `prof.fs` - Simple Profiling

**Why it matters**: When something is slow, you need to know where the time goes.
Forth's interactive nature makes profiling natural -- wrap a word, measure it,
report.

**Proposed API**:

```forth
require ~/fifth/lib/prof.fs

\ --- Time a block ---
timer-start
  \ ... do work ...
timer-stop .ms
\ Output: 42ms

\ --- Time a word ---
s" my-word" ' my-word profile
\ my-word: 42ms (1000 calls, 0.042ms/call)

\ --- Benchmark with iterations ---
1000 ' my-word bench
\ 1000 iterations: 42ms total, 0.042ms/avg

\ --- Memory usage (approximate) ---
.mem
\ Dictionary: 12,400 bytes used
\ Data space: 3,200 bytes used

\ --- Compare two approaches ---
1000 ' approach-a ' approach-b bench-compare
\ approach-a: 42ms (1000 iterations)
\ approach-b: 38ms (1000 iterations)
\ approach-b is 1.11x faster
```

**Implementation approach**: Uses Gforth's `utime` for microsecond timing.
`profile` executes the word in a loop and divides total time by iterations.
Memory reporting uses `here` and `unused` (Gforth built-ins). Pure Forth,
no shell-out needed.

**Estimated size**: ~120 lines

**Dependencies**: none (standalone)

---

## 8. Integration Library

### 8.1 `jq.fs` - JSON Query Interface

**Why it matters**: Fifth generates data. `jq` queries data. Together they handle
the full JSON lifecycle. Rather than writing a JSON parser in Forth (hundreds of
lines, fragile), shell out to `jq` -- the best JSON tool ever written.

**Proposed API**:

```forth
require ~/fifth/lib/jq.fs

\ --- Query a JSON file ---
s" /tmp/data.json" s" .name" jq type        ( file$ query$ -- addr u )
\ "Fifth"

s" /tmp/data.json" s" .tags | length" jq-n  ( file$ query$ -- n )
\ 2

\ --- Query JSON string (pipe to jq) ---
s" {\"a\":1}" s" .a" jqs type               ( json$ query$ -- addr u )
\ "1"

\ --- Extract array elements ---
s" /tmp/data.json" s" .items[]" jq-lines    ( file$ query$ -- )
\ Results in temp file, iterate like sql-open
jq-open
begin jq-next? while
  type cr
repeat
jq-close

\ --- Raw mode (no quotes on strings) ---
s" /tmp/data.json" s" .name" jq-raw type    ( file$ query$ -- addr u )
\ Fifth (no quotes)

\ --- Pretty print ---
s" /tmp/data.json" jq-pretty                ( file$ -- ) \ cat file | jq .
```

**Implementation approach**: Every operation shells out to `jq`. Input from
file or piped via `echo`. Output captured to temp file and read back. Uses
the same temp-file-and-read pattern as `sql.fs`.

**Estimated size**: ~120 lines

**Dependencies**: `str.fs`, requires `jq`

---

## Summary

### Proposed Library Inventory

| Library | Lines | Category | Shell Dependencies | Fifth Dependencies |
|---------|-------|----------|-------------------|-------------------|
| `json.fs` | ~150 | Data | none | `str.fs` |
| `csv.fs` | ~180 | Data | `awk` (optional) | `str.fs`, `sql.fs` |
| `ini.fs` | ~200 | Data | none | `str.fs` |
| `http.fs` | ~200 | Network | `curl` | `str.fs` |
| `url.fs` | ~120 | Network | none | `str.fs` |
| `test.fs` | ~180 | Testing | none | `str.fs` |
| `fmt.fs` | ~200 | Text | none | `str.fs` |
| `md.fs` | ~150 | Text | none | `str.fs` |
| `glob.fs` | ~150 | Text | `find` | `str.fs` |
| `fs.fs` | ~200 | System | `stat`, `ls` | `str.fs` |
| `env.fs` | ~100 | System | `uname` | `str.fs` |
| `proc.fs` | ~150 | System | none | `str.fs` |
| `hash.fs` | ~150 | Crypto | `shasum`/`openssl`, `base64` | `str.fs` |
| `log.fs` | ~150 | Dev Tools | `date` | `str.fs` |
| `docgen.fs` | ~250 | Dev Tools | none | `str.fs`, `html.fs`, `md.fs` |
| `prof.fs` | ~120 | Dev Tools | none | none |
| `jq.fs` | ~120 | Integration | `jq` | `str.fs` |
| **Total** | **~2,770** | | | |

### Implementation Priority

**Phase 1 -- Foundation** (unlocks the most capability):
1. `test.fs` -- Cannot build reliably without tests
2. `proc.fs` -- Formalizes the shell-out pattern everything else depends on
3. `env.fs` -- Platform detection and configuration
4. `fmt.fs` -- String formatting for human-readable output

**Phase 2 -- Data** (makes Fifth useful for real workflows):
5. `json.fs` -- The universal data format
6. `csv.fs` -- Data exchange
7. `jq.fs` -- JSON querying
8. `fs.fs` -- File system operations

**Phase 3 -- Network** (connects Fifth to the outside world):
9. `http.fs` -- HTTP client
10. `url.fs` -- URL construction
11. `hash.fs` -- Checksums and encoding

**Phase 4 -- Polish** (developer experience):
12. `log.fs` -- Structured logging
13. `md.fs` -- Markdown generation
14. `ini.fs` -- Configuration files
15. `glob.fs` -- Pattern matching
16. `docgen.fs` -- Self-documentation
17. `prof.fs` -- Performance measurement

### Design Principles Applied

Every proposal follows these rules:

1. **One thing well** -- Each library has a single clear purpose
2. **Shell out, do not reimplement** -- `curl` for HTTP, `jq` for JSON parsing, `shasum` for hashing
3. **Under 300 lines** -- No library exceeds the complexity budget
4. **Pure Forth where possible** -- JSON generation, string formatting, test assertions need no external tools
5. **Same patterns throughout** -- File output via `fid` variables, iteration via temp files, strings via `str-buf`
6. **Composable** -- `http.fs` + `jq.fs` = API client. `csv.fs` + `sql.fs` = data pipeline. `test.fs` + `prof.fs` = benchmarks.

Fifth at ~1,600 lines can generate HTML dashboards from SQLite databases. With
these 17 libraries adding ~2,800 lines, Fifth at ~4,400 lines would be a
complete application toolkit: fetch APIs, parse JSON, run tests, hash data,
manage files, log events, and generate documentation -- all from Forth.

---

*Last updated: 2026-01-28*
