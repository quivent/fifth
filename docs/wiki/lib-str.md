---
layout: default
title: str.fs - String Buffers
---

# str.fs — String Buffers

Safe string building without dynamic allocation.

## The Problem

Forth strings are transient — `s" hello"` doesn't persist. And `s+` crashes.

## The Solution

Two static buffers with accumulator pattern:

```forth
require ~/.fifth/lib/pkg.fs
use lib:str.fs
```

## Primary Buffer

```forth
str-reset              \ Clear buffer
s" Hello, " str+       \ Append string
s" World!" str+        \ Append more
str$                   \ Get result ( addr u )
type                   \ Hello, World!
```

## Words

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str-reset` | `( -- )` | Clear primary buffer |
| `str+` | `( addr u -- )` | Append string |
| `str-char` | `( c -- )` | Append character |
| `str$` | `( -- addr u )` | Get buffer contents |

## Secondary Buffer

For when you need two buffers (e.g., escaping inside building):

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `str2-reset` | `( -- )` | Clear secondary buffer |
| `str2+` | `( addr u -- )` | Append to secondary |
| `str2$` | `( -- addr u )` | Get secondary contents |

## Parsing

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `split-at` | `( addr u c -- before-addr before-u after-addr after-u )` | Split at character |
| `sql-field` | `( addr u n -- field-addr field-u )` | Extract pipe-delimited field |
| `str=` | `( addr1 u1 addr2 u2 -- flag )` | Compare strings |

## Example: Building a Command

```forth
: run-query ( db-addr db-u sql-addr sql-u -- )
  str-reset
  s" sqlite3 -separator '|' " str+
  2swap str+                     \ database
  s"  \"" str+
  str+                           \ SQL
  s" \"" str+
  str$ system ;
```

## Rules

1. **Never nest same-buffer operations**
2. **Use str2 for escaping** — html-escape uses secondary buffer
3. **Copy results if you need to keep them** — buffer gets reused

[Back to Wiki](../)
