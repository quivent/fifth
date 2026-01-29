---
title: Why Forth for LLMs
parent: Concepts
nav_order: 2
---

# Why Forth for LLMs

Most languages optimize for human intuition. Forth optimizes for explicit reasoning.

## The Problem

| Challenge | Other Languages | Forth |
|-----------|----------------|-------|
| Implicit state | Variables everywhere | One stack |
| Large API | Thousands of methods | 75 words |
| Complex control | Callbacks, async | Linear |
| Hidden effects | Mutations | Stack effects |

## Why It Matters

### LLMs Generate Better Forth

Not because it's easier for humans. Because:

- **Pattern matching** — Consistent, minimal patterns
- **Formal reasoning** — Stack effects compose

```forth
: double ( n -- n*2 ) 2 * ;
: quadruple ( n -- n*4 ) double double ;
\ Verifiable: ( n -- n*2 -- n*4 ) ✓
```

### Explicit State

When the only state is a stack, there's nowhere for hallucinations to hide. Either:

- Stack is correct → working code
- Stack is wrong → immediate crash

### Small Vocabulary

GPT-4 hallucinates method names from millions of APIs.

Fifth has 75 words. An LLM can hold it all in context.

## Stack as Contract

```forth
: word ( inputs -- outputs ) ... ;
```

- Documentation
- Verification  
- Testing

No hidden state. No surprises.
