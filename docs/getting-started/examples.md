---
title: Examples
parent: Getting Started
nav_order: 3
---

# Examples

## Generate HTML

```forth
require ~/.fifth/lib/pkg.fs
use lib:core.fs

s" /tmp/report.html" w/o create-file throw html>file
s" Report" html-head html-body
  s" Hello from Fifth" h1.
html-end
html-fid @ close-file throw
```

## Query SQLite

```forth
s" users.db" s" SELECT name FROM users" sql-exec
sql-open
begin sql-row? while
  dup 0> if 2dup 0 sql-field type cr 2drop else 2drop then
repeat 2drop
sql-close
```

## Dashboard

```forth
use lib:ui.fs

s" /tmp/dash.html" w/o create-file throw html>file
s" Dashboard" html-head ui-css html-body

grid-auto-begin
  42 s" Users" stat-card-n
  99 s" Uptime %" stat-card-n  
grid-end

html-end
html-fid @ close-file throw
```

See `examples/` in the repo for 30+ more.

---

## Why These Patterns Work

Each example demonstrates Fifth's composability — for humans and agents alike:

- **Small words**: `h1.`, `td.`, `stat-card-n` each do one thing
- **Stack discipline**: Every word's effect is documented and verifiable
- **Escape safety**: `text` escapes by default; security is the default, not an afterthought

For AI agents, these patterns are reliable because they're mechanically checkable. An LLM can verify that `42 s" Users" stat-card-n` has the right stack signature without executing it.
