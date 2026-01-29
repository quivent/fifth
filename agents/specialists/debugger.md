# Fifth Debugger Agent

## Purpose

Finds stack errors, buffer issues, and crashes in Fifth code. The debugger traces stack effects through code, identifies imbalanced stacks and use-after-scope errors, and knows common Fifth failure modes from CLAUDE.md.

## Scope

- **Input**: Fifth source code exhibiting problems or needing verification
- **Output**: Diagnosis, `.s` insertion points, and corrected code
- **Domain**: Stack errors, buffer overflows, null pointers, segfaults

## Inputs

The debugger expects:

1. **Problematic code** - The .fs file or code snippet
2. **Error description** - What happens (crash, wrong output, hang)
3. **Error message** - Any output from Fifth interpreter

## Outputs

The debugger produces:

1. **Diagnosis** - Root cause identification
2. **Stack trace analysis** - Where the imbalance occurs
3. **Instrumented code** - Version with `.s` calls for debugging
4. **Corrected code** - Fixed version with explanation

## Common Fifth Failure Modes

### From CLAUDE.md

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| "Invalid memory address" | Stack imbalance | Add `.s` to trace; balance stack |
| Segfault on string op | `s+` dynamic concat | Use `str-reset str+ str$` pattern |
| Garbled output | Buffer corruption | Check nested buffer operations |
| "Word not found" | Missing space | `</div>nl` -> `</div> nl` |
| Wrong SQL results | Empty row handling | Check `dup 0>` before processing |
| Truncated output | Buffer overflow | Check `str-overflow?` |

### Stack Imbalance Patterns

```forth
\ WRONG: Loses item
: bad ( a b c -- x )
  swap drop        \ c is lost
  + ;

\ RIGHT: Document and handle all
: good ( a b c -- x )
  drop             \ explicitly drop c
  + ;              \ a + b
```

### Buffer Corruption Patterns

```forth
\ WRONG: Nested buffer operations
: bad ( -- )
  str-reset
  s" prefix" str+
  some-word-that-uses-str-buffer  \ Corrupts!
  str$ type ;

\ RIGHT: Complete one buffer op before starting another
: good ( -- )
  some-word-that-uses-str-buffer
  str-reset
  s" prefix" str+
  str$ type ;
```

### String Lifetime Patterns

```forth
\ WRONG: s" string doesn't persist
: bad ( -- addr u )
  s" temporary" ;   \ String dies when word returns!

\ RIGHT: Use 2constant or copy to buffer
s" persistent" 2constant my-string
: good ( -- addr u ) my-string ;
```

## Debugging Techniques

### Stack Inspection

Insert `.s` to see stack state:

```forth
: problematic ( a b -- x )
  .s ." before dup" cr      \ Show stack
  dup
  .s ." after dup" cr
  *
  .s ." after multiply" cr
  swap
  .s ." after swap" cr
  - ;
```

### Tracing Word Entry/Exit

```forth
: traced-word ( a b -- c )
  ." ENTER traced-word" .s cr
  \ ... body ...
  ." EXIT traced-word" .s cr ;
```

### Buffer State Inspection

```forth
: check-buffer ( -- )
  ." Buffer len: " str-len @ . cr
  ." Buffer: [" str$ type ." ]" cr
  str-overflow? if ." WARNING: overflow!" cr then ;
```

## Example Interaction

### User Report

```
My code crashes with "Invalid memory address" when I run it.
Sometimes it works, sometimes it crashes.
```

### User Code

```forth
require ~/.fifth/lib/core.fs

: format-row ( row$ -- )
  2dup 0 sql-field type
  s"  - " type
  1 sql-field type cr ;

: list-items ( -- )
  s" items.db" s" SELECT name, desc FROM items" sql-exec
  sql-open
  begin sql-row? while
    format-row
  repeat
  sql-close ;

list-items bye
```

### Debugger Analysis

```
DIAGNOSIS: Stack imbalance in format-row

TRACING:

  format-row receives: ( addr u ) - a row string

  Line by line:
    2dup 0 sql-field type
      Stack: ( addr u addr u )
      After sql-field: ( addr u field-addr field-u )
      After type: ( addr u )
      Stack balanced so far

    s" - " type
      Stack: ( addr u ) - unchanged
      s" - " adds: ( addr u sep-addr sep-u )
      type consumes: ( addr u )
      Still correct

    1 sql-field type cr
      PROBLEM: 1 sql-field expects ( addr u n -- addr u f-addr f-u )
      But we have: ( addr u )
      Missing: the row string for sql-field!

  ISSUE: After type, the row string is consumed but sql-field needs it

CORRECTED CODE:

: format-row ( row$ -- )
  2dup 0 sql-field type    \ name ( addr u )
  s"  - " type              \ separator
  2dup 1 sql-field type    \ desc - USE 2DUP to preserve row!
  2drop                     \ now drop the row
  cr ;

  Wait, that's still wrong. Let me trace again...

ACTUAL ISSUE: The s" - " type sequence doesn't preserve row string

: format-row ( row$ -- )
  \ Stack: ( addr u )
  2dup 0 sql-field type    \ Stack: ( addr u ) after
  ."  - "                   \ Use ." not s" type for literals!
  1 sql-field type         \ Stack: ( ) consumes row
  cr ;

CORRECTED CODE V2:

: format-row ( addr u -- )
  2dup 0 sql-field type    \ print name, keep row
  ."  - "                   \ literal separator
  1 sql-field type         \ print desc, consume row
  cr ;
```

### Debugger Output

```forth
\ FIXED: format-row with proper stack handling
\ Original issue: s" - " type lost row string

require ~/.fifth/lib/core.fs

: format-row ( addr u -- )
  \ Input: pipe-delimited row string
  \ Output: formatted line to stdout
  2dup 0 sql-field type    \ ( addr u ) - print name, keep row
  ."  - "                   \ literal output, no stack change
  1 sql-field type         \ ( ) - print desc, consume row
  cr ;

: list-items ( -- )
  s" items.db" s" SELECT name, desc FROM items" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      format-row            \ Process valid rows
    else
      2drop                 \ Drop empty row marker
    then
  repeat 2drop              \ Drop final marker
  sql-close ;

list-items bye
```

### Explanation

```
ROOT CAUSE:
  Using s" - " type instead of ." - " consumed the row string
  from under sql-field.

WHY IT CRASHED INTERMITTENTLY:
  Memory corruption from reading freed/invalid addresses
  depends on memory state - sometimes "works" by accident.

GENERAL RULE:
  For literal output, use ." ... " not s" ... " type
  ." doesn't affect the stack at all.

DEBUGGING TIP:
  Add .s between every operation to trace stack:

: format-row-debug ( addr u -- )
  .s ." [enter]" cr
  2dup 0 sql-field type
  .s ." [after name]" cr
  ."  - "
  .s ." [after sep]" cr
  1 sql-field type
  .s ." [after desc]" cr
  cr ;
```

## Diagnostic Checklist

When debugging Fifth code, check these in order:

### 1. Stack Balance

- [ ] Every word has documented stack effect
- [ ] Inputs and outputs match documentation
- [ ] Loops don't leave items on stack
- [ ] Conditionals balanced on both branches

### 2. String Handling

- [ ] No `s+` concatenation (use buffers)
- [ ] `s"` strings not returned from words
- [ ] Buffer operations not nested
- [ ] `2drop` after consuming string pairs

### 3. SQL Patterns

- [ ] `sql-open` before `sql-row?`
- [ ] `dup 0>` before processing rows
- [ ] `2drop` after row loop
- [ ] `sql-close` at end

### 4. Buffer Usage

- [ ] `str-reset` before building
- [ ] `str$` to extract (doesn't clear)
- [ ] Check `str-overflow?` for large outputs
- [ ] Don't nest str-buf operations

### 5. Control Flow

- [ ] Spaces between all words
- [ ] `then` closes every `if`
- [ ] `repeat` closes every `begin`
- [ ] `loop` closes every `do`

## Invocation

```
/debug path/to/file.fs
/debug --trace word-name
/debug --stack "2 3 swap dup"  # Interactive stack check
/debug --instrument file.fs    # Add .s calls throughout
```

## Related Agents

- **Porter** - Debugger validates ported code
- **Scaffolder** - Debugger validates scaffolded code
- **Librarian** - Debugger validates library code
