# Context Manager

Intelligent context window management for LLM-powered agents.

## The Problem

Large Language Models have finite context windows (4K to 200K tokens). During long-running agent sessions:

1. **Important information gets pushed out** - Early instructions, key decisions, and critical context disappear as new tokens arrive
2. **Redundant content wastes tokens** - Repeated information, verbose outputs, and irrelevant history consume precious space
3. **No prioritization** - Recent trivia crowds out older essential context
4. **Retrieval failures** - When context is lost, agents make inconsistent decisions or repeat mistakes

```
┌─────────────────────────────────────────────────────────────────┐
│                    CONTEXT WINDOW PROBLEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Token 0          Token N/2           Token N (current)         │
│  ───────          ────────            ─────────────────         │
│  [LOST]           [COMPRESSED]        [FULL DETAIL]             │
│                                                                 │
│  System prompt    Summarized          Current task              │
│  Key decisions    conversations       Tool outputs              │
│  Project rules    Old tasks           Recent messages           │
│                                                                 │
│  ↑                ↑                   ↑                         │
│  Critical but     Medium priority     High priority             │
│  often lost       degraded fidelity   full fidelity             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Strategies

### 1. Sliding Window

The simplest approach: keep the N most recent tokens.

**Pros**: Easy to implement, no summarization overhead
**Cons**: Important early context gets dropped entirely

```
Before: [System][Task1][Task2][Task3][Task4][Task5][Current]
After:  [System]                    [Task4][Task5][Current]
         ↑ pinned                    ↑ sliding window
```

### 2. Hierarchical Memory

Multiple memory levels with different retention policies:

```
┌──────────────────────────────────────────────────────────────┐
│ LEVEL 1: PERMANENT (always in context)                       │
│ ─────────────────────────────────────────────────────────────│
│ System prompt, project rules, critical constraints           │
│ Token budget: 2000 tokens                                    │
├──────────────────────────────────────────────────────────────┤
│ LEVEL 2: SESSION (compressed after 30 min)                   │
│ ─────────────────────────────────────────────────────────────│
│ Task summaries, key decisions, error resolutions             │
│ Token budget: 4000 tokens                                    │
├──────────────────────────────────────────────────────────────┤
│ LEVEL 3: WORKING (compressed after 5 min)                    │
│ ─────────────────────────────────────────────────────────────│
│ Recent conversation, tool outputs, current reasoning         │
│ Token budget: 8000 tokens                                    │
├──────────────────────────────────────────────────────────────┤
│ LEVEL 4: VOLATILE (dropped when space needed)                │
│ ─────────────────────────────────────────────────────────────│
│ Verbose tool output, exploration, dead ends                  │
│ Token budget: remaining space                                │
└──────────────────────────────────────────────────────────────┘
```

### 3. Summarization

Use an LLM to compress older context while preserving essential information:

```
Original (500 tokens):
  User asked to fix bug in parser.js line 45. I read the file,
  found the issue was a missing null check before accessing
  obj.property. I suggested adding: if (obj && obj.property).
  User approved. I made the edit. Tests passed.

Summarized (50 tokens):
  Fixed null check bug in parser.js:45. Added guard: if (obj && obj.property).
  Tests pass.
```

**When to summarize**:
- Context exceeds 80% of budget
- Content older than threshold (e.g., 10 minutes)
- Low priority score

### 4. Priority Scoring

Score content importance to decide what to keep:

```
Score = w1*Recency + w2*References + w3*TaskRelevance + w4*Explicit

Where:
  Recency       = 1 / (1 + minutes_since_created)
  References    = times_mentioned_later / total_mentions
  TaskRelevance = semantic_similarity(content, current_task)
  Explicit      = 1 if marked important, 0 otherwise

Weights: w1=0.3, w2=0.2, w3=0.4, w4=0.1
```

### 5. Retrieval-Augmented Context

Store full history in a database, retrieve relevant portions on demand:

```
┌────────────────────────────────────────────────────────────┐
│                    CONTEXT FLOW                             │
│                                                             │
│  Full History (SQLite)    Current Context Window            │
│  ─────────────────────    ──────────────────────            │
│                                                             │
│  ┌─────────────┐          ┌─────────────────────┐           │
│  │ All messages│  ──────> │ System prompt       │           │
│  │ All tasks   │  retrieve│ Retrieved summaries │           │
│  │ All outputs │  ──────> │ Recent conversation │           │
│  │ Summaries   │          │ Current task        │           │
│  └─────────────┘          └─────────────────────┘           │
│        ↑                          │                         │
│        │                          │                         │
│        └──────── store ───────────┘                         │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

## When to Compress vs. Retrieve

| Situation | Action |
|-----------|--------|
| Context > 80% full | Compress oldest working memory |
| Referencing old task | Retrieve summary from DB |
| Error repeating | Retrieve relevant error history |
| New task started | Compress previous task, clear volatile |
| User mentions past topic | Retrieve and expand that topic |

## Fifth Buffer System as Context Manager

Fifth's buffer system naturally maps to context management:

```
Fifth Buffer          Context Analogy
────────────          ──────────────────
str-buf (4KB)         Working context builder
str2-buf (4KB)        Escape/transform buffer
line-buf (512B)       Line-by-line processing
SQLite                Persistent full history
```

**Key insight**: Fifth's static allocation forces efficient memory use. No dynamic allocation means predictable memory footprint - exactly what you want for context management.

## Token Counting

Accurate token counting is essential. Options:

1. **Approximation**: `chars / 4` (rough, fast)
2. **tiktoken**: Shell out to Python `tiktoken` (accurate, slower)
3. **cl100k_base**: Claude/GPT-4 tokenizer via API

```bash
# Using tiktoken
echo "text to count" | python3 -c "
import tiktoken
enc = tiktoken.get_encoding('cl100k_base')
import sys
print(len(enc.encode(sys.stdin.read())))
"
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CONTEXT MANAGER                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Tracker    │  │  Summarizer  │  │  Retriever   │       │
│  │   ────────   │  │  ──────────  │  │  ─────────   │       │
│  │  Add entry   │  │  Compress    │  │  By recency  │       │
│  │  Score item  │  │  Merge       │  │  By relevance│       │
│  │  Prune old   │  │  LLM call    │  │  By keyword  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│         │                 │                 │               │
│         └─────────────────┼─────────────────┘               │
│                           │                                 │
│                    ┌──────┴──────┐                          │
│                    │   Builder   │                          │
│                    │   ───────   │                          │
│                    │  Assemble   │                          │
│                    │  context    │                          │
│                    │  for LLM    │                          │
│                    └─────────────┘                          │
│                           │                                 │
│         ┌─────────────────┼─────────────────┐               │
│         │                 │                 │               │
│  ┌──────┴──────┐  ┌───────┴───────┐  ┌──────┴──────┐       │
│  │  SQLite DB  │  │  Token Count  │  │   Metrics   │       │
│  │  ─────────  │  │  ───────────  │  │  ───────    │       │
│  │  messages   │  │  Accurate     │  │  Compression│       │
│  │  summaries  │  │  counting     │  │  ratio      │       │
│  │  history    │  │  per model    │  │  retrieval  │       │
│  └─────────────┘  └───────────────┘  └─────────────┘       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Usage

```bash
# Interactive mode
./fifth examples/context-manager/main.fs

# Add a message to context
./fifth examples/context-manager/main.fs add "user" "Fix the bug in auth.js"

# Show current context stats
./fifth examples/context-manager/main.fs stats

# Build optimized context for LLM call
./fifth examples/context-manager/main.fs build > context.txt

# Force compression
./fifth examples/context-manager/main.fs compress
```

## Database Schema

```sql
-- Raw conversation history
CREATE TABLE messages (
  id INTEGER PRIMARY KEY,
  role TEXT NOT NULL,           -- system, user, assistant, tool
  content TEXT NOT NULL,
  tokens INTEGER,
  priority INTEGER DEFAULT 50,  -- 0-100
  created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Compressed summaries
CREATE TABLE summaries (
  id INTEGER PRIMARY KEY,
  level INTEGER NOT NULL,       -- 1=permanent, 2=session, 3=working
  content TEXT NOT NULL,
  tokens INTEGER,
  source_ids TEXT,              -- JSON array of message IDs summarized
  created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Retrieval index
CREATE TABLE keywords (
  id INTEGER PRIMARY KEY,
  keyword TEXT NOT NULL,
  message_id INTEGER,
  summary_id INTEGER,
  FOREIGN KEY (message_id) REFERENCES messages(id),
  FOREIGN KEY (summary_id) REFERENCES summaries(id)
);

-- Compression metrics
CREATE TABLE metrics (
  id INTEGER PRIMARY KEY,
  original_tokens INTEGER,
  compressed_tokens INTEGER,
  compression_ratio REAL,
  timestamp TEXT DEFAULT CURRENT_TIMESTAMP
);
```

## Configuration

```forth
\ Token budgets per level
2000 constant level1-budget   \ Permanent
4000 constant level2-budget   \ Session
8000 constant level3-budget   \ Working
16000 constant total-budget   \ Total context window

\ Compression thresholds
300 constant age-threshold    \ Seconds before eligible for compression
80 constant capacity-trigger  \ Compress when context > 80% full

\ Summarization prompt
: summarization-prompt ( -- addr u )
  s" Summarize the following conversation, preserving: key decisions, action items, errors encountered, and current task state. Be concise." ;
```

## Metrics

Track compression effectiveness:

```
Context Manager Stats
─────────────────────
Total tokens used:     12,847 / 16,000 (80%)
Messages stored:       156
Summaries:             23
Compression ratio:     4.2x average

Level breakdown:
  Permanent:   1,850 / 2,000 tokens
  Session:     3,200 / 4,000 tokens
  Working:     6,500 / 8,000 tokens
  Volatile:    1,297 tokens

Last compression:  2 minutes ago
Retrieved items:   3 (task-42, error-17, decision-8)
```

## Best Practices

1. **Always pin system prompts** - They define agent behavior
2. **Summarize tasks on completion** - Don't keep verbose task details
3. **Track error resolutions** - Agents should learn from past mistakes
4. **Prune dead ends** - Failed explorations waste tokens
5. **Retrieve proactively** - When starting related work, pull relevant history
6. **Monitor compression ratio** - If < 2x, summaries may be too detailed

## Integration with Agentic Workflows

```
┌─────────────────────────────────────────────────────────────┐
│                    AGENT LOOP                               │
│                                                             │
│  1. Receive task                                            │
│     └─> Retrieve relevant context from DB                   │
│                                                             │
│  2. Build context                                           │
│     └─> Assemble: permanent + session + working + retrieved │
│                                                             │
│  3. Call LLM                                                │
│     └─> Pass optimized context                              │
│                                                             │
│  4. Execute tools                                           │
│     └─> Store outputs at appropriate level                  │
│                                                             │
│  5. Check capacity                                          │
│     └─> If > threshold, compress oldest working memory      │
│                                                             │
│  6. Task complete                                           │
│     └─> Summarize and promote to session level              │
│                                                             │
│  7. Loop                                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Limitations

- Token counting via shell is slow (consider batching)
- LLM summarization has latency and cost
- Semantic similarity requires embeddings (not implemented)
- Keyword extraction is naive (regex-based)

## Future Enhancements

- Vector embeddings for semantic retrieval
- Streaming compression during idle time
- Adaptive budgets based on task complexity
- Cross-session memory persistence
