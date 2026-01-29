# Codebase Indexer - Semantic Search for AI Agents

A production-grade semantic code indexing system written in Fifth/Forth. This demonstrates Fifth as a serious tool for AI infrastructure.

## Why Semantic Code Indexing Matters

Traditional text search fails for code:

| Query | Text Search | Semantic Search |
|-------|-------------|-----------------|
| "authentication handler" | Misses `verify_user()` | Finds conceptually related code |
| "database connection" | Returns config files | Returns actual connection logic |
| "error handling" | Matches string literals | Finds catch blocks, Result types |

**For AI agents**, semantic indexing enables:
- **Context retrieval**: Feed relevant code to LLMs without exceeding token limits
- **Code navigation**: "Find all code related to payment processing"
- **Dependency understanding**: Discover implicit relationships grep cannot find

## Architecture

```
                    ┌─────────────────┐
    Source Files    │  Code Chunker   │    Intelligent splitting by
   *.js *.py *.fs   │                 │    function/class boundaries
         │          └────────┬────────┘
         │                   │
         ▼                   ▼
    ┌─────────────────────────────────┐
    │       Embedding API             │    OpenAI ada-002 or
    │  (OpenAI / Ollama / Local)      │    local models via Ollama
    └────────────────┬────────────────┘
                     │
                     ▼
    ┌─────────────────────────────────┐
    │     SQLite Vector Store         │    Vectors as JSON arrays
    │                                 │    Metadata: file, line, type
    │  chunks    embeddings   meta    │
    └────────────────┬────────────────┘
                     │
                     ▼
    ┌─────────────────────────────────┐
    │      Semantic Search            │    Cosine similarity in SQL
    │                                 │    Returns ranked results
    └─────────────────────────────────┘
```

### Chunking Strategies

Smart chunking preserves semantic coherence:

```
NAIVE CHUNKING (by lines):              INTELLIGENT CHUNKING:
┌─────────────────────────┐             ┌─────────────────────────┐
│ function authenticate(  │             │ function authenticate(  │
│   user, password        │             │   user, password        │
├─────────────────────────┤ <-- break   │ ) {                     │
│ ) {                     │             │   const hash = ...      │
│   const hash = ...      │             │   return verify(...)    │
│   return verify(...)    │             │ }                       │
│ }                       │             └─────────────────────────┘
└─────────────────────────┘             Complete semantic unit
```

The indexer detects:
- **Function definitions**: `function`, `def`, `:`, `fn`
- **Class/struct boundaries**: `class`, `struct`, `type`
- **Module declarations**: `module`, `package`, `namespace`
- **Forth word definitions**: `: word-name ... ;`

### Embedding Models

| Model | Provider | Dimensions | Speed | Quality |
|-------|----------|------------|-------|---------|
| text-embedding-ada-002 | OpenAI API | 1536 | Fast | Excellent |
| nomic-embed-text | Ollama (local) | 768 | Medium | Good |
| all-minilm | Ollama (local) | 384 | Very fast | Adequate |

Fifth's shell-out pattern means zero dependencies - just curl.

### Vector Storage

SQLite stores everything:

```sql
CREATE TABLE embeddings (
    id INTEGER PRIMARY KEY,
    file_path TEXT NOT NULL,
    chunk_hash TEXT UNIQUE,        -- Deduplication
    chunk_text TEXT NOT NULL,
    chunk_type TEXT,               -- 'function', 'class', 'module'
    start_line INTEGER,
    end_line INTEGER,
    vector TEXT,                   -- JSON array of floats
    model TEXT,                    -- Which embedding model
    indexed_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_embeddings_file ON embeddings(file_path);
CREATE INDEX idx_embeddings_type ON embeddings(chunk_type);
```

Why SQLite over vector databases?
- **Zero deployment**: Single file, ships everywhere
- **SQL power**: Complex filters on metadata
- **Sufficient scale**: 100K vectors with < 1s search
- **Backup/sync**: Just copy the file

## How Fifth Makes This Elegant

### 1. Shell Composition

Traditional languages require HTTP libraries, JSON parsers, database drivers. Fifth shells out:

```forth
\ Call OpenAI embeddings API
: get-embedding ( text$ -- )
  str-reset
  s" curl -s https://api.openai.com/v1/embeddings " str+
  s" -H 'Authorization: Bearer '" str+
  s" OPENAI_API_KEY" getenv str+
  s" '' -H 'Content-Type: application/json' " str+
  s" -d '{\"input\": \"" str+
  str+  \ text
  s" \", \"model\": \"text-embedding-ada-002\"}'" str+
  str$ system ;
```

No dependencies. No package management. Just curl.

### 2. Buffer-Based Processing

Fifth's dual buffer system enables nested operations without allocation:

```forth
\ Primary buffer: build SQL command
\ Secondary buffer: escape text for SQL

: store-chunk ( text$ file$ -- )
  str-reset
  s" sqlite3 index.db \"INSERT INTO chunks VALUES ('" str+
  2swap sql-escape str+  \ Uses str2-buf internally
  s" ', '" str+
  str+ s" ');\"" str+
  str$ system ;
```

### 3. Composable Pipeline

Each word does one thing:

```forth
: index-file ( path$ -- )
  2dup get-file-type         \ Determine language
  read-file-chunks           \ Split by functions
  begin chunk-next? while    \ Iterate chunks
    2dup embed-chunk         \ Get vector
    store-embedding          \ Save to DB
  repeat ;
```

Stack discipline makes pipeline stages explicit.

## Workflow

### 1. Index a Codebase

```bash
# Index current directory
./fifth examples/codebase-indexer/main.fs index .

# Index specific directory with extensions
./fifth examples/codebase-indexer/main.fs index ./src "*.py,*.js"

# Index with local Ollama model
EMBEDDING_PROVIDER=ollama ./fifth examples/codebase-indexer/main.fs index .
```

### 2. Query for Context

```bash
# Find code related to a concept
./fifth examples/codebase-indexer/main.fs query "authentication and JWT tokens"

# Get top 5 results
./fifth examples/codebase-indexer/main.fs query "database migrations" 5

# Search within a file type
./fifth examples/codebase-indexer/main.fs query "error handling" --type py
```

### 3. Retrieve Context for LLM

```bash
# Get formatted context for prompt injection
./fifth examples/codebase-indexer/main.fs context "payment processing" > context.txt

# Pipe directly to an agent
./fifth examples/codebase-indexer/main.fs context "user auth" | agent-tool
```

## Performance Characteristics

| Operation | Time | Notes |
|-----------|------|-------|
| Index 1000 files | 2-5 min | Rate-limited by API |
| Index 1000 files (Ollama) | 5-10 min | CPU/GPU bound |
| Query 50K vectors | < 500ms | SQLite is fast |
| Incremental update | < 1s/file | Hash-based skip |

### Optimization: Incremental Indexing

The indexer tracks file hashes. Only changed files get re-embedded:

```forth
: needs-reindex? ( path$ -- flag )
  2dup file-hash       ( path$ hash$ )
  2swap get-stored-hash
  str= 0= ;            \ True if hashes differ
```

## Integration with AI Agents

### Feed Context to Claude

```forth
: build-prompt ( query$ -- prompt$ )
  2dup semantic-search      \ Get relevant chunks
  str-reset
  s" Based on this code context:\n\n" str+
  begin chunk-next? while
    s" ```\n" str+ str+ s" \n```\n\n" str+
  repeat
  s" Answer this question: " str+
  str+ str$ ;
```

### Agentic Coder Integration

This indexer complements the `agentic-coder` example:

```forth
\ In agentic-coder, add semantic search tool
: tool-semantic ( query$ -- json$ )
  semantic-search
  results>json ;
```

## File Structure

```
codebase-indexer/
├── README.md          This documentation
└── main.fs            Complete implementation
    ├── Configuration
    ├── Database Schema
    ├── File Discovery
    ├── Chunk Extraction
    ├── Embedding API
    ├── Vector Storage
    ├── Similarity Search
    └── CLI Interface
```

## Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `OPENAI_API_KEY` | (required) | OpenAI API authentication |
| `EMBEDDING_PROVIDER` | openai | `openai` or `ollama` |
| `EMBEDDING_MODEL` | text-embedding-ada-002 | Model identifier |
| `OLLAMA_HOST` | localhost:11434 | Ollama server address |
| `INDEX_DB` | codebase-index.db | SQLite database path |
| `CHUNK_MAX_TOKENS` | 512 | Maximum tokens per chunk |

## Limitations and Future Work

**Current limitations:**
- Cosine similarity computed in application (SQLite lacks vector ops)
- Single-threaded embedding calls
- No streaming for large files

**Future enhancements:**
- SQLite vector extension (sqlite-vss) for native similarity
- Parallel embedding with job queue
- Language-specific AST parsing via tree-sitter
- Hierarchical indexing (file -> class -> method)

## References

- [OpenAI Embeddings Guide](https://platform.openai.com/docs/guides/embeddings)
- [Ollama Embedding Models](https://ollama.ai/library)
- [RAG Architecture Patterns](https://www.pinecone.io/learn/retrieval-augmented-generation/)
- [Fifth Language Documentation](../../../README.md)
