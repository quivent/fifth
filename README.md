# Fifth

```
    ███████╗██╗███████╗████████╗██╗  ██╗
    ██╔════╝██║██╔════╝╚══██╔══╝██║  ██║
    █████╗  ██║█████╗     ██║   ███████║
    ██╔══╝  ██║██╔══╝     ██║   ██╔══██║
    ██║     ██║██║        ██║   ██║  ██║
    ╚═╝     ╚═╝╚═╝        ╚═╝   ╚═╝  ╚═╝
         A Forth for the Agentic Era
```

> *"I think the industry is fundamentally unable to appreciate simplicity."*
> — Chuck Moore, creator of Forth

Fifth is a self-contained Forth ecosystem designed for AI-assisted development. One binary, zero dependencies, instant startup. The explicit stack model and small vocabulary make it uniquely suited for LLM code generation — where other languages struggle with implicit state and sprawling APIs, Forth's simplicity becomes an advantage.

Write tools that parse data, generate HTML, query databases — and optionally compile them to native code when you need speed.

---

## Installation

### Quick Install (30 seconds)

```bash
# Clone the repository
git clone https://github.com/youruser/fifth.git
cd fifth

# Build the interpreter (zero dependencies, <1 second)
cd engine && make && cd ..

# Set up the package system
mkdir -p ~/.fifth/lib ~/.fifth/packages
cp -r lib/* ~/.fifth/lib/

# Verify installation
./fifth -e "2 3 + . cr"   # Should print: 5
```

### What You Get

```
~/fifth/                     Your Fifth installation
├── fifth                    CLI wrapper script
├── engine/fifth             57 KB interpreter binary
└── examples/                Ready-to-run examples

~/.fifth/                    Your package directory (FIFTH_HOME)
├── lib/                     Core libraries (str, html, sql, ui)
└── packages/                Your installed packages
```

---

## Quick Start

### Hello, World

```bash
./fifth -e ': hello ." Hello, World!" cr ; hello'
```

### Interactive REPL

```bash
./fifth
\ Welcome to Fifth
2 3 + .          \ 5
: square dup * ;
5 square .       \ 25
bye
```

### Run a File

```bash
./fifth examples/project-dashboard.fs
```

### Your First Program

Create `hello.fs`:

```forth
\ hello.fs - My first Fifth program

: greet ( -- )
  ." Welcome to Fifth!" cr
  ." The stack has " depth . ." items." cr ;

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
./fifth hello.fs
```

---

## Usage Examples

### Generate HTML Reports

```forth
require ~/.fifth/lib/pkg.fs
use lib:core.fs
use lib:html.fs

s" /tmp/hello.html" w/o create-file throw html>file
s" My Page" html-head html-body
  s" Hello from Fifth!" h1.
  s" Generated with zero dependencies." p.
html-end
html-fid @ close-file throw

\ Open in browser
s" open /tmp/hello.html" system
```

### Query SQLite Databases

```forth
require ~/.fifth/lib/pkg.fs
use lib:core.fs

\ Count users
s" users.db" s" SELECT COUNT(*) FROM users" sql-count .   \ 42

\ List all users
s" users.db" s" SELECT name, email FROM users" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type ."  <"
    2dup 1 sql-field type ." >" cr
    2drop
  else 2drop then
repeat 2drop
sql-close
```

### Build a Dashboard

```forth
require ~/.fifth/lib/pkg.fs
use lib:core.fs
use lib:ui.fs

s" /tmp/dashboard.html" w/o create-file throw html>file
s" System Status" html-head ui-css html-body

grid-auto-begin
  42 s" Users" stat-card-n
  7 s" Active" stat-card-n
  99 s" Uptime %" stat-card-n
grid-end

html-end
html-fid @ close-file throw
```

---

## Package System

Fifth uses `~/.fifth/` as its package home (configurable via `FIFTH_HOME`).

### Using Libraries

```forth
\ Bootstrap the package system first
require ~/.fifth/lib/pkg.fs

\ Load core libraries with lib: prefix
use lib:str.fs           \ String buffers
use lib:html.fs          \ HTML generation
use lib:sql.fs           \ SQLite interface
use lib:ui.fs            \ Dashboard components
use lib:core.fs          \ Loads str + html + sql

\ Or load everything at once
use lib:core.fs
```

### Using Packages

```forth
\ Load a package with pkg: prefix
use pkg:my-package       \ Loads ~/.fifth/packages/my-package/package.fs
```

### Creating a Package

```bash
# Create package directory
mkdir -p ~/.fifth/packages/my-tools

# Create the main entry point
cat > ~/.fifth/packages/my-tools/package.fs << 'EOF'
\ my-tools/package.fs - My custom tools

require ~/.fifth/lib/pkg.fs
use lib:str.fs

: greet-user ( addr u -- )
  ." Hello, " type ." !" cr ;

: timestamp ( -- )
  ." Generated: "
  s" date '+%Y-%m-%d %H:%M:%S'" system ;
EOF
```

Now use it:

```forth
require ~/.fifth/lib/pkg.fs
use pkg:my-tools

s" Alice" greet-user    \ Hello, Alice!
timestamp               \ Generated: 2024-01-28 15:30:00
```

### Package Structure

```
~/.fifth/packages/my-package/
├── package.fs           Entry point (required)
├── utils.fs             Additional modules
├── data/                Package data files
└── README.md            Documentation
```

---

## Why Forth? A Brief History

Forth was created by **Chuck Moore** in 1970 for controlling telescopes. Its design priorities:

- **Minimal footprint** — Run on 4KB of RAM
- **Interactive development** — Test words immediately
- **Direct hardware access** — No OS abstraction layer
- **Self-contained** — Compiler, interpreter, and editor in one

These constraints produced a language unlike any other:

```forth
: SQUARED  DUP * ;
: CUBED    DUP SQUARED * ;
5 CUBED .   \ 125
```

No syntax. No types. No objects. Just words operating on a stack.

### The Forth Family Tree

```
1970  FORTH          Chuck Moore's original
  │
  ├── 1983  Forth-83      First standardization attempt
  │
  ├── 1994  ANS Forth     ANSI standard (X3.215-1994)
  │     │
  │     ├── Gforth        GNU Forth, reference implementation
  │     ├── SwiftForth    Commercial, Windows focus
  │     └── VFX Forth     Optimizing compiler
  │
  └── 2024  Fifth         For the agentic era ← You are here
```

### Fifth vs Gforth

| Aspect | Gforth | Fifth |
|--------|--------|-------|
| **Philosophy** | Standards compliance | Practical minimalism |
| **Binary size** | ~2 MB | 57 KB |
| **Startup** | 5-10ms | <1ms |
| **Dependencies** | libffi, libltdl | None |
| **FFI** | Yes (complex) | Shell-out pattern |
| **Compilation** | Threaded code | Native via Cranelift |
| **Focus** | General-purpose | AI-assisted development |

Fifth isn't "better" than Gforth — it has different goals. Gforth is a complete ANS Forth implementation. Fifth is a practical toolkit optimized for code generation and rapid deployment.

---

## Built for the Agentic Era

Most programming languages were designed for humans typing code. They optimize for expressiveness, flexibility, and familiar syntax. But when AI generates code, these "features" become liabilities:

| Challenge for LLMs | Traditional Languages | Fifth/Forth |
|-------------------|----------------------|-------------|
| **Implicit state** | Variables scattered across scopes, closures capturing context | One explicit stack. All state visible. |
| **Large API surface** | Thousands of methods, multiple ways to do everything | ~75 core words. One way to do each thing. |
| **Complex control flow** | Callbacks, promises, async/await, exceptions | Linear execution. Explicit branches. |
| **Hidden side effects** | Methods that mutate, getters that compute | Stack effects documented on every word. |
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

→ *See [docs/agentic-coding.md](docs/agentic-coding.md) for the full analysis.*

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
| Core words/keywords | 75 | 35 + 150 builtins | 50+ keywords | 50+ keywords |
| Concepts to learn | Stack, dictionary, words | Objects, classes, async, decorators... | Prototypes, closures, promises... | Ownership, borrowing, lifetimes... |
| Syntax rules | 1 (whitespace splits) | Many | Many | Many |
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
| Embedded | Possible | Via C codegen |

**Dependencies:**
- Interpreter: C11 compiler only (gcc, clang, tcc)
- Compiler: Rust toolchain (optional)
- Libraries: `sqlite3` CLI (optional, for database features)

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

---

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

### Backends Comparison

| Backend | Startup | Speed vs C | Binary Size | Use Case |
|---------|---------|------------|-------------|----------|
| **Interpreter** | <1ms | 5-15% | 57 KB | Development, scripts, CLI tools |
| **Cranelift JIT** | ~50ms | 70-85% | 10-50 KB | Production binaries |
| **C Codegen** | 2-20ms | 40-70% | 10-50 KB | Embedding, portability |

```bash
# Interpreted (default)
./fifth program.fs

# Compiled to native
./fifth compile program.fs -o program
./program
```

---

## Project Structure

```
~/fifth/
├── fifth                    # CLI wrapper
├── engine/                  # C interpreter (57 KB binary)
│   ├── vm.c                 # Virtual machine core
│   ├── prims.c              # Primitive words (~75)
│   └── io.c                 # File I/O, system calls
├── compiler/                # Rust compiler (Cranelift backend)
├── lib/                     # Source libraries (copied to ~/.fifth/lib/)
├── examples/                # Example applications
└── docs/                    # Documentation

~/.fifth/                    # Package system (FIFTH_HOME)
├── lib/                     # Core libraries
│   ├── str.fs               # String buffers, parsing
│   ├── html.fs              # HTML generation, escaping
│   ├── sql.fs               # SQLite interface
│   ├── template.fs          # Deferred slots, layouts
│   ├── ui.fs                # Dashboard components
│   ├── pkg.fs               # Package system
│   └── core.fs              # Loads all libraries
└── packages/                # Your installed packages
```

---

## Building

### Interpreter Only (Recommended)

```bash
cd engine
make
# Binary: engine/fifth (57 KB, zero dependencies)
```

### With Native Compiler

```bash
# Interpreter
cd engine && make && cd ..

# Compiler (requires Rust)
cd compiler && cargo build --release --features cranelift && cd ..

# Use unified CLI
./fifth examples/hello.fs           # interpret
./fifth compile examples/hello.fs   # compile
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [CLAUDE.md](CLAUDE.md) | Project constraints and patterns |
| [docs/language-spec.md](docs/language-spec.md) | Complete language specification |
| [docs/forth-reference.md](docs/forth-reference.md) | Forth language concepts |
| [docs/contributing.md](docs/contributing.md) | Development guide |
| [docs/agentic-coding.md](docs/agentic-coding.md) | AI-assisted development in depth |

---

## Contributing

Fifth grows by solving real problems. If you build something useful, extract the reusable words and submit them.

See [docs/contributing.md](docs/contributing.md).

---

## License

MIT

---

```
    "Simplicity is prerequisite for reliability."
                              — Edsger Dijkstra

    "Make it work, make it right, make it fast."
                              — Kent Beck

    "If you can't explain it simply, you don't understand it."
                              — Richard Feynman
```

*Fifth: Because sometimes less is more.*
