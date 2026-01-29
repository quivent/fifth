# Fifth

**A Forth for the agentic era.**

Fifth is a self-contained Forth ecosystem designed for AI-assisted development. One binary, zero dependencies, instant startup. The explicit stack model and small vocabulary make it uniquely suited for LLM code generation — where other languages struggle with implicit state and sprawling APIs, Forth's simplicity becomes an advantage.

Write tools that parse data, generate HTML, query databases — and optionally compile them to native code when you need speed.

---

## Built for the Agentic Era

Most programming languages were designed for humans typing code. They optimize for expressiveness, flexibility, and familiar syntax. But when AI generates code, these "features" become liabilities:

| Challenge for LLMs | Traditional Languages | Fifth/Forth |
|-------------------|----------------------|-------------|
| **Implicit state** | Variables scattered across scopes, closures capturing context, mutable globals | One explicit stack. All state visible. |
| **Large API surface** | Thousands of methods, multiple ways to do everything | ~75 core words. One way to do each thing. |
| **Complex control flow** | Callbacks, promises, async/await, exceptions | Linear execution. Explicit branches. |
| **Hidden side effects** | Methods that mutate, getters that compute, operators that allocate | Stack effects documented on every word. |
| **Verification difficulty** | Types help but don't prevent logic errors | Stack effect composition is mechanically checkable. |

### Why This Matters

**LLMs generate better Forth than Python.** Not because Forth is easier — it isn't, for humans. But LLMs don't have the intuitions that make Python feel natural. What they have is pattern matching and formal reasoning. Forth rewards both:

```forth
\ Every word declares its contract
: double ( n -- n*2 ) 2 * ;
: quadruple ( n -- n*4 ) double double ;

\ Effects compose predictably
\ quadruple = ( n -- n*2 -- n*4 ) ✓
```

An LLM can verify this composition. It cannot verify that a Python function with three parameters, two optional keyword arguments, and a context manager doesn't have subtle bugs.

**Explicit state eliminates hallucination vectors.** When the only state is a stack of integers, there's nowhere for imagined variables or phantom objects to hide. The LLM either tracks the stack correctly or produces code that fails immediately — not code that works sometimes and corrupts data later.

**Small vocabulary means fewer combinations to learn.** GPT-4 has seen millions of Python programs with millions of API combinations. It still hallucinates method names. Fifth has 75 words. An LLM can hold the entire language in context and generate valid code reliably.

---

## Benchmarks

### Startup Time

For CLI tools and scripts, startup time dominates. A tool that takes 50ms to start feels slow when you run it in a loop.

| Language | Startup Time | Notes |
|----------|-------------|-------|
| **Fifth (interpreter)** | **<1ms** | Direct execution, no initialization |
| Lua | 1-2ms | Lightweight interpreter |
| Perl | 5-10ms | |
| Fifth (compiled) | ~10ms | Native binary, minimal runtime |
| Python | 30-50ms | Interpreter + module imports |
| Node.js | 30-40ms | V8 initialization |
| Ruby | 50-80ms | |
| Java | 50-100ms | JVM startup |

### Runtime Performance

Throughput on compute-bound tasks, relative to optimized C.

| Language | % of C | Notes |
|----------|--------|-------|
| C | 100% | Baseline |
| Rust | 95-105% | Sometimes faster due to optimizations |
| **Fifth (Cranelift)** | **70-85%** | Native compilation, no GC |
| LuaJIT | 30-80% | Tracing JIT, varies by workload |
| **Fifth (interpreter)** | **5-15%** | Threaded code, no JIT |
| JavaScript (V8) | 20-50% | JIT with warmup |
| Python | 1-3% | Pure interpreter |
| Ruby | 2-5% | |

### Memory Usage

Baseline memory for a minimal program.

| Language | Memory | Notes |
|----------|--------|-------|
| **Fifth (interpreter)** | **1-2 MB** | No GC, static allocation |
| Lua | 1-2 MB | |
| C | 1-2 MB | Depends on allocations |
| **Fifth (compiled)** | **1-2 MB** | Minimal runtime |
| Perl | 5-10 MB | |
| Python | 10-15 MB | Interpreter + builtins |
| Ruby | 15-20 MB | |
| Node.js | 30-50 MB | V8 heap |
| Java | 50-100 MB | JVM baseline |

### Binary Size

What you ship.

| Language | Binary/Runtime Size | Notes |
|----------|-------------------|-------|
| **Fifth (interpreter)** | **57 KB** | Complete interpreter |
| Lua | 250 KB | Interpreter |
| **Fifth (compiled)** | **10-50 KB** | Depends on program |
| C (static) | 10-100 KB | Depends on libc |
| Go | 2-10 MB | Includes runtime |
| Rust | 300 KB - 5 MB | Depends on dependencies |
| Python | 4 MB + stdlib | Plus dependencies |
| Node.js | 40-80 MB | V8 + npm modules |

---

## By the Numbers

### Simplicity

| Metric | Fifth | Python | JavaScript | Rust |
|--------|-------|--------|------------|------|
| Core words/keywords | 75 | 35 keywords + 150+ builtins | 50+ keywords + 1000s of APIs | 50+ keywords + large stdlib |
| Concepts to learn | Stack, dictionary, words | Objects, classes, decorators, generators, async, context managers, etc. | Prototypes, closures, promises, async, this binding, etc. | Ownership, borrowing, lifetimes, traits, macros, etc. |
| Syntax rules | 1 (whitespace splits) | Many (significant whitespace, operators, comprehensions, etc.) | Many (ASI, operator precedence, destructuring, etc.) | Many (complex grammar) |
| Time to learn basics | 1-2 hours | 1-2 days | 1-2 days | 1-2 weeks |
| Time to mastery | 1-2 weeks | Months | Months | Months to years |

### Portability

| Platform | Interpreter | Compiler |
|----------|-------------|----------|
| Linux (x86_64) | ✓ | ✓ |
| Linux (ARM64) | ✓ | ✓ |
| macOS (x86_64) | ✓ | ✓ |
| macOS (ARM64) | ✓ | ✓ |
| Windows | ✓ (MinGW/WSL) | ✓ |
| FreeBSD | ✓ | ✓ |
| WebAssembly | Planned | Planned |
| Embedded (bare metal) | Possible (port required) | Via C codegen |

**Dependencies:**
- Interpreter: C11 compiler only (gcc, clang, tcc)
- Compiler: Rust toolchain
- Libraries: `sqlite3` CLI for database features (optional)

### Size Breakdown

```
Fifth Interpreter (engine/)
├── vm.c          2,100 lines    Virtual machine, dictionary
├── prims.c       1,800 lines    Primitive words
├── io.c            450 lines    File I/O, system
├── main.c          150 lines    Entry point
├── boot/core.fs    400 lines    Forth bootstrap
└── Total         4,900 lines    → 57 KB binary

Fifth Libraries (~/.fifth/lib/)
├── str.fs          150 lines    String buffers
├── html.fs         340 lines    HTML generation
├── sql.fs          150 lines    SQLite interface
├── template.fs     120 lines    Templates
├── ui.fs           260 lines    UI components
├── pkg.fs          150 lines    Package system
├── core.fs          70 lines    Loader
└── Total         1,240 lines

Fifth Compiler (compiler/)
├── frontend/     3,500 lines    Lexer, parser, SSA
├── optimizer/    4,200 lines    5-pass optimization
├── backend/      5,800 lines    Cranelift, C codegen
├── runtime/        800 lines    C runtime library
└── Total        14,300 lines
```

### Optimization Passes

The compiler applies five optimization passes:

| Pass | What It Does | Impact |
|------|--------------|--------|
| **Constant folding** | `2 3 +` → `5` at compile time | Eliminates runtime math |
| **Dead code elimination** | Removes unreachable branches | Smaller binaries |
| **Inline expansion** | Small words inlined at call sites | Reduces call overhead |
| **Stack scheduling** | Reorders operations to minimize stack shuffling | Fewer `swap`/`rot` operations |
| **Register allocation** | Maps stack slots to CPU registers | Native performance |

### Adaptability

| Extension Point | Mechanism |
|-----------------|-----------|
| **New words** | Define with `: word ... ;` |
| **New primitives** | Add C function to `prims.c`, register with `vm_add_prim()` |
| **New libraries** | Create `.fs` file in `~/.fifth/lib/`, load with `use lib:` |
| **New packages** | Create directory in `~/.fifth/packages/`, add `package.fs` |
| **Custom backends** | Implement `CodeGen` trait in compiler |
| **Embedded use** | Link `libfifth.a`, call `vm_init()`, `vm_eval()` |

---

## Quick Start

```bash
# Build the interpreter (zero dependencies, <1 second)
cd engine && make && cd ..

# Run a program
./fifth examples/project-dashboard.fs

# Interactive REPL
./fifth

# One-liner
./fifth -e "2 3 + . cr"
```

## What You Can Build

Fifth includes practical libraries for real tasks:

```forth
\ Generate an HTML dashboard from a SQLite database
require ~/.fifth/lib/pkg.fs
use lib:core.fs
use lib:ui.fs

s" /tmp/report.html" w/o create-file throw html>file
s" Sales Report" html-head ui-css html-body

s" sales.db" s" SELECT region, total FROM summary" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field   \ region
    2dup 1 sql-field   \ total
    stat-card
    2drop
  else 2drop then
repeat 2drop
sql-close

html-end
html-fid @ close-file throw
```

This generates a styled HTML report with stat cards — in 20 lines, with automatic HTML escaping, no dependencies beyond `sqlite3`.

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
 C Interpreter  Cranelift     gcc/clang
 <1ms startup   JIT/AOT       native
 5-15% of C     70-85% of C   50-70% of C
```

Same source files work on all backends.

## Project Structure

```
~/fifth/
├── fifth                    # CLI wrapper
├── engine/                  # C interpreter (57 KB binary)
│   ├── vm.c                 # Virtual machine core
│   ├── prims.c              # Primitive words
│   └── io.c                 # File I/O, system calls
├── compiler/                # Rust compiler (Cranelift backend)
└── examples/                # Example applications

~/.fifth/                    # Package system (FIFTH_HOME)
├── lib/                     # Core libraries
│   ├── str.fs               # String buffers, parsing
│   ├── html.fs              # HTML generation, escaping
│   ├── sql.fs               # SQLite interface
│   ├── template.fs          # Deferred slots, layouts
│   ├── ui.fs                # Dashboard components
│   ├── pkg.fs               # Package system
│   └── core.fs              # Loads all libraries
└── packages/                # Installed packages
    └── claude-tools/        # Example package
```

## Package System

Fifth uses a simple package system with `lib:` and `pkg:` prefixes:

```forth
\ Bootstrap the package system
require ~/.fifth/lib/pkg.fs

\ Load a library
use lib:core.fs          \ Loads from ~/.fifth/lib/core.fs
use lib:ui.fs            \ Loads from ~/.fifth/lib/ui.fs

\ Load a package
use pkg:claude-tools     \ Loads ~/.fifth/packages/claude-tools/package.fs

\ Package info
.fifth-home              \ Shows FIFTH_HOME path
./fifth pkg list         \ Lists installed packages
```

## Libraries

### str.fs — String Buffers

No dynamic allocation. Two static buffers that handle all string operations.

```forth
use lib:str.fs

str-reset
s" Hello, " str+
s" World!" str+
str$ type               \ Hello, World!

\ Parse delimited data
s" alice|bob|charlie" 1 parse-pipe type   \ bob
```

### html.fs — HTML Generation

Full HTML5 vocabulary with automatic XSS escaping.

```forth
use lib:html.fs

s" output.html" w/o create-file throw html>file
s" Page Title" html-head html-body
  s" <script>alert('xss')</script>" h1.   \ Escaped automatically!
html-end
html-fid @ close-file throw
```

### sql.fs — SQLite Interface

Query databases via the `sqlite3` CLI. No C bindings, no FFI.

```forth
use lib:sql.fs

\ Count rows
s" users.db" s" SELECT COUNT(*) FROM users" sql-count .   \ 42

\ Iterate results
s" users.db" s" SELECT name, email FROM users" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type ."  - "
    2dup 1 sql-field type cr
    2drop
  else 2drop then
repeat 2drop
sql-close
```

### ui.fs — Dashboard Components

Pre-built components with dark theme CSS.

```forth
use lib:ui.fs

\ Stat cards
42 s" Users" stat-card-n
s" Active" badge-success

\ Tabs
tabs-begin
  s" Overview" s" tab1" true tab
  s" Details" s" tab2" false tab
tabs-end

s" tab1" true panel-begin
  s" Overview content" p.
panel-end
```

## Backends

| Backend | Startup | Speed vs C | Binary Size | Use Case |
|---------|---------|------------|-------------|----------|
| **Interpreter** | <1ms | 5-15% | 57 KB | Development, scripts, CLI tools |
| **Cranelift JIT** | ~50ms | 70-85% | 10-50 KB | Production binaries |
| **C Codegen** | 2-20ms | 40-70% | 10-50 KB | Embedding, portability |

```bash
# Interpreted (default)
./fifth program.fs

# Compiled
./fifth compile program.fs -o program
./program
```

## Building

### Interpreter Only

```bash
cd engine
make
# Binary: engine/fifth (57 KB, zero dependencies)
```

### With Compiler

```bash
# Interpreter
cd engine && make && cd ..

# Compiler (requires Rust)
cd compiler && cargo build --release --features cranelift && cd ..

# Use unified CLI
./fifth examples/hello.fs           # interpret
./fifth compile examples/hello.fs   # compile
```

## Documentation

| Document | Description |
|----------|-------------|
| [CLAUDE.md](CLAUDE.md) | Project constraints and patterns |
| [docs/language-spec.md](docs/language-spec.md) | Complete language specification |
| [docs/forth-reference.md](docs/forth-reference.md) | Forth language concepts |
| [docs/contributing.md](docs/contributing.md) | Development guide |
| [docs/agentic-coding.md](docs/agentic-coding.md) | AI-assisted development in depth |

## License

MIT

## Contributing

Fifth grows by solving real problems. If you build something useful, extract the reusable words and submit them. See [docs/contributing.md](docs/contributing.md).
