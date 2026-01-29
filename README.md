# Fifth

**A practical Forth ecosystem with multiple execution backends.**

Write Forth once, run it interpreted (fast startup) or compiled (native performance).

## Quick Start

```bash
# Interpreter (fast startup, 5-15% of C)
./fifth examples/project-dashboard.fs

# Compiler (native code, 70-85% of C)
./fifth compile examples/project-dashboard.fs -o dashboard
./dashboard

# C codegen (planned, backend exists but not yet wired to CLI)
# ./fifth --emit-c examples/project-dashboard.fs > dashboard.c
```

## Architecture

```
                YOUR FORTH CODE
                : square dup * ;
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   ./fifth        ./fifth        ./fifth
  (default)       compile       --emit-c
        │             │             │
        ▼             ▼             ▼
   Threaded code  Cranelift     gcc/clang
   Interpreter    JIT/AOT       → native
   5-15% of C     70-85% of C   50-70% of C
```

Same `.fs` files work on all backends.

## Project Structure

```
~/fifth/
├── fifth                    # Unified CLI wrapper
├── engine/                  # C interpreter (57 KB, zero deps)
│   ├── fifth.h              # Core types, VM struct
│   ├── vm.c                 # Interpreter, dictionary
│   ├── prims.c              # 164 primitives
│   ├── io.c                 # File I/O, system
│   ├── main.c               # Entry point
│   └── boot/core.fs         # Forth bootstrap
├── compiler/                # Rust compiler (Cranelift/LLVM)
│   ├── frontend/            # Lexer, parser, SSA
│   ├── optimizer/           # 5-pass optimization
│   ├── backend/             # Cranelift, LLVM, C codegen
│   └── runtime/             # C runtime library
├── lib/                     # Forth libraries
│   ├── core.fs              # Loads str+html+sql+template
│   ├── str.fs               # String buffers, parsing
│   ├── html.fs              # HTML5 generation, escaping
│   ├── sql.fs               # SQLite CLI interface
│   ├── template.fs          # Slots, deferred rendering
│   └── ui.fs                # Dashboard components
├── examples/                # Example applications
│   ├── db-viewer.fs         # Database HTML viewer
│   └── project-dashboard.fs # Tabbed dashboard
├── tcc/                     # TinyCC docs (optional)
└── docs/                    # Documentation
```

## Backends

| Backend | Output | Toolchain | Compile | Speed vs C |
|---------|--------|-----------|---------|------------|
| **Interpreter** | — | None (C11) | <1ms | 5-15% |
| **Cranelift JIT** | native | Rust (~400 MB) | ~50ms | 70-85% |
| **LLVM AOT** | native | Rust+LLVM (~800 MB) | 50-100ms | 85-110% |
| **C Codegen + clang** | native | clang | 10-20ms | 50-70% |
| **C Codegen + tcc** | native | tcc (~200 KB) | 2-5ms | 40-50% |

**When to use which:**
- **Interpreter**: Development, scripting, small programs
- **Cranelift**: Production binaries, fast compile cycle
- **LLVM**: Maximum performance, distribution
- **C Codegen**: Portability, embedding, no Rust needed

## Building

### Interpreter only (zero dependencies)

```bash
cd engine
make
./fifth ../examples/project-dashboard.fs
```

### Full toolchain (with compiler)

```bash
# Build interpreter
cd engine && make && cd ..

# Build compiler (requires Rust)
cd compiler && cargo build --release --features cranelift && cd ..

# Now use unified CLI
./fifth examples/project-dashboard.fs          # interpret
./fifth compile examples/project-dashboard.fs  # compile
```

## CLI Reference

```
INTERPRETER (default):
  ./fifth program.fs           Execute Forth file
  ./fifth -e "2 3 + ."         Execute one-liner
  ./fifth                      Interactive REPL

COMPILER:
  ./fifth compile program.fs   Compile to native binary
  ./fifth run program.fs       JIT execute
  ./fifth repl                 Compiled REPL
  ./fifth --emit-c program.fs  Emit C source

OPTIONS:
  --help, -h                   Show help
```

## Libraries

### str.fs - String Utilities

Buffer-based string operations without dynamic allocation.

```forth
require ~/fifth/lib/str.fs

str-reset
s" Hello, " str+
s" World!" str+
str$ type  \ prints: Hello, World!

s" apple|banana|cherry" 1 parse-pipe type  \ prints: banana
```

### html.fs - HTML Generation

Full HTML5 tag vocabulary with automatic XSS-safe escaping.

```forth
require ~/fifth/lib/html.fs

s" /tmp/page.html" w/o create-file throw html>file
s" My Page" html-head
html-body
  s" Hello <World>" h1.   \ auto-escaped!
html-end
html-fid @ close-file throw
```

### sql.fs - SQLite Interface

Query SQLite databases via CLI.

```forth
require ~/fifth/lib/sql.fs

s" mydb.db" s" SELECT COUNT(*) FROM users" sql-count .  \ prints: 42
```

### ui.fs - Dashboard Components

Pre-built components with dark theme CSS.

```forth
require ~/fifth/lib/ui.fs

42 s" Users" stat-card-n
s" Active" badge-success
card-begin ... card-end
tabs-begin ... tabs-end
```

## Philosophy

- **Write once, run anywhere**: Same Forth source for interpreter and compiler
- **Minimal dependencies**: Interpreter is 57 KB with zero deps
- **Proper escaping**: Security by default (HTML entities, SQL quoting)
- **Stack-based DSLs**: Build vocabularies that make intent clear
- **Composable**: Small words that combine into larger patterns
- **AI-native**: Explicit state, verifiable contracts, small vocabulary — ideal for LLM-assisted development (see [docs/agentic-coding.md](docs/agentic-coding.md))

## Examples

```bash
# Run examples with interpreter
./fifth examples/project-dashboard.fs
./fifth examples/db-viewer.fs

# Compile for distribution
./fifth compile examples/project-dashboard.fs -o dashboard
./dashboard
```

## Documentation

| Document | Description |
|----------|-------------|
| [docs/INDEX.md](docs/INDEX.md) | Documentation navigation |
| [docs/language-spec.md](docs/language-spec.md) | Complete language specification |
| [docs/forth-reference.md](docs/forth-reference.md) | Forth/Gforth reference |
| [docs/contributing.md](docs/contributing.md) | Development guide |
| [docs/roadmap.md](docs/roadmap.md) | Vision and priorities |
| [CLAUDE.md](CLAUDE.md) | Project constraints |

## License

MIT

## Contributing

Build the vocabulary. Submit words that solve real problems. See [docs/contributing.md](docs/contributing.md).
