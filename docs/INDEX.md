# Fifth Documentation

## Quick Navigation

| Document | Purpose |
|----------|---------|
| [../README.md](../README.md) | Quick start, architecture overview |
| [../CLAUDE.md](../CLAUDE.md) | Project constraints, critical gotchas |

## Core Documentation

| Document | Description |
|----------|-------------|
| [language-spec.md](language-spec.md) | Complete Fifth language specification |
| [forth-reference.md](forth-reference.md) | Forth language reference |
| [library-reference.md](library-reference.md) | Proposed new libraries |
| [contributing.md](contributing.md) | Development guide, debugging, testing |
| [roadmap.md](roadmap.md) | Vision, priorities, planned improvements |
| [agentic-coding.md](agentic-coding.md) | Why Forth is ideal for AI-assisted development |

## Subsystem Documentation

| Location | Description |
|----------|-------------|
| [../engine/ENGINE.md](../engine/ENGINE.md) | C interpreter architecture |
| [../compiler/docs/](../compiler/docs/) | Rust compiler documentation |

## Library Files

| File | Lines | Purpose |
|------|-------|---------|
| `lib/str.fs` | 147 | String buffers, parsing |
| `lib/html.fs` | 336 | HTML5 generation, escaping |
| `lib/sql.fs` | 152 | SQLite CLI interface |
| `lib/template.fs` | 123 | Slots, deferred rendering |
| `lib/ui.fs` | 261 | Dashboard components |
| `lib/core.fs` | 67 | Loader and utilities |

## Dependency Graph

```
str.fs  <--  html.fs  <--  template.fs  <--  ui.fs
  ^             ^
  |             |
sql.fs      core.fs (loads str + html + sql)
```

## Examples

| File | Description |
|------|-------------|
| `examples/db-viewer.fs` | Dual-database HTML viewer |
| `examples/project-dashboard.fs` | Tabbed dashboard with stat cards |

---

*Index generated from repository structure*
