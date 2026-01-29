---
layout: default
title: Why Forth for LLMs
---

# Why Forth for LLMs

Most programming languages were designed for humans. They optimize for expressiveness, flexibility, and familiar syntax. But when AI generates code, these "features" become liabilities.

## The Problem with Traditional Languages

| Challenge | Traditional Languages | Forth |
|-----------|----------------------|-------|
| **Implicit state** | Variables scattered across scopes, closures capturing context | One explicit stack. All state visible. |
| **Large API surface** | Thousands of methods, multiple ways to do everything | ~75 core words. One way to do each thing. |
| **Complex control flow** | Callbacks, promises, async/await, exceptions | Linear execution. Explicit branches. |
| **Hidden side effects** | Methods that mutate, getters that compute | Stack effects documented on every word. |
| **Verification difficulty** | Types help but don't prevent logic errors | Stack effect composition is mechanically checkable. |

## Why This Matters

### LLMs Generate Better Forth Than Python

Not because Forth is easier — it isn't, for humans. But LLMs don't have the intuitions that make Python feel natural. What they have is:

- **Pattern matching** — Forth has consistent, minimal patterns
- **Formal reasoning** — Stack effects compose predictably

```forth
: double ( n -- n*2 ) 2 * ;
: quadruple ( n -- n*4 ) double double ;

\ An LLM can verify: ( n -- n*2 -- n*4 ) ✓
```

An LLM can verify this composition. It cannot verify that a Python function with three parameters, two optional keyword arguments, and a context manager doesn't have subtle bugs.

### Explicit State Eliminates Hallucination Vectors

When the only state is a stack of integers, there's nowhere for imagined variables or phantom objects to hide. The LLM either:

- Tracks the stack correctly → working code
- Tracks it incorrectly → immediate crash

Not code that works sometimes and corrupts data later.

### Small Vocabulary = Fewer Combinations

GPT-4 has seen millions of Python programs with millions of API combinations. It still hallucinates method names.

Fifth has 75 words. An LLM can hold the entire language in context and generate valid code reliably.

## Patterns for AI Code Generation

### Always Document Stack Effects

```forth
: process-user ( addr u -- flag )
  \ addr u = username string
  \ flag = true if valid
  ... ;
```

### Use Explicit State

```forth
variable user-count

: add-user ( addr u -- )
  validate-user if
    1 user-count +!
  then ;
```

### Compose Small Words

```forth
: valid-email? ( addr u -- flag )
  2dup has-at? -rot has-dot? and ;

: validate-user ( addr u -- flag )
  2dup empty? not -rot
  valid-email? and ;
```

### Fail Loudly

```forth
: require-user ( addr u -- )
  validate-user 0= if
    ." Invalid user" cr abort
  then ;
```

## The Stack as Contract

Every word declares its contract:

```forth
: word ( inputs -- outputs ) ... ;
```

This is:
- **Documentation** — humans know what to expect
- **Verification** — LLMs can check composition
- **Testing** — effects are observable

No hidden state. No side effects. No surprises.

[Back to Wiki](../)
