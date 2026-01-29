---
layout: default
title: Gotchas
---

# Gotchas

Things that will break you if you don't know them.

## 1. Whitespace is Everything

Forth tokenizes on whitespace. There is no other syntax.

```forth
\ WRONG - this is ONE undefined word
</div>nl

\ RIGHT - these are TWO words
</div> nl
```

## 2. s" Strings Don't Escape

Standard `s"` treats backslash as literal:

```forth
s" hello\nworld"    \ Contains literal backslash-n
```

Use `s\"` for escapes:

```forth
s\" hello\nworld"   \ Contains actual newline
```

## 3. Strings Are Transient

`s"` strings don't persist after the word returns:

```forth
\ WRONG
: get-greeting s" Hello" ;
get-greeting type    \ May crash or print garbage

\ RIGHT - use the string immediately or copy it
```

## 4. Never Use s+

Dynamic string concatenation crashes:

```forth
\ WRONG - memory errors
s" Hello, " s" World!" s+

\ RIGHT - use buffer pattern
str-reset
s" Hello, " str+
s" World!" str+
str$ type
```

## 5. Buffer Nesting

Don't nest operations on the same buffer:

```forth
\ WRONG - second str-reset clobbers first
str-reset
s" outer " str+
str-reset           \ Oops!
s" inner" str+

\ RIGHT - use different buffers or sequence operations
```

## 6. Stack Imbalance = Cryptic Crashes

"Invalid memory address" usually means stack error:

```forth
\ WRONG - loses 'a'
: bad ( a b c -- ) swap drop ;

\ Debug with .s
: bad ( a b c -- ) .s swap drop .s ;
```

## 7. SQL Single Quotes

Shell commands use single quotes. SQL strings use single quotes. Conflict:

```forth
\ PROBLEMATIC
s" SELECT * FROM users WHERE name='Alice'" sql-exec

\ WORKAROUNDS
\ - Use numeric comparisons
\ - Use double-quoting tricks
\ - Avoid string literals in WHERE clauses
```

## 8. Don't Redefine Core Words

Shadowing words like `emit`, `type`, `@` causes chaos:

```forth
\ WRONG
: type ." DEBUG: " type ;    \ Infinite recursion
```

## 9. Include vs Require

`include` loads every time. `require` loads once:

```forth
\ WRONG - double definitions, crashes
include lib/html.fs
include lib/html.fs

\ RIGHT
require lib/html.fs
require lib/html.fs    \ Silently ignored
```

## 10. Forgetting 2drop for Strings

Strings are two stack items (addr + length):

```forth
\ WRONG - stack grows forever
: process ( addr u -- )
  type ;              \ type consumes both, good

: broken ( addr u -- )
  drop type ;         \ Only drops length, addr remains
```

[Back to Wiki](../)
