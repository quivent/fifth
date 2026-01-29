---
title: Stack Operations
parent: Language
nav_order: 1
---

# Stack Operations

The stack is the heart of Forth.

## Visualizing

```forth
1 2 3    \ Stack: 1 2 3 ← top
```

## Basic Operations

| Word | Effect | Description |
|------|--------|-------------|
| `dup` | `( a -- a a )` | Duplicate top |
| `drop` | `( a -- )` | Discard top |
| `swap` | `( a b -- b a )` | Exchange top two |
| `over` | `( a b -- a b a )` | Copy second to top |
| `rot` | `( a b c -- b c a )` | Rotate third to top |

## Double-Width

For string pairs (address + length):

| Word | Effect | Description |
|------|--------|-------------|
| `2dup` | `( a b -- a b a b )` | Duplicate pair |
| `2drop` | `( a b -- )` | Drop pair |
| `2swap` | `( a b c d -- c d a b )` | Exchange pairs |

## Return Stack

| Word | Effect | Description |
|------|--------|-------------|
| `>r` | `( a -- ) R:( -- a )` | Push to return stack |
| `r>` | `( -- a ) R:( a -- )` | Pop from return stack |
| `r@` | `( -- a )` | Copy from return stack |

**Warning**: Always balance `>r` with `r>` within a word.
