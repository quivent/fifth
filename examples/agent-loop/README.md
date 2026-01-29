# Agent Loop Framework for Fifth

A production-grade autonomous agent implementation in Fifth (Forth), demonstrating how the stack machine paradigm naturally models the ReAct (Reason + Act) pattern used in modern AI agents.

## Architecture Overview

```
                    +-----------------+
                    |   User Task     |
                    +--------+--------+
                             |
                             v
                    +--------+--------+
                    |   THINK Phase   |  <-- LLM reasons about state
                    |  (call-llm)     |
                    +--------+--------+
                             |
                             v
                    +--------+--------+
                    |  PARSE Response |  <-- Extract tool call or final answer
                    |  (parse-action) |
                    +--------+--------+
                             |
              +--------------+--------------+
              |                             |
              v                             v
     +--------+--------+           +--------+--------+
     |   TOOL CALL     |           |   FINAL ANSWER  |
     | (dispatch-tool) |           |  (emit-result)  |
     +--------+--------+           +-----------------+
              |
              v
     +--------+--------+
     |   OBSERVE       |  <-- Capture tool output
     | (tool result)   |
     +--------+--------+
              |
              v
     +--------+--------+
     |  UPDATE STATE   |  <-- Add to history, check limits
     | (history-add)   |
     +--------+--------+
              |
              +-----------> Loop back to THINK
```

## The ReAct Pattern

ReAct (Reasoning + Acting) is the dominant pattern for tool-using LLM agents:

1. **Thought**: The model reasons about the current state and what action to take
2. **Action**: The model emits a structured tool call
3. **Observation**: The tool executes and returns results
4. **Repeat**: The observation feeds back into the next thought

This maps elegantly to Forth's execution model where the stack carries state between words.

## Why Fifth/Forth for Agents?

### Stack as Agent State

The Forth stack naturally represents agent state flow:

```forth
\ Each phase consumes input and produces output on the stack
task$           ( -- task-addr task-u )
history$        ( task$ -- task$ history$ )
build-prompt    ( task$ history$ -- prompt$ )
call-llm        ( prompt$ -- response$ )
parse-action    ( response$ -- action-type tool$ args$ | action-type result$ )
dispatch-tool   ( tool$ args$ -- result$ )
history-add     ( result$ -- )
```

### Explicit Control Flow

No hidden async/await magic. The loop is visible:

```forth
: agent-loop ( task$ max-iters -- result$ )
  0 do
    think
    act dup ACTION-DONE = if leave then
    observe
  loop
  final-result ;
```

### Composition Over Inheritance

Small, focused words compose into complex behavior:

```forth
: think ( -- response$ )        build-prompt call-llm ;
: act   ( response$ -- type )   parse-action dispatch-or-finish ;
: observe ( result$ -- )        history-add check-limits ;
```

## Tool System

### Tool Registry

Tools are stored in a simple lookup table with metadata:

```forth
\ Tool structure: name, description, handler-xt
: tool: ( name$ desc$ xt -- )
  tools-count @ tools-max < if
    tool-entry!
    1 tools-count +!
  else
    2drop 2drop drop
    ." Error: Tool registry full" cr
  then ;

\ Register tools
s" shell" s" Execute shell command" ' tool-shell tool:
s" read"  s" Read file contents"    ' tool-read  tool:
s" write" s" Write to file"         ' tool-write tool:
s" grep"  s" Search files"          ' tool-grep  tool:
```

### Tool Protocol

Each tool follows a uniform interface:

```forth
\ Input:  ( args-addr args-u -- result-addr result-u success? )
\ Output: JSON-formatted result string + success flag

: tool-shell ( args$ -- result$ success? )
  \ Parse command from args
  \ Execute via system
  \ Capture output
  \ Format as JSON result
  ;
```

### Tool Dispatch

```forth
: dispatch-tool ( tool$ args$ -- result$ success? )
  2>r  \ save args
  tools-lookup if
    2r> swap execute  \ call handler with args
  else
    2r> 2drop
    s" {\"error\": \"unknown_tool\"}" false
  then ;
```

## Memory Management

### Conversation History

History is stored in a ring buffer of fixed-size entries:

```forth
\ History entry structure
\ - role:    8 bytes (padded)
\ - content: 1024 bytes max
\ - timestamp: 8 bytes

256 constant history-max       \ Max entries
1040 constant history-entry-size
create history-buf history-max history-entry-size * allot
variable history-head          \ Write position (circular)
variable history-count         \ Current count

: history-add ( role$ content$ -- )
  history-entry-alloc
  dup >r
  2swap history-entry-role!
  r> 2swap history-entry-content!
  history-count @ history-max < if
    1 history-count +!
  then
  history-head @ 1+ history-max mod history-head ! ;
```

### Context Window Management

To fit within LLM context limits, we summarize old history:

```forth
: context-tokens ( -- n )
  \ Estimate token count of current context
  history-count @ 0 ?do
    i history-entry-content@ nip
    4 /  \ rough estimate: 4 chars per token
    +
  loop ;

: maybe-summarize ( -- )
  context-tokens max-context-tokens > if
    summarize-history
  then ;

: summarize-history ( -- )
  \ Take oldest N entries, ask LLM to summarize
  \ Replace with single summary entry
  history-oldest-n 10
  s" Summarize these conversation turns:" build-summary-prompt
  call-llm
  history-replace-old-with-summary ;
```

## Self-Correction and Error Handling

### Retry with Exponential Backoff

```forth
variable retry-count
variable retry-delay-ms

: reset-retries ( -- ) 0 retry-count ! 1000 retry-delay-ms ! ;
: next-retry ( -- delay )
  retry-delay-ms @ dup 2* 30000 min retry-delay-ms !
  1 retry-count +! ;

: with-retry ( xt max-retries -- result success? )
  reset-retries
  >r
  begin
    dup execute if
      r> drop true exit
    then
    retry-count @ r@ >= if
      r> drop false exit
    then
    next-retry sleep
  again
  r> drop false ;
```

### Error Recovery

When a tool fails, the agent can reason about the failure:

```forth
: handle-tool-error ( error$ tool$ -- )
  2swap 2>r
  s" tool_error" 2r>
  str-reset
  s" Tool '" str+ str+ s" ' failed: " str+ str+ str$
  history-add

  \ Next LLM call will see the error and can try alternative approaches
  ;

: dispatch-with-recovery ( tool$ args$ -- result$ success? )
  2over 2>r  \ save tool name for error message
  dispatch-tool
  dup 0= if
    2r> handle-tool-error
    s" " false
  else
    2r> 2drop
  then ;
```

### Stuck Detection

Detect when agent is looping without progress:

```forth
variable last-action-hash
variable same-action-count

: action-hash ( tool$ args$ -- hash )
  \ Simple hash of action
  + swap + ;

: check-stuck ( tool$ args$ -- stuck? )
  action-hash
  dup last-action-hash @ = if
    1 same-action-count +!
    same-action-count @ 3 > if
      drop true exit
    then
  else
    last-action-hash !
    0 same-action-count !
  then
  false ;

: handle-stuck ( -- )
  s" system"
  s" You seem to be repeating the same action. Try a different approach."
  history-add ;
```

## JSON Parsing

Since Fifth shells out, we use `jq` for JSON parsing:

```forth
: jq ( json$ filter$ -- result$ success? )
  str-reset
  s" echo '" str+ 2swap str+ s" ' | jq -r '" str+ str+ s" '" str+
  str$ capture-output ;

: parse-tool-call ( response$ -- tool$ args$ | 0 0 )
  2dup s" .tool // empty" jq if
    2swap s" .args // {}" jq if
      exit
    then
    2drop
  then
  2drop 0 0 ;

: parse-result ( response$ -- result$ is-final? )
  2dup s" .done // false" jq if
    s" true" str= if
      s" .result // \"\"" jq drop
      true exit
    then
  then
  2drop s" " false ;
```

## Configuration

### Environment

```forth
\ API Configuration
: api-key$ ( -- addr u ) s" ANTHROPIC_API_KEY" getenv ;
: model$   ( -- addr u ) s" claude-sonnet-4-20250514" ;
: max-tokens ( -- n )    4096 ;

\ Agent Limits
: max-iterations ( -- n ) 25 ;
: max-context-tokens ( -- n ) 100000 ;
: tool-timeout-ms ( -- n ) 30000 ;

\ Retry Configuration
: max-retries ( -- n ) 3 ;
: initial-retry-ms ( -- n ) 1000 ;
: max-retry-ms ( -- n ) 30000 ;
```

### System Prompt

```forth
: system-prompt$ ( -- addr u )
  s\" You are an autonomous coding agent. You have access to tools.\n\n\
TOOLS:\n\
- shell: Execute shell commands. Args: {\"cmd\": \"...\"}\n\
- read: Read file contents. Args: {\"path\": \"...\"}\n\
- write: Write to file. Args: {\"path\": \"...\", \"content\": \"...\"}\n\
- grep: Search files. Args: {\"pattern\": \"...\", \"path\": \"...\"}\n\n\
RESPONSE FORMAT:\n\
To use a tool: {\"thought\": \"...\", \"tool\": \"name\", \"args\": {...}}\n\
When done: {\"thought\": \"...\", \"done\": true, \"result\": \"...\"}\n\n\
Always include your reasoning in the thought field.\n\
Execute one tool at a time and observe the result before proceeding.\" ;
```

## Usage

The agent uses environment variables for configuration:

```bash
# List available tools
AGENT_MODE=tools ./fifth examples/agent-loop/main.fs

# Dry run (show request without API call)
AGENT_MODE=dry AGENT_TASK="Your task" ./fifth examples/agent-loop/main.fs

# Run agent (requires ANTHROPIC_API_KEY)
AGENT_TASK="Your task" ./fifth examples/agent-loop/main.fs
```

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `AGENT_TASK` | For run mode | The task for the agent to complete |
| `AGENT_MODE` | No | `run` (default), `dry`, or `tools` |
| `ANTHROPIC_API_KEY` | For run mode | Anthropic API key |

## Usage Example

### Find and Fix a Bug

```bash
AGENT_TASK="Find the bug in src/parser.c that causes segfault on empty input and fix it" \
  ./fifth examples/agent-loop/main.fs
```

The agent will:

1. **Think**: "I need to understand the parser code first"
2. **Act**: `{"tool": "read", "args": {"path": "src/parser.c"}}`
3. **Observe**: [file contents]
4. **Think**: "I see the parse() function doesn't check for NULL. Let me find where it's called..."
5. **Act**: `{"tool": "grep", "args": {"pattern": "parse\\(", "path": "src/"}}`
6. **Observe**: [grep results]
7. **Think**: "The bug is on line 42 - no null check. I'll add one."
8. **Act**: `{"tool": "write", "args": {"path": "src/parser.c", "content": "..."}}`
9. **Observe**: [success]
10. **Think**: "Let me verify the fix works"
11. **Act**: `{"tool": "shell", "args": {"cmd": "make test"}}`
12. **Observe**: [tests pass]
13. **Done**: `{"done": true, "result": "Fixed null pointer bug in parse() on line 42"}`

### Analyze Codebase

```bash
./fifth examples/agent-loop/main.fs run "What design patterns are used in this codebase?"
```

### Execute Multi-Step Task

```bash
./fifth examples/agent-loop/main.fs run "Create a new feature that adds user authentication. Start with the data model, then API endpoints, then tests."
```

## CLI Commands

```bash
# Run agent with task
./fifth examples/agent-loop/main.fs run "Your task here"

# Run with custom max iterations
./fifth examples/agent-loop/main.fs run --max-iter 50 "Complex task"

# Show available tools
./fifth examples/agent-loop/main.fs tools

# Show last session history
./fifth examples/agent-loop/main.fs history

# Clear history
./fifth examples/agent-loop/main.fs clear

# Dry run (show prompts without API calls)
./fifth examples/agent-loop/main.fs dry "Test task"
```

## Extending the Framework

### Adding a New Tool

```forth
\ 1. Define the tool handler
: tool-web-search ( args$ -- result$ success? )
  \ Parse query from args
  s" .query" jq drop
  \ Build curl command for search API
  str-reset
  s" curl -s 'https://api.duckduckgo.com/?q=" str+
  str+ s" &format=json'" str+
  str$ capture-output ;

\ 2. Register the tool
s" search" s" Search the web for information" ' tool-web-search tool:
```

### Custom Stop Conditions

```forth
\ Stop when tests pass
: tests-pass? ( result$ -- flag )
  s" PASSED" str-contains? ;

: custom-stop ( result$ -- stop? )
  2dup tests-pass? if
    2drop true exit
  then
  \ Also check for done signal
  parse-result nip ;
```

### Persistent Memory

```forth
\ Store agent memory in SQLite
: memory-init ( -- )
  s" sqlite3 agent.db \"CREATE TABLE IF NOT EXISTS memory (key TEXT PRIMARY KEY, value TEXT)\""
  system drop ;

: memory-set ( key$ value$ -- )
  str-reset
  s" sqlite3 agent.db \"INSERT OR REPLACE INTO memory VALUES ('" str+
  2swap str+ s" ', '" str+ str+ s" ')\"" str+
  str$ system drop ;

: memory-get ( key$ -- value$ found? )
  str-reset
  s" sqlite3 agent.db \"SELECT value FROM memory WHERE key='" str+
  str+ s" '\"" str+
  str$ capture-output ;
```

## Information-Theoretic Perspective

*From Claude Shannon's lens on agent design:*

### Entropy in Agent Decisions

At each step, the agent reduces uncertainty about the solution. We can measure this:

```
H(Solution | Initial Task) >> H(Solution | Task + Observations_1..n)
```

Each tool call should maximize information gain - choosing actions that most reduce uncertainty about the goal state.

### Channel Capacity Constraints

The LLM API is a noisy channel with finite capacity:
- **Input tokens**: Limited context window
- **Output tokens**: Limited response length
- **Latency**: API round-trip time

The agent framework manages this by:
1. Summarizing history to fit context
2. Chunking large file reads
3. Caching to avoid redundant calls

### Redundancy for Reliability

Tool results are inherently noisy. The framework adds redundancy:
- Retry with backoff on failures
- Verification steps after writes
- Alternative tool paths when stuck

## Design Principles

### From Bjarne Stroustrup's Perspective

1. **Zero-overhead abstraction**: The framework adds no hidden costs. Each word does exactly what it says.

2. **Resource management via RAII pattern**: File handles, buffers, and connections are acquired and released in matched pairs.

3. **Type safety through stack discipline**: While Forth is untyped, consistent stack comments enforce a "type contract" at each word boundary.

4. **Explicit over implicit**: No magic. The agent loop is visible, debuggable, modifiable.

### Forth Philosophy

1. **Factor mercilessly**: No word exceeds 10 lines. Complex behavior emerges from simple compositions.

2. **Stack is state**: No hidden globals. State flows through the stack.

3. **Words are tools**: Each word is a small, sharp tool. Combine them for power.

## Debugging

### Trace Mode

```forth
variable trace-on

: trace ( str$ -- )
  trace-on @ if
    ." [TRACE] " type cr
  else
    2drop
  then ;

: traced-dispatch ( tool$ args$ -- result$ success? )
  2over 2over
  s" Dispatching: " trace type s"  with " trace type cr
  dispatch-tool
  dup if s" Success" trace else s" Failed" trace then ;
```

### Stack Inspection

```forth
: show-stack ( -- )
  ." Stack: " .s cr ;

: step-through ( task$ -- )
  ." Starting agent with task: " 2dup type cr
  begin
    show-stack
    s" Press Enter to continue..." type key drop
    think
    show-stack
    act dup ACTION-DONE =
  until
  show-stack
  final-result type cr ;
```

## Limitations

1. **No parallelism**: Forth is single-threaded. Tools run sequentially.
2. **Buffer sizes**: Fixed buffers limit response sizes. Adjust constants for larger outputs.
3. **No streaming**: LLM responses are received all at once, not streamed.
4. **Shell injection risk**: Tool arguments must be sanitized before shell execution.

## Files

```
examples/agent-loop/
  README.md     - This documentation
  main.fs       - Main implementation
```

## License

MIT - Part of the Fifth project.
