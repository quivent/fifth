# fast-forth

A high-performance Forth compiler, embeddable runtime, and AI-assisted code generation toolkit.

## Three Things

**Compiler** — Frontend (lexer, parser, SSA) → Optimizer (constant folding, inlining, superinstructions, dead code elimination, stack caching) → Backend (Cranelift JIT / LLVM AOT). Targets 80-100% of C performance with sub-100ms compile times.

**Runtime** — Embeddable Forth execution engine with C runtime, FFI, concurrency primitives, and interactive REPL. Stack-based calling convention: `fn(*mut i64) -> *mut i64`.

**Codegen** — Specification-driven code generation with provenance tracking, semantic diff, symbolic equivalence checking, and pattern database. Built for AI agent workflows.

## Quick Start

```bash
# Build
cargo build --release

# Execute Forth code
./target/release/fastforth execute "10 20 + 3 *"

# Interactive REPL
./target/release/fastforth repl

# Run tests
cargo test
```

## Project Layout

```
Compiler (turns Forth source code into native machine code):
  frontend/       Reads Forth source → produces structured representation
  optimizer/      Transforms code to run faster (5 passes)
  backend/        Produces native machine code (Cranelift JIT or LLVM)

Runtime (executes Forth programs):
  runtime/        C library: virtual machine, memory, FFI, threads
  cli/            Command-line tool: compiler, REPL, profiler

AI Code Generation (builds Forth programs from specifications):
  src/            Spec engine, pattern database, provenance, semantic diff

Support:
  tests/          Test suites (compliance, correctness, fuzzing, stress)
  benches/        Performance benchmarks (Criterion)
  benchmarks/     Comparison benchmarks against C baselines
  examples/       Sample Forth programs and usage demos
  scripts/        Build and automation scripts
  docs/           Architecture, reference, and guides
```

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full system design.

## Documentation

| Doc | Contents |
|-----|----------|
| [ARCHITECTURE](docs/ARCHITECTURE.md) | System design, IR layers, type system |
| [QUICK_START](docs/QUICK_START.md) | Installation and first program |
| [BACKEND](docs/BACKEND.md) | Cranelift/LLVM backends, bootstrapping |
| [PERFORMANCE](docs/PERFORMANCE.md) | Benchmarks, optimization targets |
| [CONCURRENCY](docs/CONCURRENCY.md) | Threading primitives, multi-agent |
| [RUNTIME_REFERENCE](docs/RUNTIME_REFERENCE.md) | Stack operations, memory model |
| [ERROR_CODES](docs/ERROR_CODES.md) | Error taxonomy |
| [TESTING_GUIDE](docs/TESTING_GUIDE.md) | Test organization and coverage |
| [CROSS_PLATFORM](docs/CROSS_PLATFORM_SUPPORT.md) | Platform support |
| [SYSTEM_DIAGRAMS](docs/SYSTEM_DIAGRAMS.md) | Architecture diagrams |

## Status

See [ROADMAP.md](ROADMAP.md) for implementation phases and next steps.
