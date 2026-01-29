---
title: str.fs
parent: Libraries
nav_order: 1
---

# str.fs — String Buffers

Safe string building without dynamic allocation.

## Usage

```forth
use lib:str.fs

str-reset
s" Hello, " str+
s" World!" str+
str$ type    \ Hello, World!
```

## Words

| Word | Effect | Description |
|------|--------|-------------|
| `str-reset` | `( -- )` | Clear buffer |
| `str+` | `( addr u -- )` | Append string |
| `str-char` | `( c -- )` | Append character |
| `str$` | `( -- addr u )` | Get contents |

## Secondary Buffer

For nested operations:

| Word | Description |
|------|-------------|
| `str2-reset` | Clear secondary |
| `str2+` | Append to secondary |
| `str2$` | Get secondary contents |

**Rule**: Never nest operations on the same buffer.
