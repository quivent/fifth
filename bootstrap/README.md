# Fifth Metacompilation

*Forth that generates itself.*

## The Idea

Instead of a C interpreter, Fifth bootstraps from a tiny **seed** — just enough
Forth to run Forth. Then `meta.fs` builds the full interpreter.

```
seed (2KB) + meta.fs (Forth) → fifth (57KB)
```

The seed is the only non-Forth code. Everything else is Forth all the way down.

## Seed Primitives (~20 words)

The seed needs exactly these primitives to bootstrap:

| Word | Stack | Description |
|------|-------|-------------|
| `@` | `( addr -- x )` | Fetch cell |
| `!` | `( x addr -- )` | Store cell |
| `c@` | `( addr -- c )` | Fetch byte |
| `c!` | `( c addr -- )` | Store byte |
| `+` | `( a b -- a+b )` | Add |
| `-` | `( a b -- a-b )` | Subtract |
| `*` | `( a b -- a*b )` | Multiply |
| `/` | `( a b -- a/b )` | Divide |
| `and` | `( a b -- a&b )` | Bitwise AND |
| `or` | `( a b -- a\|b )` | Bitwise OR |
| `xor` | `( a b -- a^b )` | Bitwise XOR |
| `<` | `( a b -- flag )` | Less than |
| `emit` | `( c -- )` | Output character |
| `key` | `( -- c )` | Input character |
| `syscall` | `( ... n -- result )` | OS system call |
| `branch` | `( -- )` | Unconditional jump |
| `0branch` | `( flag -- )` | Jump if zero |
| `exit` | `( -- )` | Return from word |
| `lit` | `( -- x )` | Push literal |
| `execute` | `( xt -- )` | Execute word |

That's it. ~20 primitives. Everything else is built in Forth.

## Bootstrap Process

```bash
# One-time: compile the seed (only non-Forth step)
cc -o seed bootstrap/seed.c   # ~200 lines of C

# Bootstrap Fifth from Forth
./seed bootstrap/meta.fs      # Generates: fifth

# Fifth can now regenerate itself
./fifth bootstrap/meta.fs     # Generates: fifth (identical)

# Install
./fifth install.fs
```

After initial bootstrap, the seed is never needed again. Fifth regenerates itself.

## File Structure

```
bootstrap/
├── README.md        This file
├── seed.c           Minimal C seed (~200 lines)
├── meta.fs          Metacompiler (generates fifth binary)
├── kernel.fs        Core words built from primitives
├── compiler.fs      The Forth compiler in Forth
└── image.fs         Binary image generator
```

## Why This Matters

1. **Forth all the way down** — The language implements itself
2. **Minimal trusted base** — Only ~200 lines of C to audit
3. **True portability** — Port the seed, get Fifth free
4. **Educational** — See exactly how Forth works
5. **Philosophical** — A language that can explain itself

## The Metacompiler

The heart is `meta.fs` — a Forth program that generates native executables.

```forth
\ meta.fs - Fifth metacompiler (sketch)

\ Target memory image
65536 constant image-size
create image image-size allot
variable ip  \ Image pointer

\ Emit bytes to image
: b, ( byte -- ) ip @ image + c!  1 ip +! ;
: w, ( word -- ) dup b, 8 rshift b, ;
: l, ( long -- ) dup w, 16 rshift w, ;
: q, ( quad -- ) dup l, 32 rshift l, ;

\ x86-64 code generation
: ret, ( -- ) 0xC3 b, ;
: push-rax, ( -- ) 0x50 b, ;
: pop-rax, ( -- ) 0x58 b, ;
: mov-rax-imm, ( n -- ) 0x48 b, 0xB8 b, q, ;

\ Build a primitive
: primitive ( "name" -- )
  create here ,
  does> @ image + ;

\ Generate the full interpreter...
\ (500+ lines of Forth generating machine code)
```

## Status

This is the vision. Current Fifth uses a C interpreter for pragmatic reasons.
Metacompilation is the path to true self-hosting.

---

*"A language that can't describe itself is incomplete."*
