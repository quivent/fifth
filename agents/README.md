# Fifth Agent Suite

> AI agents specialized for Fifth development - bridging modern AI capabilities with Forth-based programming.

```
    _____ _  __ _   _       _                    _
   |  ___(_)/ _| |_| |__   / \   __ _  ___ _ __ | |_ ___
   | |_  | | |_| __| '_ \ / _ \ / _` |/ _ \ '_ \| __/ __|
   |  _| | |  _| |_| | | / ___ \ (_| |  __/ | | | |_\__ \
   |_|   |_|_|  \__|_| |_/_/   \_\__, |\___|_| |_|\__|___/
                                 |___/
```

The Fifth Agent Suite provides a comprehensive set of AI agents, perspectives, and orchestrators designed specifically for Fifth (Forth) development. Each agent understands Fifth's unique constraints: static buffers, stack-based execution, shell-out patterns, and the discipline required to write correct Forth code.

---

## Table of Contents

- [Agent Taxonomy](#agent-taxonomy)
- [Tier Architecture](#tier-architecture)
- [Model Support](#model-support)
- [Usage](#usage)
- [Directory Structure](#directory-structure)
- [Database Schema](#database-schema)
- [Quick Reference](#quick-reference)

---

## Agent Taxonomy

```
                           +------------------+
                           |   ORCHESTRATORS  |
                           |     (Tier 1)     |
                           +--------+---------+
                                    |
         +------------+-------------+-------------+------------+
         |            |             |             |            |
    +----v----+  +----v----+  +-----v-----+  +---v----+  +----v----+
    |Conductor|  |  Critic |  |   Router  |  | Forge  |  |Archivist|
    +---------+  +---------+  +-----------+  +--------+  +---------+
         |            |             |             |            |
         +------------+------+------+-------------+------------+
                             |
              +--------------v--------------+
              |    CONTEXT ARCHITECTS       |
              |         (Tier 1)            |
              +----+-----+-----+-----+------+
                   |     |     |     |
              +----v--+ +v---+ +v----v+ +---v----+
              |Condens| |Lens| |Valid.| |Router  |
              +-------+ +----+ +------+ +--------+
                             |
         +-------------------v-------------------+
         |           PERSPECTIVES                |
         |             (Tier 2)                  |
         +----+--------+--------+--------+------+
              |        |        |        |
         +----v--+ +---v---+ +--v---+ +--v---+
         | Chuck | | Stack | |Buffer| |Shell |
         +-------+ +-------+ +------+ +------+
                             |
         +-------------------v-------------------+
         |           SPECIALISTS                 |
         |             (Tier 3)                  |
         +--+------+------+------+------+-------+
            |      |      |      |      |
        +---v-+ +--v--+ +-v--+ +-v---+ +v------+
        |Port.| |Scaf.| |Lib.| |Deb. | |Docum. |
        +-----+ +-----+ +----+ +-----+ +-------+
                             |
         +-------------------v-------------------+
         |          DOMAIN BRIDGES               |
         |             (Tier 5)                  |
         +--+------+------+------+------+-------+
            |      |      |      |      |
        +---v---+ +v----+ +v----+ +v---+ +v------+
        |DataSm.| |WebCr| |Query| |Sys.| |Autom. |
        +-------+ +-----+ +-----+ +----+ +-------+
```

---

## Tier Architecture

### Tier 1: Context Architects & Orchestrators

High-level agents that manage context, coordinate work, and make routing decisions.

| Agent | File | Purpose |
|-------|------|---------|
| **Condenser** | `orchestrators/condenser.md` | Compresses Fifth knowledge into optimal context for any model. Applies Shannon's information theory to minimize tokens while preserving correctness. |
| **Lens** | `orchestrators/lens.md` | Transforms prompts to inject "Fifth thinking" - stack consciousness, buffer awareness, constraint adherence. Makes any model temporarily think in Forth. |
| **Validator** | `orchestrators/validator.md` | Post-processes model output against Fifth constraints. Catches hallucinated words, buffer violations, forbidden patterns. |
| **Router** | `orchestrators/router.md` | Selects optimal model for each task based on complexity, cost, latency, and privacy requirements. Implements evidence-based escalation. |
| **Conductor** | `orchestrators/conductor.md` | Decomposes large projects into agent-sized tasks. Coordinates handoffs, tracks dependencies, manages parallel work. |
| **Critic** | `orchestrators/critic.md` | Multi-dimensional quality scoring for Fifth code. Evaluates correctness, style, efficiency, and constraint compliance. |
| **Forge** | `orchestrators/forge.md` | Meta-agent for creating new Fifth agents. Understands agent taxonomy, file formats, and ecosystem integration. |
| **Archivist** | `orchestrators/archivist.md` | Maintains the agent suite over time. Updates contexts, tracks performance, manages deprecation, keeps database in sync. |

### Tier 2: Perspectives

Alternative viewpoints that can be applied to any Fifth development task. These are not specialists - they are lenses through which to view code.

| Agent | File | Philosophy |
|-------|------|------------|
| **Chuck** | `perspectives/chuck.md` | Chuck Moore's radical simplicity. Every word, every abstraction is suspect until proven necessary. "If you can remove it and nothing breaks, it shouldn't have been there." |
| **Stack** | `perspectives/stack.md` | Pure stack-effect thinking. Every word is a transformation `( before -- after )`. Code is composition of transformations. "The stack is not a data structure. It is a proof system." |
| **Buffer** | `perspectives/buffer.md` | Memory guardian. Tracks ownership, lifetimes, collision points. "Memory does not forgive. Memory does not forget. Memory simply overwrites." |
| **Shell** | `perspectives/shell.md` | Unix pipeline thinking. Everything is a stream, a filter, a stage. "No bindings. Only pipes. The shell is not external to Fifth - it IS the integration layer." |

### Tier 3: Specialists

Deep expertise in specific Fifth domains.

| Agent | File | Specialty |
|-------|------|-----------|
| **Porter** | `specialists/porter.md` | Converts Python, JavaScript, Shell, and Ruby into idiomatic Fifth. Performs semantic translation, not syntax transformation. |
| **Scaffolder** | `specialists/scaffolder.md` | Creates new Fifth projects from natural language. Knows the examples directory, selects appropriate templates. |
| **Librarian** | `specialists/librarian.md` | Extends `~/.fifth/lib/` with new reusable words. Follows library conventions, manages dependency graph. |
| **Debugger** | `specialists/debugger.md` | Finds stack errors, buffer issues, and crashes. Traces stack effects, identifies imbalances, instruments code with `.s`. |
| **Documenter** | `specialists/documenter.md` | Writes stack comments, generates examples, creates tutorials and README files. Explains Fifth idioms to newcomers. |

### Tier 5: Domain Bridges

Connect Fifth to specific external domains with deep knowledge of both sides.

| Agent | File | Domain |
|-------|------|--------|
| **DataSmith** | `bridges/datasmith.md` | CSV/JSON processing, ETL pipelines, data transformation. Line-by-line streaming for large files. |
| **WebCraft** | `bridges/webcraft.md` | HTML generation, dashboards, reports. Knows the `html-head/html-body/html-end` sandwich pattern. |
| **QueryMind** | `bridges/querymind.md` | SQLite queries, result parsing, report generation. Shell-out to sqlite3 CLI with pipe-delimited results. |
| **SysOps** | `bridges/sysops.md` | Server health, monitoring, deployment, cron jobs. System administration via shell-out pattern. |
| **Automator** | `bridges/automator.md` | Bash script replacement, CI/CD, file operations. Structured Fifth programs instead of fragile shell scripts. |

---

## Model Support

The Fifth Agent Suite includes optimized contexts for multiple LLM providers and models.

### Context Files by Provider

```
contexts/
+-- anthropic/
|   +-- opus-4.5.ctx      # Full context (200K window)
|   +-- sonnet-4.ctx      # Balanced context
|   +-- haiku-3.5.ctx     # Compressed context
|
+-- google/
|   +-- gemini-1.5-pro.ctx
|   +-- gemini-flash.ctx
|   +-- gemini-ultra.ctx
|
+-- openai/
|   +-- gpt-4o.ctx
|   +-- o1.ctx            # Reasoning-optimized
|   +-- o3.ctx
|
+-- meta/
|   +-- llama-405b.ctx
|   +-- llama-70b.ctx
|
+-- mistral/
|   +-- mistral-large.ctx
|   +-- codestral.ctx     # Code-specialized
|
+-- deepseek/
|   +-- coder-v2.ctx      # Code generation focus
|
+-- cohere/
|   +-- command-r-plus.ctx
|
+-- xai/
|   +-- grok-2.ctx
|
+-- local/
    +-- universal.ctx     # Minimal, works everywhere
```

### Model Capability Matrix

| Provider | Model | Context | Strengths | Best For |
|----------|-------|---------|-----------|----------|
| **Anthropic** | Opus 4.5 | 200K | Complex reasoning, nuanced judgment | Architecture, debugging, novel problems |
| | Sonnet 4 | 200K | Balanced capability, fast | Code generation, routine tasks |
| | Haiku 3.5 | 200K | Very fast, efficient | Classification, formatting, simple tasks |
| **Google** | Gemini Ultra | 1M+ | Massive context | Large codebase analysis |
| | Gemini 1.5 Pro | 1M+ | Strong reasoning | Multi-file refactoring |
| | Gemini Flash | 1M+ | Fast, efficient | Quick iterations |
| **OpenAI** | o1/o3 | 128K | Deep reasoning, multi-step | Complex debugging, proofs |
| | GPT-4o | 128K | Fast, multimodal | General development |
| **Meta** | Llama 405B | 128K | Open weights, local | Privacy-sensitive code |
| | Llama 70B | 8K | Fast local | Quick local tasks |
| **Mistral** | Large | 32K | Strong reasoning | Code review |
| | Codestral | 32K | Code-specialized | Implementation |
| **DeepSeek** | Coder V2 | 64K | Code generation | Large implementations |
| **Cohere** | Command R+ | 128K | Enterprise features | Production pipelines |
| **xAI** | Grok 2 | 128K | Fast inference | Rapid prototyping |

### Token Budgets

| Model Tier | Safe Budget | Aggressive Budget |
|------------|-------------|-------------------|
| Large (Opus, o1) | 8K | 4K |
| Medium (Sonnet, GPT-4o) | 6K | 3K |
| Small (Haiku, Flash) | 4K | 2K |
| Local (Llama) | 2K | 1K |

---

## Usage

### Invoking Agents

```bash
# Run a specific agent
fifth agent porter python script.py

# Use with model-specific context
fifth agent scaffolder --model opus-4.5 "Build a metrics dashboard"

# Apply a perspective
fifth perspective stack review my-code.fs

# Run orchestration pipeline
fifth orchestrate conductor "Build a blog generator"
```

### Programmatic Usage

```forth
\ Load agent context programmatically
require ~/.fifth/agents/load.fs

\ Apply Chuck Moore perspective
chuck-perspective
  s" my-word.fs" load-source
  review-code
end-perspective

\ Route to optimal model
s" Fix this stack imbalance" route-task
\ Returns: model: opus-4.5, agent: debugger, confidence: 87%
```

### Orchestration Flow

```
User Task
    |
    v
+---+---+
|Condenser| --> Compress Fifth knowledge
+---+---+
    |
    v
+---+---+
|  Lens  | --> Transform prompt for Fifth thinking
+---+---+
    |
    v
+---+---+
| Router | --> Select optimal model
+---+---+
    |
    v
+---+---+
| Model  | --> Generate code
+---+---+
    |
    v
+---+---+
|Validator| --> Check constraints
+---+---+
    |
    +---> PASS: Return result
    |
    +---> FAIL: Adjust context, retry (max 3)
```

---

## Directory Structure

```
agents/
+-- README.md                 # This file
+-- fifth-agents.db           # SQLite database for agent metadata
|
+-- orchestrators/            # Tier 1: Coordination agents
|   +-- conductor.md
|   +-- critic.md
|   +-- condenser.md
|   +-- lens.md
|   +-- validator.md
|   +-- router.md
|   +-- forge.md
|   +-- archivist.md
|
+-- perspectives/             # Tier 2: Viewpoint agents
|   +-- chuck.md
|   +-- stack.md
|   +-- buffer.md
|   +-- shell.md
|
+-- specialists/              # Tier 3: Domain experts
|   +-- porter.md
|   +-- scaffolder.md
|   +-- librarian.md
|   +-- debugger.md
|   +-- documenter.md
|
+-- bridges/                  # Tier 5: External domain bridges
|   +-- datasmith.md
|   +-- webcraft.md
|   +-- querymind.md
|   +-- sysops.md
|   +-- automator.md
|
+-- contexts/                 # Model-specific context files
|   +-- anthropic/
|   +-- google/
|   +-- openai/
|   +-- meta/
|   +-- mistral/
|   +-- deepseek/
|   +-- cohere/
|   +-- xai/
|   +-- local/
|
+-- adapters/                 # Format adapters (future)
```

---

## Database Schema

The `fifth-agents.db` SQLite database tracks agent metadata, contexts, and routing rules.

### Tables

```sql
-- Agent registry
CREATE TABLE agents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,      -- e.g., "conductor", "porter"
    tier TEXT NOT NULL,              -- e.g., "orchestrator", "specialist"
    category TEXT NOT NULL,          -- e.g., "core", "web", "data"
    purpose TEXT NOT NULL,           -- One-sentence description
    personality TEXT,                -- Voice/style description
    constraints TEXT,                -- JSON array of enforced constraints
    model_affinity TEXT,             -- Preferred models for this agent
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Model context configurations
CREATE TABLE contexts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    agent_id INTEGER NOT NULL,
    model_provider TEXT NOT NULL,    -- e.g., "anthropic", "openai"
    model_name TEXT NOT NULL,        -- e.g., "opus-4.5", "gpt-4o"
    context_strategy TEXT,           -- "full", "summarized", "retrieval"
    token_budget INTEGER,            -- Maximum tokens for context
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (agent_id) REFERENCES agents(id)
);

-- Task routing rules
CREATE TABLE routing_rules (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_pattern TEXT NOT NULL,      -- Regex for matching task types
    primary_model TEXT NOT NULL,     -- First choice model
    fallback_model TEXT,             -- If primary fails
    reasoning TEXT                   -- Why this routing decision
);
```

### Query Examples

```sql
-- Find all specialist agents
SELECT name, purpose FROM agents WHERE tier = 'specialist';

-- Get context budget for a model
SELECT token_budget FROM contexts
WHERE model_provider = 'anthropic' AND model_name = 'opus-4.5';

-- Find routing for debugging tasks
SELECT * FROM routing_rules WHERE task_pattern LIKE '%debug%';
```

---

## Quick Reference

### Constraint Checklist (All Agents Enforce)

- [ ] No `allocate`/`free` (use static buffers)
- [ ] No `s+` (causes memory corruption)
- [ ] Stack comments on every word `( before -- after )`
- [ ] Whitespace between all Forth words
- [ ] Use `require` not `include`
- [ ] Use `text` for user data (escapes HTML)
- [ ] Use `s\"` for embedded quotes
- [ ] No single-quoted SQL literals in shell commands

### Common Stack Effects

```forth
dup     ( n -- n n )           2dup    ( a b -- a b a b )
drop    ( n -- )               2drop   ( a b -- )
swap    ( a b -- b a )         2swap   ( a b c d -- c d a b )
over    ( a b -- a b a )       2over   ( a b c d -- a b c d a b )
rot     ( a b c -- b c a )     2>r     ( a b -- ) R:( a b )
-rot    ( a b c -- c a b )     2r>     R:( a b -- a b )
```

### Buffer Operations

```forth
\ Primary buffer
str-reset                      \ Clear buffer
str+      ( addr u -- )        \ Append string
str$      ( -- addr u )        \ Get buffer contents
str-char  ( char -- )          \ Append character

\ Secondary buffer (for escaping)
str2-reset str2+ str2$         \ Same operations, separate buffer
```

### SQL Pattern

```forth
s" db.db" s" SELECT a, b FROM t" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    2dup 0 sql-field type      \ First column
    2dup 1 sql-field type      \ Second column
    2drop                       \ Drop row string
  else 2drop then
repeat 2drop
sql-close
```

### HTML Pattern

```forth
s" /tmp/out.html" w/o create-file throw html>file
s" Title" html-head
  <style> ... </style>         \ Inject while head open
html-body
  s" Hello" h1.
  s" class" <div.> ... </div> nl
html-end
html-fid @ close-file throw
```

---

## Contributing

To add a new agent:

1. Determine the appropriate tier (orchestrator, perspective, specialist, bridge)
2. Create a markdown file following the existing format
3. Register in `fifth-agents.db`:
   ```sql
   INSERT INTO agents (name, tier, category, purpose, model_affinity)
   VALUES ('my-agent', 'specialist', 'domain', 'Purpose here', 'sonnet');
   ```
4. Add context configurations for relevant models
5. Update this README

---

## License

The Fifth Agent Suite is part of the Fifth project. See the main repository for license information.

---

*"No component commits to a single answer. All produce features and associated confidence scores."*
*- Dave Ferrucci, DeepQA Methodology*
