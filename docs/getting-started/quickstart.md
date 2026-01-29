---
title: Quick Start
parent: Getting Started
nav_order: 2
---

# Quick Start

## Hello World

```bash
fifth -e ': hello ." Hello, World!" cr ; hello'
```

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

Create `hello.fs`. Notice the stack comments `( -- )` and `( n -- )` — these are contracts. A human reads them as documentation; an LLM can verify that `countdown` consumes one number and produces nothing. This mechanical checkability is what makes Fifth reliable for code generation.

```forth
\ hello.fs - My first Fifth program

: greet ( -- )
  ." Welcome to Fifth!" cr ;

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
