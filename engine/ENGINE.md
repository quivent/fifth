# Fifth Engine

A clean-room, minimal Forth implementation in C11 and Forth. No external dependencies beyond a C compiler and POSIX. MIT licensed.

## Quick Start

```bash
cd engine
make          # Build
make test     # Run smoke tests
./fifth       # Interactive REPL
./fifth -e "2 3 + ."   # One-liner
./fifth file.fs         # Execute file
```

## Stats

| Metric | Value |
|--------|-------|
| Total source | 2,270 lines |
| C code | 2,120 lines (4 files + header) |
| Forth bootstrap | 92 lines |
| Binary size | ~57 KB |
| Primitives | ~164 registered words |
| Dependencies | None (C11 + POSIX) |

## Architecture

### Files

```
engine/
  fifth.h        209 lines  Core types, VM struct, inline helpers
  vm.c           346 lines  VM lifecycle, interpreter, dictionary
  prims.c       1026 lines  Stack, arithmetic, memory, compiler, strings
  io.c           434 lines  File I/O, system, include/require, comments
  main.c         105 lines  Entry point and CLI
  boot/
    core.fs       92 lines  Forth bootstrap (defining words, utilities)
  Makefile        58 lines  Build system with smoke tests
```

### Memory Model

Flat byte array. All Forth addresses are byte offsets into `vm->mem[]` (1 MB).

```c
uint8_t mem[MEM_SIZE];   // 1 MB data space
cell_t  here;            // Next free byte offset
```

- `cell_t` = `intptr_t` (native pointer width: 32 or 64 bit)
- All compiled code, strings, and user data live in `mem[]`
- Variables store their data address (byte offset into `mem[]`)
- `HERE` advances as data is compiled

### Dictionary

C struct array, not flat memory. Each entry:

```c
typedef struct {
    int      link;                    // Previous entry index (-1 = end)
    uint8_t  flags;                   // F_IMMEDIATE | F_HIDDEN | name length
    char     name[NAME_MAX_LEN + 1];  // 31 chars max
    prim_fn  code;                    // Handler function pointer
    cell_t   param;                   // Body (byte offset or constant value)
    cell_t   does;                    // DOES> IP, -1 if unused
} dict_entry_t;
```

Up to 8,192 entries. Lookup is linear from `latest` following `link` chain (case-insensitive).

### Threading Model

Indirect threaded code via C function pointers. Each dictionary entry has a `code` field pointing to one of four word handlers:

| Handler | Purpose | Behavior |
|---------|---------|----------|
| `docol` | Colon definitions | Push IP to return stack, set IP to param |
| `dovar` | Variables | Push param (data address) to data stack |
| `docon` | Constants | Push param (value) to data stack |
| `dodoes` | DOES> words | Push param, then enter DOES> code |

Primitives have their own C function as the `code` handler — no indirection.

### Inner Interpreter

```c
void vm_run(vm_t *vm) {
    cell_t *rsp_base = vm->rsp;
    while (vm->running && vm->rsp <= rsp_base) {
        cell_t xt = vm_fetch_ip(vm);  // Read next XT from mem[]
        vm->w = xt;
        vm->dict[xt].code(vm);        // Call handler
    }
}
```

Runs compiled code by fetching execution tokens (dictionary indices) from `mem[]` and calling their handlers. Stops when the return stack returns to starting level (i.e., `(exit)` pops back to the caller).

### Outer Interpreter

For each word in the input:
1. Look up in dictionary (`vm_find`)
2. If found and interpreting (or IMMEDIATE): execute
3. If found and compiling: compile the XT into `mem[]`
4. If not found: try as number
5. If number and interpreting: push to stack
6. If number and compiling: compile as `(lit) value`
7. Otherwise: error

### Stacks

Both stacks grow downward, 256 cells deep:

```c
cell_t dstack[256];   // Data stack
cell_t *sp;           // Stack pointer (points to TOS+1, grows down)

cell_t rstack[256];   // Return stack
cell_t *rsp;          // Return stack pointer
```

### Control Flow

All control flow words are IMMEDIATE. They compile branch instructions at compile time:

| Instruction | Behavior |
|-------------|----------|
| `(branch)` | Unconditional jump (reads target from next cell) |
| `(0branch)` | Jump if TOS = 0 |
| `(do)` | Set up DO loop (push limit and index to return stack) |
| `(?do)` | Like DO but skip loop body if limit = index |
| `(loop)` | Increment index, branch back if not done |
| `(+loop)` | Increment by TOS, branch back if not done |

Forward references (IF, WHILE) leave a placeholder and patch it when the target is known (THEN, REPEAT).

### Compile-time vs Runtime Separation

IMMEDIATE words that need different behavior at compile-time and runtime have separate handlers. Critical example — `DOES>`:

- **Compile-time** (`p_does_compile`): Compiles `(does>)` into the current definition
- **Runtime** (`p_does_runtime`): Sets the latest word's code to `dodoes` and records the DOES> IP

This separation prevents the DOES> bug where executing runtime behavior during compilation would corrupt the dictionary.

## Primitives Reference

### Stack Operations
`dup` `drop` `swap` `over` `rot` `-rot` `nip` `tuck` `?dup` `2dup` `2drop` `2swap` `2over` `>r` `r>` `r@` `2>r` `2r>` `2r@` `depth` `pick`

### Arithmetic
`+` `-` `*` `/` `mod` `/mod` `negate` `abs` `min` `max` `1+` `1-` `*/`

### Comparison
`=` `<>` `<` `>` `u<` `0=` `0<` `0>`

### Logic
`and` `or` `xor` `invert` `lshift` `rshift`

### Memory
`@` `!` `c@` `c!` `+!` `here` `allot` `cells` `cell+` `,` `c,` `move` `fill` `/string` `count`

### Strings
`s"` `s\"` `."` `.(` `[char]` `char`

### Compiler
`:` `;` `immediate` `[` `]` `state` `'` `[']` `execute` `>body` `create` `find` `literal` `compile,` `postpone` `does>` `recurse`

### Control Flow (IMMEDIATE)
`if` `else` `then` `begin` `while` `repeat` `until` `again` `do` `?do` `loop` `+loop` `i` `j` `unloop` `case` `of` `endof` `endcase` `exit`

### I/O
`emit` `type` `cr` `key` `accept` `.` `u.` `.s` `space` `spaces`

### Numeric Output
`<#` `#` `#s` `#>` `hold` `sign`

### File I/O
`open-file` `create-file` `close-file` `write-file` `read-line` `emit-file` `flush-file` `slurp-file` `r/o` `w/o` `r/w` `stdout`

### File Loading
`include` `require` `included`

### System
`system` `bye` `throw` `abort` `abort"` `noop`

### Constants
`true` `false` `bl` `base` `decimal` `hex`

### Number Parsing
`s>number?` `>number`

## Boot Words (core.fs)

The Forth bootstrap defines high-level words on top of C primitives:

`variable` `constant` `2constant` `value` `defer` `is` `str=` `n>str` `nls` `print` `2rot` `erase` `blank` `not` `.fifth`

## Building

```bash
make            # Optimized build (-O2)
make debug      # Debug build (-g -O0 -DDEBUG)
make clean      # Remove build artifacts
make test       # Run smoke tests
make size       # Show binary size and line counts
make install    # Copy to /usr/local/bin/
```

## Design Decisions

**Dictionary as struct array** (not flat memory): Simplifies implementation since Fifth doesn't need FORGET/MARKER. Each entry is a fixed-size C struct with clear fields.

**Case-insensitive lookup**: Standard Forth behavior. Uses `strncasecmp`.

**Indirect threading via function pointers**: Each dict entry has a C function pointer. Simpler than token threading or direct threading, and fast enough for Fifth's use case.

**Escape-aware `s\"`**: The parser handles `\"` inside escaped strings correctly, unlike a naive `vm_parse` that would stop at the first `"`.

**Require deduplication**: Uses `realpath()` to normalize paths before checking if a file has already been loaded.

**Boot file search**: Looks for `boot/core.fs` relative to the binary, then in `~/fifth/engine/boot/`. Falls back gracefully if not found.
