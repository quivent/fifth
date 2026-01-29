# Fifth Use Cases

Fifth's design—static buffers, shell-out integration, HTML generation, and stack-based composition—makes it particularly suited for certain kinds of applications.

---

## Web & Report Generation

### Static Site Generator

Generate a blog or documentation site from markdown files:

```forth
\ Scan posts/ directory, generate HTML pages with navigation
\ Shell out to pandoc for markdown conversion
\ Output to dist/ with consistent styling
```

**Why Fifth fits:** HTML DSL makes templating natural. No runtime dependencies in output. Fast regeneration.

### Dashboard Generator

Pull metrics from multiple sources, render a single-page dashboard:

```forth
\ Query SQLite for historical data
\ Shell to curl for API endpoints
\ Generate self-contained HTML with embedded charts (via CDN)
```

**Why Fifth fits:** The `sql.fs` + `html.fs` combination handles the full pipeline. Static output works anywhere.

### Invoice/Report System

Generate PDF invoices from database records:

```forth
\ Query orders from SQLite
\ Generate HTML with print-optimized CSS
\ Shell to wkhtmltopdf or weasyprint for PDF
```

**Why Fifth fits:** Stack-based field extraction from SQL results. HTML escaping prevents injection.

---

## Data Processing

### Log Analyzer

Parse and summarize application logs:

```forth
\ Read log files line by line
\ Pattern match with Forth string words
\ Aggregate into SQLite for querying
\ Output HTML report with charts
```

**Why Fifth fits:** Line-at-a-time processing fits static buffers. SQL aggregation handles the heavy lifting.

### CSV Transformer

Convert between data formats or enrich CSV data:

```forth
\ Parse CSV (pipe-delimited internally)
\ Transform fields with stack operations
\ Join with SQLite lookup tables
\ Output new format
```

**Why Fifth fits:** Stack operations natural for field manipulation. `sql-field` already handles delimited data.

### Database Migration Tool

Schema versioning and data migration:

```forth
\ Track applied migrations in metadata table
\ Execute SQL files in order
\ Generate rollback scripts
\ Output migration report
```

**Why Fifth fits:** Shell-out to `sqlite3` handles arbitrary SQL. Forth control flow manages sequencing.

---

## System Administration

### Configuration Generator

Generate nginx/Apache/systemd configs from templates:

```forth
\ Define server blocks as Forth words
\ Compose configurations from components
\ Output to appropriate system paths
```

**Why Fifth fits:** Word composition maps to config block composition. No templating engine needed.

### Server Health Dashboard

Aggregate system metrics into a status page:

```forth
\ Shell to df, free, uptime, etc.
\ Parse output, store in SQLite
\ Generate HTML dashboard
\ Optionally shell to send alerts
```

**Why Fifth fits:** Shell-out gives access to all system tools. Static HTML output needs no server.

### Deployment Script

Orchestrate multi-step deployments:

```forth
\ Check prerequisites
\ Run build commands
\ Copy files to targets
\ Run health checks
\ Rollback on failure
```

**Why Fifth fits:** Stack-based control flow handles conditionals. Shell-out executes actual commands.

---

## Developer Tools

### Code Generator

Generate boilerplate from specifications:

```forth
\ Read schema (JSON/YAML via jq/yq)
\ Generate models, routes, tests
\ Output to appropriate directories
```

**Why Fifth fits:** Text generation is natural. Stack holds context during nested generation.

### Documentation Generator

Extract and format documentation from source:

```forth
\ Parse source files for doc comments
\ Cross-reference definitions
\ Generate HTML or markdown
\ Build searchable index
```

**Why Fifth fits:** Line-by-line parsing fits well. HTML generation built in.

### Project Scaffolder

Create new project structures from templates:

```forth
\ Prompt for project parameters
\ Generate directory structure
\ Render template files with substitutions
\ Initialize git, install dependencies
```

**Why Fifth fits:** Shell-out handles git/npm/etc. Template system manages substitutions.

### CSS Framework Generator

Generate modular CSS with design tokens and utilities:

```forth
\ Define color palettes as data
\ Generate CSS custom properties
\ Emit utility classes programmatically
\ Build component styles from primitives
```

**Why Fifth fits:** Composable generation maps to composable CSS. No templating engine overhead.

---

## Domain-Specific Applications

### Financial Calculator

RPN calculators and financial modeling:

```forth
\ Compound interest, amortization schedules
\ Stack naturally holds intermediate values
\ Generate reports as HTML tables
```

**Why Fifth fits:** Stack-based calculation is RPN. No floating-point footguns with integer cents.

### Quiz/Survey System

Generate and score assessments:

```forth
\ Define questions as data
\ Render HTML forms
\ Score submissions
\ Store results in SQLite
\ Generate reports
```

**Why Fifth fits:** Static HTML forms work offline. SQL stores responses.

### Recipe/Formula Manager

Store and scale recipes or chemical formulas:

```forth
\ Define ingredients with ratios
\ Scale by desired output
\ Generate shopping lists
\ Track inventory in SQLite
```

**Why Fifth fits:** Stack operations handle ratio calculations. SQL tracks state.

### Bookmark/Note Manager

Personal knowledge management:

```forth
\ Store links/notes in SQLite
\ Full-text search via FTS5
\ Generate browsable HTML export
\ Tag-based organization
```

**Why Fifth fits:** SQLite FTS5 handles search. HTML export is portable.

---

## Embedded & Constrained

### IoT Device Scripting

Lightweight automation on resource-constrained devices:

```forth
\ Read sensors via shell commands
\ Apply thresholds and rules
\ Trigger actuators
\ Log to local SQLite
```

**Why Fifth fits:** Small footprint. No dynamic allocation. Works on minimal systems.

### Kiosk/Display System

Drive information displays:

```forth
\ Query data sources
\ Generate full-screen HTML
\ Refresh on schedule
\ Shell to browser in kiosk mode
```

**Why Fifth fits:** Self-contained HTML. No runtime server needed.

---

## Integration Patterns

### Webhook Handler

Process incoming webhooks:

```forth
\ Parse JSON payload (via jq)
\ Validate and transform
\ Store in SQLite
\ Trigger downstream actions
```

### Cron Job Orchestrator

Manage scheduled tasks:

```forth
\ Define jobs as Forth words
\ Track execution in SQLite
\ Handle dependencies
\ Generate status reports
```

### API Client

Wrap REST APIs in Forth words:

```forth
\ Shell to curl for requests
\ Parse JSON responses with jq
\ Provide high-level Forth interface
\ Cache responses in SQLite
```

---

---

## Agentic Coding

### AI Coding Assistant

Build an agentic coding assistant that uses LLM APIs:

```forth
\ Shell to Claude/GPT APIs via curl
\ Execute tool calls (read, write, search, git)
\ Manage context and conversation memory
\ Plan and decompose complex tasks
```

**Why Fifth fits:** Shell-out to LLM APIs, SQLite for memory, tool dispatch via word composition. Lightweight enough to be the "glue" layer between LLM and system.

Key components:
- **Tools**: File ops, shell commands, git, code search
- **Memory**: SQLite-backed context and conversation history
- **Planner**: Task decomposition and execution tracking
- **LLM Interface**: API calls with tool use protocol

---

## Anti-Patterns (What Fifth is NOT For)

- **Long-running servers** — No event loop, no concurrency
- **Real-time systems** — Shell-out latency too high
- **GUI applications** — No graphics primitives
- **Heavy computation** — Interpreted performance, no FFI
- **Large string manipulation** — Static buffers have fixed size

---

## Getting Started

Each use case follows the same pattern:

1. **Data in**: Shell commands, SQLite queries, file reads
2. **Transform**: Stack operations, Forth words
3. **Data out**: HTML generation, file writes, shell commands

Start with the existing examples:
- `db-viewer.fs` — SQLite + HTML pattern
- `project-dashboard.fs` — Multi-source aggregation

Build incrementally. Test each word in the REPL. Compose into larger systems.
