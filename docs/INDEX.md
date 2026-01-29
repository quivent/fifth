---
layout: default
title: Fifth Wiki
---

![Fifth](https://raw.githubusercontent.com/quivent/fifth/main/brand/logo.svg)

> *"I think the industry is fundamentally unable to appreciate simplicity."*  
> — Chuck Moore, creator of Forth

Fifth is a self-contained Forth ecosystem designed for AI-assisted development. One binary, zero dependencies, instant startup.

## Quick Install

```bash
brew tap quivent/fifth
brew install fifth
```

Or [build from source](wiki/installation).

---

## Documentation

### Getting Started
- [Installation](wiki/installation) — Homebrew, source, manual
- [Quick Start](wiki/quickstart) — Hello world, REPL, first program
- [Examples](wiki/examples) — Real-world usage patterns

### Language Reference
- [Stack Operations](wiki/stack) — dup, drop, swap, over, rot
- [Arithmetic](wiki/arithmetic) — Math and logic
- [Words](wiki/words) — Defining and composing
- [Control Flow](wiki/control-flow) — Conditionals and loops
- [Memory](wiki/memory) — Fetch, store, buffers
- [Strings](wiki/strings) — The buffer pattern

### Libraries
- [str.fs](wiki/lib-str) — String buffers, parsing
- [html.fs](wiki/lib-html) — HTML generation, escaping
- [sql.fs](wiki/lib-sql) — SQLite interface
- [template.fs](wiki/lib-template) — Slot-based templates
- [ui.fs](wiki/lib-ui) — Dashboard components
- [pkg.fs](wiki/lib-pkg) — Package system

### Architecture
- [Interpreter](wiki/interpreter) — How the VM works
- [Compiler](wiki/compiler) — Native code via Cranelift
- [Metacompilation](wiki/metacompilation) — Forth building Forth

### For AI Agents
- [Why Forth for LLMs](wiki/agentic) — Explicit state, small vocabulary
- [Patterns](wiki/patterns) — Common idioms for code generation
- [Gotchas](wiki/gotchas) — Things that will break you

---

[GitHub](https://github.com/quivent/fifth) ·
[Homebrew](https://github.com/quivent/homebrew-fifth) ·
[Issues](https://github.com/quivent/fifth/issues)
