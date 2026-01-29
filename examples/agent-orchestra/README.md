# Agent Orchestra Example

Demonstrates multi-agent collaboration in Fifth, simulating how specialized agents work together to accomplish a code translation task.

## What This Example Demonstrates

This example shows the orchestration of three specialized agents:

1. **Conductor** - The task coordinator
   - Receives the high-level task
   - Decomposes it into subtasks
   - Delegates to specialized agents

2. **Porter** - The code translator
   - Takes Python code as input
   - Analyzes syntax and semantics
   - Produces equivalent Fifth code
   - Documents stack effects

3. **Critic** - The validator
   - Reviews the translated code
   - Checks for Fifth conventions
   - Identifies potential issues
   - Suggests improvements

## How to Run

```bash
./fifth examples/agent-orchestra/main.fs
```

This runs the full demo with console output showing the agent collaboration.

## Expected Output

The demo processes this task:
> "Convert Python calculate_stats function to Fifth"

### Input Python Code
```python
def calculate_stats(numbers):
    total = sum(numbers)
    count = len(numbers)
    average = total / count
    return {'total': total, 'avg': average}
```

### Output Fifth Code
```forth
: calculate-stats ( addr count -- total avg )
  2dup                \ keep copy for average calc
  0 -rot              \ ( 0 addr count )
  0 ?do               \ loop count times
    dup i cells + @   \ get numbers[i]
    rot + swap        \ accumulate sum
  loop
  drop                \ drop addr, keep sum
  dup rot             \ ( sum sum count )
  / ;                 \ ( sum avg )
```

## Agent Workflow

```
Task Input
    |
    v
[CONDUCTOR]
    |
    | decompose into subtasks
    v
[PORTER]
    |
    | translate code
    v
[CRITIC]
    |
    | validate & suggest
    v
Output + Review
```

## Key Fifth Patterns Demonstrated

### State Management
- Uses fixed buffers for agent data (no `allocate`)
- Execution log with bounded size
- Agent status tracking

### Stack Discipline
- All words have stack comments: `( before -- after )`
- String pairs handled with `2dup`, `2drop`, etc.
- Clear data flow through agent phases

### Buffer Pattern
- `log-reset`, `log+`, `log$` for log accumulation
- Agent name/role/status stored in fixed-size fields
- No dynamic string concatenation

### Console Output
- Simple console-based output for portability
- Works without external library dependencies

## Extending This Example

To add new agents:

1. Increase `num-agents` constant
2. Add initialization in `init-agents`
3. Create agent-specific words (e.g., `optimizer-translate`)
4. Update `run-orchestra` to include new phase

To connect to real LLMs:

1. Replace hardcoded output with `call-llm` (see `agent-loop` example)
2. Build proper JSON prompts for each agent's role
3. Parse LLM responses to drive agent behavior

## Design Notes

This example is self-contained and does not require the core.fs library.
It includes a minimal string buffer implementation to demonstrate the
pattern without external dependencies.

## Related Examples

- `agent-loop/main.fs` - Full ReAct agent with LLM integration
- `agentic-coder/main.fs` - Complete coding assistant
- `code-generator/main.fs` - Code generation patterns
