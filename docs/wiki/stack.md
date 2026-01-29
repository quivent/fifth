---
layout: default
title: Stack Operations
---

# Stack Operations

The stack is the heart of Forth. Everything operates on it.

## Visualizing the Stack

```forth
1 2 3    \ Stack: 1 2 3 ← top
```

The rightmost item is the top (TOS = Top Of Stack).

## Basic Operations

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `dup` | `( a -- a a )` | Duplicate top |
| `drop` | `( a -- )` | Discard top |
| `swap` | `( a b -- b a )` | Exchange top two |
| `over` | `( a b -- a b a )` | Copy second to top |
| `rot` | `( a b c -- b c a )` | Rotate third to top |
| `-rot` | `( a b c -- c a b )` | Rotate top to third |
| `nip` | `( a b -- b )` | Drop second |
| `tuck` | `( a b -- b a b )` | Copy top under second |

## Examples

```forth
1 2 3 dup      \ 1 2 3 3
1 2 3 drop     \ 1 2
1 2 3 swap     \ 1 3 2
1 2 3 over     \ 1 2 3 2
1 2 3 rot      \ 2 3 1
```

## Double-Width Operations

For pairs (like strings: address + length):

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `2dup` | `( a b -- a b a b )` | Duplicate pair |
| `2drop` | `( a b -- )` | Drop pair |
| `2swap` | `( a b c d -- c d a b )` | Exchange pairs |
| `2over` | `( a b c d -- a b c d a b )` | Copy second pair |

## Return Stack

Temporary storage during computation:

| Word | Stack Effect | Description |
|------|-------------|-------------|
| `>r` | `( a -- ) R:( -- a )` | Move to return stack |
| `r>` | `( -- a ) R:( a -- )` | Move from return stack |
| `r@` | `( -- a ) R:( a -- a )` | Copy from return stack |
| `2>r` | `( a b -- ) R:( -- a b )` | Move pair to return |
| `2r>` | `( -- a b ) R:( a b -- )` | Move pair from return |

**Warning**: Always balance `>r` with `r>` within a word.

## Debugging

```forth
.s     \ Print stack without consuming
.      \ Print and consume top
depth  \ Push stack depth
```

[Back to Wiki](../)
