---
layout: default
title: Quick Start
---

# Quick Start

## Hello World

```bash
fifth -e ': hello ." Hello, World!" cr ; hello'
```

What this does:
- `: hello` — Start defining a word named "hello"
- `." Hello, World!"` — Print the string
- `cr` — Print newline
- `;` — End definition
- `hello` — Execute the word

## Interactive REPL

```bash
fifth
```

```forth
2 3 + .          \ 5
: square dup * ;
5 square .       \ 25
bye
```

## Run a File

```bash
fifth examples/project-dashboard.fs
```

## Your First Program

Create `hello.fs`:

```forth
\ hello.fs - My first Fifth program

: greet ( -- )
  ." Welcome to Fifth!" cr
  ." The stack has " depth . ." items." cr ;

: countdown ( n -- )
  begin
    dup .
    1-
    dup 0=
  until drop
  ." Liftoff!" cr ;

greet
5 countdown
bye
```

Run it:

```bash
fifth hello.fs
```

Output:

```
Welcome to Fifth!
The stack has 0 items.
5 4 3 2 1 Liftoff!
```

## Understanding Stack Comments

Every word should have a stack effect comment:

```forth
: double ( n -- n*2 ) 2 * ;
```

- `( n -- n*2 )` means: takes one number, returns one number (doubled)
- The `--` separates inputs from outputs

Common patterns:

| Comment | Meaning |
|---------|---------|
| `( -- )` | No inputs, no outputs |
| `( n -- )` | Consumes one number |
| `( -- n )` | Produces one number |
| `( a b -- a+b )` | Takes two, returns one |
| `( addr u -- )` | Takes a string (address + length) |

## Next Steps

- [Stack Operations](stack) — The fundamentals
- [Words](words) — Defining and composing
- [Examples](examples) — Real programs

[Back to Wiki](../)
