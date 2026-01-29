# Agentic Coder

A lightweight agentic coding assistant built with Fifth.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      AGENTIC CODER                          │
├─────────────────────────────────────────────────────────────┤
│  Planner          │  Executor         │  Memory             │
│  ────────         │  ────────         │  ──────             │
│  Task decompose   │  Tool dispatch    │  Context store      │
│  Priority queue   │  Result capture   │  Conversation log   │
│  Dependency DAG   │  Error handling   │  File cache         │
├─────────────────────────────────────────────────────────────┤
│                         TOOLS                               │
├──────────┬──────────┬──────────┬──────────┬────────────────┤
│ file-ops │  shell   │   llm    │   git    │    search      │
│ ──────── │  ─────   │   ───    │   ───    │    ──────      │
│ read     │  exec    │  query   │  status  │    grep        │
│ write    │  capture │  stream  │  diff    │    glob        │
│ edit     │  bg      │  embed   │  commit  │    ast         │
│ patch    │          │          │  branch  │                │
└──────────┴──────────┴──────────┴──────────┴────────────────┘
```

## Features

- **LLM Integration**: Shell to Claude/OpenAI APIs via curl
- **Tool Use**: File ops, shell commands, git, code search
- **Memory**: SQLite-backed context and conversation history
- **Planning**: Task decomposition and execution tracking
- **Streaming**: Process LLM output incrementally

## Usage

```bash
# Interactive mode
./fifth examples/agentic-coder/main.fs

# Single task
./fifth examples/agentic-coder/main.fs ask "Explain this function"

# With file context
./fifth examples/agentic-coder/main.fs context src/main.c ask "Find bugs"

# Execute a plan
./fifth examples/agentic-coder/main.fs plan "Add error handling to parser"
```

## Structure

```
agentic-coder/
├── main.fs              # Entry point, REPL
├── planner.fs           # Task planning
├── executor.fs          # Tool execution
├── context.fs           # Memory management
├── tools/
│   ├── file-ops.fs      # File read/write/edit
│   ├── shell.fs         # Command execution
│   ├── llm.fs           # LLM API calls
│   ├── git.fs           # Git operations
│   └── search.fs        # Code search
├── prompts/
│   ├── system.txt       # Base system prompt
│   ├── planner.txt      # Planning prompt
│   └── coder.txt        # Code generation prompt
├── memory.db            # Context storage
└── output/              # Generated files
```

## Configuration

Set environment variables:
```bash
export ANTHROPIC_API_KEY="sk-..."
export OPENAI_API_KEY="sk-..."
export AGENT_MODEL="claude-3-opus"  # or gpt-4, etc.
```

## Tool Protocol

Tools follow a simple pattern:
```forth
: tool-name ( inputs -- outputs )
  \ 1. Validate inputs
  \ 2. Execute operation
  \ 3. Return structured result ;
```

Tool results are JSON-ish for LLM consumption:
```
{"status": "success", "output": "...", "error": null}
```

## Memory Schema

```sql
-- Conversation history
CREATE TABLE messages (
  id INTEGER PRIMARY KEY,
  role TEXT,      -- user, assistant, system, tool
  content TEXT,
  timestamp TEXT
);

-- File context cache
CREATE TABLE file_cache (
  path TEXT PRIMARY KEY,
  content TEXT,
  hash TEXT,
  cached_at TEXT
);

-- Task queue
CREATE TABLE tasks (
  id INTEGER PRIMARY KEY,
  description TEXT,
  status TEXT,    -- pending, running, done, failed
  parent_id INTEGER,
  result TEXT
);
```

## Example Session

```
> ./fifth agentic-coder/main.fs

Agent> What would you like to do?

You> Add input validation to the parse_config function

Agent> I'll analyze the function and add validation. Let me:
       1. Read the file containing parse_config
       2. Understand the current implementation
       3. Identify input parameters that need validation
       4. Add appropriate checks

       [Reading src/config.c...]
       [Found parse_config at line 45...]
       [Generating validation code...]

       I've added validation for:
       - Null pointer check for config parameter
       - File existence check for path
       - JSON syntax validation

       Would you like me to show the diff?
```

## Anti-Patterns

- Don't use for long autonomous runs (no human oversight)
- Don't give access to production systems
- Don't store API keys in code
- Don't trust LLM output without validation
