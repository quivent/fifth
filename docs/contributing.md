# DEVELOPMENT.md - Contributing to Fifth

How to develop, test, debug, and contribute correct Fifth code.

---

## 1. Development Philosophy

### The Fifth Way

Fifth follows three non-negotiable principles:

**Minimal.** Every word must earn its place. If a word exists only to wrap another word with no added value, it does not belong. Chuck Moore built operating systems in 2KB. We are not Chuck Moore, but we remember where we came from.

**Composable.** Small words combine into larger patterns. A word that does two things should be two words. `h1.` exists because `<h1> text </h1>` is a pattern that recurs hundreds of times -- not because we like convenience for its own sake.

**No allocation by default.** Static buffers. Temp files. Stack manipulation. If you reach for `allocate` and `free`, you are writing C with a bad syntax. Fifth uses two 1KB string buffers and file I/O. That handles everything from HTML generation to SQL result parsing.

### Why We Shell Out

Fifth talks to SQLite through the `sqlite3` CLI, not through C bindings. This is deliberate:

- **Zero build complexity.** No `gcc`, no `ld`, no `pkg-config`, no shared libraries. Install Gforth and go.
- **Universal availability.** `sqlite3` ships with macOS. It is one `apt install` away on Linux.
- **Inspectable.** Every query produces a temp file you can `cat`. Try debugging a C binding that returns a null pointer.
- **Good enough.** For dashboards and viewers, the overhead of spawning a process is irrelevant. We are generating HTML, not trading equities.

When to reconsider: if you need transactions, prepared statements, or sub-millisecond query latency, the shell-out pattern breaks down. That is not what Fifth is for today.

---

## 2. Setting Up

### Requirements

| Tool | Version | Install |
|------|---------|---------|
| Gforth | 0.7+ | `brew install gforth` (macOS) / `apt install gforth` (Linux) |
| sqlite3 | 3.x | `brew install sqlite` (macOS) / `apt install sqlite3` (Linux) |
| A browser | Any | For viewing generated HTML |

### Project Structure

```
~/fifth/
  lib/
    str.fs          String buffers, parsing, field extraction (standalone)
    html.fs         HTML5 tags, escaping, file output (requires str.fs)
    sql.fs          SQLite CLI interface, result iteration (requires str.fs)
    template.fs     Slots, conditional rendering (requires html.fs)
    ui.fs           Cards, badges, tabs, grids (requires html.fs, template.fs)
    core.fs         Loads str + html + sql + utilities
  examples/
    db-viewer.fs    Dual-database HTML viewer
    project-dashboard.fs  Tabbed dashboard with panels
```

### Dependency Graph

```
str.fs  <--  html.fs  <--  template.fs  <--  ui.fs
  ^             ^
  |             |
sql.fs      core.fs (loads str + html + sql)
```

`core.fs` does NOT load `ui.fs` or `template.fs`. If you need UI components, require them explicitly.

### Verify Your Setup

```bash
# Test 1: Gforth runs
gforth -e "42 . cr bye"
# Expected: 42

# Test 2: Libraries load without error
gforth -e "require ~/fifth/lib/core.fs .fifth bye"
# Expected: Fifth version banner

# Test 3: String buffer works
gforth -e 'require ~/fifth/lib/str.fs str-reset s" hello" str+ str$ type cr bye'
# Expected: hello

# Test 4: Run an example (requires ~/.claude/db/projects.db)
gforth ~/fifth/examples/project-dashboard.fs
# Expected: opens HTML dashboard in browser
```

If step 4 fails because the database does not exist, that is fine. Steps 1-3 confirm your libraries work.

---

## 3. Writing Fifth Code

### Stack Comments Are Mandatory

Every word definition must have a stack comment. No exceptions. Every. Single. One.

```forth
\ WRONG - will be rejected in review
: my-word dup 2dup + . ;

\ RIGHT
: my-word ( n -- n ) dup 2dup + . ;
```

Conventions for stack comment notation:

| Notation | Meaning |
|----------|---------|
| `n` | Number |
| `addr u` | String (address + length pair) |
| `flag` | Boolean (-1 true, 0 false) |
| `fid` | File descriptor |
| `xt` | Execution token |
| `$` suffix | Shorthand for addr u pair: `text$` means `addr u` |
| `--` | Separator between inputs and outputs |
| `( -- )` | No stack effect (side effects only) |

Real examples from the codebase:

```forth
: str+       ( addr u -- )              \ Append to primary buffer
: html-escape ( addr u -- addr' u' )    \ Escape HTML entities
: sql-field  ( addr u n -- addr u field-addr field-u )  \ Extract field
: badge      ( text$ class$ -- )        \ Render badge span
: ?render    ( flag xt -- )             \ Conditional execution
```

### Buffer Discipline

Fifth has two string buffers. Using the wrong one corrupts data silently.

**Primary buffer** (`str-buf`, 1024 bytes): general-purpose string building.
```forth
str-reset                  \ Clear it
s" hello " str+            \ Append
s" world" str+             \ Append more
str$                       \ Get contents: ( -- addr u )
```

**Secondary buffer** (`str2-buf`, 1024 bytes): reserved for `html-escape`.
```forth
str2-reset                 \ Clear it
s" nested" str2+           \ Append
str2$                      \ Get contents
```

**The rule**: `html-escape` uses the secondary buffer internally. If you are building a string in the primary buffer and call a word that escapes HTML (like `text`, `h1.`, `td.`), the primary buffer is safe. But if you try to build a string in the secondary buffer while something is escaping, you get corruption.

```forth
\ SAFE: primary buffer + text (which uses secondary for escaping)
str-reset
s" <div class='" str+
s" my-class" str+
s" '>" str+
str$ raw
s" user input" text    \ text uses str2-buf internally -- no conflict

\ DANGEROUS: secondary buffer + escaping
str2-reset
s" building " str2+
s" <script>" html-escape str2+   \ html-escape ALSO uses str2-buf. Corruption.
```

**What to do when you need three buffers**: You don't. Restructure. Build one string, use it, then build the next. Sequential, not nested.

### Error Handling

Forth does not have exceptions in the traditional sense. Fifth uses these patterns:

```forth
\ Pattern 1: throw on file errors (standard Gforth)
s" /tmp/output.html" w/o create-file throw html>file

\ Pattern 2: silent truncation on buffer overflow
\ str+ silently drops content that exceeds str-max (1024 bytes)
\ This is by design. If you need more, increase str-max in str.fs.

\ Pattern 3: assertion abort
42 ??    \ Passes (non-zero)
0 ??     \ Prints "ASSERTION FAILED" and aborts

\ Pattern 4: check-and-skip for SQL rows
sql-row? while
  dup 0> if        \ Non-empty row
    \ ... process ...
  else 2drop then  \ Empty row, skip
repeat 2drop       \ End of results
```

Do not invent new error handling mechanisms. Use the patterns above.

### Naming Conventions

| Convention | Examples | When to use |
|------------|----------|-------------|
| `<tag>` / `</tag>` | `<div>` `</div>` `<h1>` `</h1>` | HTML open/close tags |
| `<tag.>` | `<div.>` `<span.>` | Tag with class attribute |
| `<tag#>` | `<div#>` | Tag with id attribute |
| `<tag#.>` | `<div#.>` | Tag with id and class |
| `tag.` (dot suffix) | `h1.` `p.` `td.` `li.` | Convenience word: open + escaped text + close |
| `-begin` / `-end` | `card-begin` `card-end` | Paired wrappers for multi-line blocks |
| `-c` suffix | `card-c` | Component definition (convention from template.fs) |
| `-layout` suffix | `page-layout` | Layout definition |
| `n>str` | `n>str` | Conversion words |
| `str=` | `str=` | Comparison / predicate |
| `?word` | `?render` `?text` | Conditional execution |
| `sql-` prefix | `sql-exec` `sql-open` `sql-row?` | SQL subsystem |
| `.word` (dot prefix) | `.fifth` `.s.` `.sql-field` | Print / display words |

### How to Add a Word to an Existing Library

1. Read the library file entirely. Understand the section structure.
2. Find the correct section (or create one if none fits).
3. Write the word with a stack comment.
4. Test interactively:
   ```bash
   gforth ~/fifth/lib/core.fs
   ```
   Then type your word definition and test at the prompt.
5. Add it to the file in the correct section.
6. Verify the library still loads:
   ```bash
   gforth -e "require ~/fifth/lib/core.fs bye"
   ```

### How to Create a New Library

1. Create `lib/newlib.fs`.
2. Add the file header:
   ```forth
   \ fifth/lib/newlib.fs - Brief description
   \ What this library does

   require ~/fifth/lib/str.fs   \ or whatever you need
   ```
3. Use section separators consistent with other libraries:
   ```forth
   \ ============================================================
   \ Section Name
   \ ============================================================
   ```
4. Do NOT add it to `core.fs` automatically. Libraries are loaded explicitly by the programs that need them. Only add to `core.fs` if it is universally needed.
5. Update README.md with the new library's documentation.

---

## 4. Testing

### Current State

Fifth does not have an automated test suite. Testing is manual:

```bash
# Load and use interactively
gforth ~/fifth/lib/core.fs
\ Then type words at the prompt and verify output
```

This is the honest state of things. The project is ~1,500 lines across 6 library files.

### How to Test a Word

Interactive testing at the Gforth prompt:

```bash
gforth ~/fifth/lib/str.fs
```

```forth
\ Test str+ and str$
str-reset s" hello " str+ s" world" str+ str$ type cr
\ Expected: hello world

\ Test parse-pipe
s" apple|banana|cherry" 1 parse-pipe type cr
\ Expected: banana

\ Test edge cases
str-reset str$ type cr
\ Expected: (empty line)

s" a|b|c" 5 parse-pipe type cr
\ Expected: (empty -- field index out of range)
```

### Proposed Test File Structure

If you want to add tests, follow this pattern:

```forth
\ fifth/tests/test-str.fs - String library tests
require ~/fifth/lib/str.fs

variable test-count
variable fail-count

: test: ( "name" -- )
  \ Print test name
  ." TEST: " parse-name type ."  ... " ;

: pass ( -- ) ." PASS" cr 1 test-count +! ;
: fail ( -- ) ." FAIL" cr 1 test-count +! 1 fail-count +! ;

: expect= ( addr1 u1 addr2 u2 -- )
  str= if pass else fail then ;

: expect-n= ( n1 n2 -- )
  = if pass else fail then ;

\ --- Tests ---

test: str-reset-clears-buffer
  str-reset str$ nip 0 expect-n=

test: str-append-works
  str-reset s" hello" str+ str$ s" hello" expect=

test: str-concat-works
  str-reset s" foo" str+ s" bar" str+ str$ s" foobar" expect=

test: parse-pipe-field-0
  s" a|b|c" 0 parse-pipe s" a" expect= 2drop

test: parse-pipe-field-1
  s" a|b|c" 1 parse-pipe s" b" expect= 2drop

\ --- Summary ---
cr ." Results: "
test-count @ . ." tests, "
fail-count @ . ." failures" cr
fail-count @ 0> if 1 (bye) then
bye
```

Run with:
```bash
gforth ~/fifth/tests/test-str.fs
```

The `??` word in core.fs also works as a quick assertion:

```forth
require ~/fifth/lib/core.fs
s" hello" s" hello" str= ??     \ Passes
s" hello" s" world" str= 0= ??  \ Passes (not equal is true)
```

---

## 5. Debugging

### Common Errors and What They Mean

| Error | Likely Cause |
|-------|-------------|
| `Invalid memory address` | Stack imbalance. You consumed or left an extra item. |
| `Undefined word` | Typo, or word spacing issue. `</div>nl` is not `</div> nl`. |
| `Stack underflow` | You tried to consume a value that is not on the stack. |
| `File not found` | Path to database or output file is wrong. Check with `s" path" file-exists? .` |
| `word is redefined` (warning) | You `include`d a file twice. Use `require` instead. |
| No output / empty HTML file | `html>file` was not called, or you wrote to stdout instead. |

### Stack Debugging

The most important debugging tool is `.s` -- it shows the stack without consuming it.

```forth
\ Add .s calls to see what is on the stack at each point
: my-word ( addr u n -- )
  .s    \ See what we got
  2>r .s  \ After saving string pair
  2r> .s  \ After restoring
  ;
```

The `.s.` word in core.fs is a prettier wrapper:

```forth
.s.    \ prints "Stack: " followed by stack contents
```

### Isolating Buffer Corruption

Symptoms: garbled HTML output, wrong text in wrong places, truncated strings.

**Step 1**: Print buffer contents at key points.
```forth
str-reset
s" building..." str+
str$ type cr        \ What does the buffer actually contain?
```

**Step 2**: Check if you are accidentally using the same buffer in nested calls.
```forth
\ This is the classic bug:
str-reset
s" class='" str+
s" my-class" str+       \ So far so good, str-buf = "class='my-class"
some-word-that-calls-str-reset   \ BOOM. str-buf is now empty.
s" '" str+                       \ str-buf = "'" -- not what you wanted
```

**Step 3**: Trace which buffer each word uses. The `badge` word in ui.fs, for example, uses `str-reset` and `str+` internally to build the CSS class string. If you call `badge` while building your own string in the primary buffer, your string is destroyed.

**Fix**: Always finish one buffer operation before starting the next. Do not interleave.

### The Word Spacing Trap

Forth tokenizes on whitespace. Nothing else. These are different:

```forth
</div>nl       \ ONE word named "</div>nl" -- it exists in html.fs
</div> nl      \ TWO words: "</div>" then "nl"
</div>  nl     \ TWO words: same as above (extra space does not matter)
```

When you see "Undefined word" and the word looks correct, check for missing spaces. This is especially treacherous with tag words:

```forth
\ WRONG - tries to find word "<div.>nl"
s" container" <div.>nl

\ RIGHT
s" container" <div.> nl
```

Some closing tags DO include `nl` as a single word (like `</div>nl`). These are defined in html.fs. Check the library to see which combined forms exist.

---

## 6. Contributing

### What Makes a Good Fifth Contribution

A good contribution:

1. **Solves a real problem.** Not a hypothetical one. Show the use case.
2. **Is small.** One word. One pattern. One library addition. Not a rewrite.
3. **Has a stack comment.** No stack comment, no review.
4. **Uses existing buffers.** Does not add a third string buffer.
5. **Follows the naming conventions.** Tag words look like tags. Predicates end with `?`. Display words start with `.`.
6. **Was tested manually.** Show the Gforth session where you tested it.

### What Will Get Rejected

- **Bloat.** A word that duplicates existing functionality with a slightly different interface.
- **Unnecessary abstraction.** A word that just calls another word with one hardcoded argument, unless the pattern recurs constantly (see `badge-danger` -- it recurs in every dashboard).
- **Missing stack comments.** Non-negotiable.
- **Dynamic allocation.** `allocate`, `free`, `resize` in library code.
- **C bindings.** We shell out. If you need a C binding, you need a different project.
- **Changes to buffer sizes without justification.** If 1024 bytes is not enough, show the real program that needs more.

### Code Review Criteria

When reviewing Fifth code, check:

1. **Stack balance.** Does the word leave the stack in the documented state? Trace it manually.
2. **Buffer safety.** Does the word use `str-reset`? If so, does any caller depend on the buffer contents being preserved?
3. **HTML escaping.** User-visible text must go through `text`, not `raw`. If raw is used, there must be a comment explaining why it is safe.
4. **Section placement.** Is the word in the right section of the library file?
5. **Word naming.** Does it follow the conventions?

### Commit Message Format

```
lib: add csv-field to str.fs for comma parsing

Stack: ( addr u n -- addr u field-addr field-u )
Parses nth field from comma-delimited string.
Used in the new CSV import example.
```

Format: `<scope>: <what you did>`

Scopes: `lib`, `examples`, `docs`, `tests`, `build`

Include the stack comment in the commit body if you added or changed a word.

---

## 7. Performance

### How to Benchmark

**Wall clock time** (the only metric that matters for most Fifth programs):

```bash
time gforth ~/fifth/examples/project-dashboard.fs
```

Typical output on modern hardware:
```
real    0m0.05s    # Total elapsed
user    0m0.03s    # CPU time in Gforth
sys     0m0.02s    # CPU time in kernel (file I/O, sqlite3 spawning)
```

**Per-query timing** (for SQL-heavy programs):

```bash
# Time the sqlite3 command directly
time sqlite3 -separator '|' ~/.claude/db/projects.db 'SELECT * FROM projects'
```

If the query itself takes 50ms but your program takes 500ms, the overhead is in Gforth startup, file I/O, or HTML generation -- not SQL.

**Counting shell-outs**:

```bash
# Wrap system calls to count them
# In sql.fs, sql-exec calls system once per query
# Count sql-exec calls in your .fs file:
grep -c "sql-exec\|sql-count\|sql-dump\|sql-each" ~/fifth/examples/project-dashboard.fs
```

Each `sql-exec` or `sql-count` call spawns a `sqlite3` process. Ten queries means ten process spawns.

### Known Bottlenecks

| Bottleneck | Impact | Mitigation |
|-----------|--------|------------|
| Process spawn per SQL query | ~5-10ms per query | Batch queries with UNION ALL when possible |
| Temp file I/O | ~1ms per read/write | Already mitigated by using /tmp (usually tmpfs) |
| String buffer size (1024) | Limits single-string size | Build strings in chunks, flush to file |
| HTML escaping | Character-by-character loop | Only escape user data; use `raw` for known-safe HTML |

### When Shell-Out Is Acceptable

Shell-out (spawning `sqlite3`) is acceptable when:
- You are generating static HTML output (the dominant use case)
- Query count is under ~50 per program run
- Total runtime under 1 second is fine

Shell-out becomes a problem when:
- You need hundreds of queries in a loop
- You need transactions or rollback
- You need sub-millisecond latency per query
- You need prepared statements for safety

For the "becomes a problem" cases, Fifth is the wrong tool. Use a language with native SQLite bindings.

---

## 8. Architecture Decisions

### Why Static Buffers Over Dynamic Allocation

Gforth's `allocate` and `free` work. The dynamic string concatenation operator `s+` does not -- it causes memory errors in practice on macOS. Rather than debug a memory allocator, we use two fixed 1024-byte buffers.

This forces a discipline: build a string, use it, then build the next. You cannot hoard strings. This is a feature. Stack-based programming means values flow through; they do not accumulate.

The buffers are adequate because:
- Most HTML tags are under 100 bytes
- CSS rules are under 200 bytes
- SQL commands are under 500 bytes
- If you need more than 1024 bytes in a single string, you are building the string wrong -- write it to the file in chunks

### Why File-Based Output Over stdout

HTML generation goes to a file (`html>file`), not stdout. Reasons:

1. **Open in browser.** You cannot pipe stdout to Safari. A file path works everywhere.
2. **Inspectable.** After generation, the file sits in /tmp for you to examine.
3. **Composable.** Multiple Gforth programs can write to different files. Try composing stdout-only programs.
4. **Matches the SQL pattern.** SQL results go to temp files too. Consistency.

### Why Pipe-Delimited SQL Results

`sqlite3 -separator '|'` produces pipe-delimited output. Not CSV. Not tab. Pipe.

- Pipes rarely appear in real data (names, descriptions, paths)
- Commas appear in text content constantly
- Tabs are invisible and hard to debug
- The `parse-pipe` word is simple and fast
- If your data contains literal pipes, you have a problem. Avoid storing pipe characters in database fields that Fifth will query.

### Why No Package Manager

Fifth is six files in a directory. You `require ~/fifth/lib/core.fs` and you have everything. There is no package resolution, no version conflicts, no lockfiles, no registry, no build step.

If you need a word that does not exist, you write it in 3-10 lines and put it in the appropriate library file. If you need a library that does not exist, you create a .fs file in `lib/`. The dependency is a filesystem path.

This is not scalable to 10,000 packages. It is perfect for a focused project with a small vocabulary. Fifth is that project.

---

## Quick Reference

### Running Things

```bash
gforth ~/fifth/lib/core.fs               # Interactive REPL with all core libs
gforth ~/fifth/examples/db-viewer.fs      # Run example program
gforth -e "require ~/fifth/lib/str.fs"    # Load library, stay in REPL
gforth -e "include ~/fifth/examples/project-dashboard.fs"  # Run with stack trace
```

### Files You Must Read Before Modifying

| If you change... | Read first... |
|-------------------|---------------|
| str.fs | html.fs, sql.fs (both depend on str.fs and its buffers) |
| html.fs | ui.fs, template.fs, all examples |
| sql.fs | All examples that use databases |
| core.fs | All examples (they all require core.fs) |
| Any library | CLAUDE.md (project constraints) |

### The Five Rules

1. Stack comment on every word.
2. No dynamic allocation.
3. `text` for user data, `raw` for trusted HTML.
4. `require` not `include`.
5. Test it in the REPL before committing.

---

*Last updated: 2026-01-28*
