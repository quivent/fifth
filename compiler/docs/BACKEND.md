# Backend Architecture

## Pipeline

```
SSA IR (Frontend)
    → Code Generator
    → Control Flow Lowering
    → LLVM IR → Optimization Passes → Object File
    → Linker → Executable
```

## LLVM Backend

Full SSA-to-LLVM-IR conversion with function/basic-block generation, PHI node support, module verification, and optimization pass integration. Supports AOT and JIT compilation modes. Optimization levels: None, Less, Default, Aggressive.

**Stack Cache** — Configurable depth (default 3 registers), 70-90% reduction in memory operations. Automatic spilling/filling.

**Primitives** — Arithmetic, comparison, logical, and unary operations for integer and floating-point types. Uses LLVM intrinsics where beneficial.

**Control Flow** — IF/THEN/ELSE, DO/LOOP, BEGIN/UNTIL, BEGIN/WHILE/REPEAT, tail call optimization. Proper PHI nodes for loop counters.

**Linker** — Static and dynamic linking. Multi-toolchain: Clang, GCC, LD (auto-detect with fallback).

### Dependencies

- `inkwell` 0.4 (LLVM Rust bindings), LLVM 16+
- Feature-gated: `--features llvm`

### Cranelift Backend

Alternative JIT backend via Cranelift (Wasmtime project). Faster compilation than LLVM, suitable for interactive/REPL use. Feature-gated: `--features cranelift`.

## Bootstrapping Strategy

The full compiler requires Rust + LLVM. Here's the honest breakdown:

| Backend | Output Binary | Toolchain Size | Compile Time | Runtime vs C |
|---------|---------------|----------------|--------------|--------------|
| LLVM AOT | 2.6 MB | ~800 MB | 50-100ms | 85-110% |
| Cranelift JIT | 2.6 MB | ~400 MB | ~50ms | 70-85% |
| C Codegen + gcc | varies | ~100 MB | 10-20ms | 50-70% |
| C Codegen + tcc | varies | ~200 KB | 5-10ms | 40-60% |
| Fifth Engine | 57 KB | 0 (self-contained) | <1ms | 5-15% |

**Toolchain breakdown:**
- **LLVM AOT**: Requires Rust (~300 MB) + LLVM 16+ libs (~500 MB). Best codegen, heaviest deps.
- **Cranelift JIT**: Requires Rust (~300 MB) + Cranelift (~100 MB). Good balance.
- **C Codegen**: Emits C via `CCodegen` (optimizer/src/codegen.rs). Use any C compiler.
- **C Codegen + tcc**: TinyCC can be bundled (~200 KB). Zero external deps, ~60% of C perf.
- **Fifth Engine**: Pure C11 interpreter at ~/fifth/engine/. 2,270 lines, 164 primitives, complete Forth. No toolchain needed beyond `cc`.

**Feature flags** (Cargo.toml):
```toml
cranelift = ["backend/cranelift"]  # ~400 MB toolchain
llvm = ["backend/llvm"]            # ~800 MB toolchain
interpreter = []                   # Pure Rust, no JIT
```

**Recommendation:**
- **Development**: Cranelift JIT (fast iteration, ~50ms compile)
- **Distribution**: C Codegen + tcc (zero deps, portable)
- **Embedding**: Fifth Engine (57 KB, self-contained, MIT license)

## Integration

```
CLI Binary (src/main.rs)
    → Integration Layer (src/lib.rs)
    → Frontend / Optimizer / Backend (workspace crates)
```

### Compilation Phases

1. **Frontend**: Lexer/Parser → AST → Semantic Analysis → Type Inference → SSA
2. **IR Conversion**: SSA → stack-based IR
3. **Optimization**: 5 passes (constant folding, inlining, superinstructions, dead code, stack caching). 40-60% instruction reduction at Aggressive level.
4. **Code Generation**: AOT → LLVM IR → native executable. JIT → compile & execute → result value.
