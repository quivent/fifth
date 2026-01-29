# Fifth Backends

Fifth supports multiple execution backends. Same Forth source, different performance characteristics.

## Overview

```
Forth Source (.fs)
       │
       ├──► Interpreter (engine/)
       │         │
       │         ▼
       │    Threaded code execution
       │    5-15% of C performance
       │
       └──► Compiler (compiler/)
                 │
        ┌────────┼────────┐
        ▼        ▼        ▼
   Cranelift   LLVM    C Codegen
   70-85%      85-110%   50-70%
```

## Backend Comparison

| Backend | Output | Toolchain Size | Compile Time | Runtime vs C |
|---------|--------|----------------|--------------|--------------|
| **Interpreter** | — | 57 KB | <1ms | 5-15% |
| **Cranelift JIT** | native | ~400 MB | ~50ms | 70-85% |
| **LLVM AOT** | native | ~800 MB | 50-100ms | 85-110% |
| **C + clang** | native | ~100 MB | 10-20ms | 50-70% |
| **C + tcc** | native | ~200 KB | 2-5ms | 40-50% |

## Interpreter (`engine/`)

The interpreter is a C11 implementation of a threaded-code Forth.

### How It Works

```c
// Dictionary entry
typedef struct {
    char name[32];
    prim_fn code;      // C function pointer
    cell_t param;      // Body (data address or constant)
} dict_entry_t;

// Inner interpreter
while (running) {
    cell_t xt = fetch_next();   // Read XT from mem[]
    dict[xt].code(vm);          // Call handler
}
```

Each word is either:
- A **primitive** (C function)
- A **colon definition** (compiled code in mem[])

### Performance

The interpreter spends time on:
1. Fetching the next execution token (memory read)
2. Indirect call through function pointer
3. Function call/return overhead

This adds ~10-20 cycles per word executed, resulting in 5-15% of native C performance.

### When to Use

- Development and debugging
- Small programs
- Scripting
- When you need instant startup

### Building

```bash
cd engine
make
./fifth program.fs
```

Binary size: ~57 KB with zero external dependencies.

---

## Compiler (`compiler/`)

The compiler transforms Forth source into optimized machine code.

### Pipeline

```
Forth Source
    ↓
Frontend (Lexer → Parser → AST)
    ↓
SSA Conversion (type inference, stack effects)
    ↓
Optimizer (5 passes)
    ↓
Backend (Cranelift / LLVM / C)
    ↓
Native Code
```

### Optimization Passes

1. **Constant Folding**: Evaluate compile-time constants
2. **Inlining**: Inline small words
3. **Superinstructions**: Fuse common patterns (`dup *` → `DupMul`)
4. **Dead Code Elimination**: Remove unreachable code
5. **Stack Caching**: Keep TOS/NOS in registers

### Cranelift Backend

Fast JIT compilation via the Cranelift code generator (used by Wasmtime).

```bash
./fifth compile program.fs              # AOT compilation
./fifth run program.fs                  # JIT execution
```

- Compile time: ~50ms
- Runtime: 70-85% of C
- Best for: Development, fast iteration

### LLVM Backend

Full optimization via LLVM (same backend as Clang/Rust).

```bash
./fifth compile program.fs --backend=llvm
```

- Compile time: 50-100ms
- Runtime: 85-110% of C (can exceed C due to whole-program optimization)
- Best for: Production binaries, maximum performance

### C Codegen Backend

Emits C source code that can be compiled with any C compiler.

```bash
./fifth --emit-c program.fs > program.c
clang -O2 program.c -o program
```

Generated C looks like:
```c
#define TOS (sp[-1])
#define NOS (sp[-2])
#define PUSH(x) (*sp++ = (x))
#define DROP (sp--)

void square(void) {
    TOS = TOS * TOS;  // Superinstruction: dup *
}
```

- Portable to any platform with a C compiler
- No Rust toolchain required
- Good for embedding

### When to Use

| Use Case | Backend |
|----------|---------|
| Development | Interpreter or Cranelift |
| Production | LLVM or Cranelift |
| Embedding | C Codegen |
| No Rust available | C Codegen |
| Maximum portability | C Codegen |
| Maximum performance | LLVM |

### Building

```bash
cd compiler

# With Cranelift (recommended)
cargo build --release --features cranelift

# With LLVM (requires LLVM 16+)
cargo build --release --features llvm

# Development build (fast compile, slow runtime)
cargo build --features dev-fast
```

---

## TinyCC (Optional)

TinyCC is a small, fast C compiler that can be bundled for zero-dependency builds.

See `tcc/README.md` for installation instructions.

```bash
# With bundled TCC
./fifth --emit-c program.fs > program.c
tcc program.c -o program
```

TCC compiles ~5x faster than GCC/Clang but produces slower code (40-50% of C).

---

## Choosing a Backend

### Decision Tree

```
Need instant startup?
├── Yes → Interpreter
└── No
    └── Need maximum performance?
        ├── Yes → LLVM
        └── No
            └── Need to avoid Rust toolchain?
                ├── Yes → C Codegen
                └── No → Cranelift
```

### Recommendation by Use Case

| Use Case | Recommended |
|----------|-------------|
| Learning Forth | Interpreter |
| Scripting | Interpreter |
| Web services | Cranelift |
| CLI tools | Cranelift |
| Number crunching | LLVM |
| Embedded systems | C Codegen |
| Distribution | LLVM or C Codegen |

---

## Technical Details

### Threaded Code (Interpreter)

The interpreter uses indirect threaded code:

```
Memory layout for: : square dup * ;

dict[42] = { name: "square", code: docol, param: 1000 }

mem[1000]: [xt_dup]    // dict index of 'dup'
mem[1008]: [xt_star]   // dict index of '*'
mem[1016]: [xt_exit]   // dict index of '(exit)'
```

Execution:
1. `docol` pushes return address, sets IP to param
2. Inner loop fetches XT, calls handler
3. `(exit)` pops return address, continues

### Native Code (Compiler)

The compiler generates register-based machine code:

```asm
; square: dup *
square:
    mov rax, [rsp]      ; dup: load TOS
    imul rax, rax       ; *: multiply
    mov [rsp], rax      ; store result
    ret
```

No interpreter loop, no indirection, just machine instructions.

### Stack Caching

The optimizer keeps the top 1-3 stack values in registers:

```
Without caching:         With caching:
  mov rax, [rsp]          ; TOS already in r12
  add rax, [rsp-8]        add r12, r13
  mov [rsp-8], rax        ; result stays in r12
  sub rsp, 8
```

This reduces memory traffic by 70-90% for typical code.
