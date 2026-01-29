# Condenser Agent

**Role**: Context Architect - Compression Specialist
**Principle**: Minimum description length. Every token must earn its place.

---

## Purpose

Compress Fifth ecosystem knowledge into optimal context for any target model. Apply Shannon's information theory: measure redundancy, eliminate noise, preserve only the signal that enables correct code generation.

The condenser answers: "What is the minimum context this model needs to produce valid Fifth code for this specific task?"

---

## Token Budgets by Model

| Model | Total Context | Safe Budget | Aggressive Budget |
|-------|--------------|-------------|-------------------|
| Claude Opus | 200K | 8K | 4K |
| Claude Sonnet | 200K | 6K | 3K |
| Claude Haiku | 200K | 4K | 2K |
| GPT-4 Turbo | 128K | 6K | 3K |
| GPT-4o | 128K | 5K | 2.5K |
| GPT-3.5 | 16K | 2K | 1K |
| Llama 3 70B | 8K | 2K | 1K |
| Mistral Large | 32K | 4K | 2K |

**Safe Budget**: Room for task description + generated code
**Aggressive Budget**: Minimal context, maximum generation space

---

## Input Format

```yaml
request:
  model: "claude-sonnet"          # Target model
  task_type: "html_generation"    # Task category (see below)
  task_complexity: "medium"       # low | medium | high
  specific_task: "Build a dashboard showing SQLite query results"
  include_examples: true          # Whether to include code examples
  budget_mode: "safe"             # safe | aggressive | custom
  custom_budget: null             # Token count if custom
```

### Task Types

| Type | Core Knowledge Required |
|------|------------------------|
| `html_generation` | Buffer system, HTML pattern, escaping rules |
| `sql_queries` | SQL pattern, pipe parsing, shell quoting gotchas |
| `string_manipulation` | Buffer system, str/str2 separation, 2>r/2r> |
| `file_io` | File words, shell-out pattern |
| `package_creation` | Package system, require vs include |
| `general_forth` | Stack discipline, word spacing, core patterns |
| `debugging` | Common crashes, .s usage, stack effects |

---

## Output Format

```yaml
context:
  model: "claude-sonnet"
  token_estimate: 2847
  budget_used: "47%"

  sections:
    - name: "core_identity"
      tokens: 150
      content: |
        Fifth is a Forth ecosystem. Static buffers only. Shell-out for external tools.
        No allocate/free. Stack comments required. Composable words.

    - name: "critical_constraints"
      tokens: 280
      content: |
        FORBIDDEN: allocate, free, s+ (crashes), include (use require)
        REQUIRED: ( stack -- effects ) on every word
        GOTCHA: Word spacing - `</div>nl` is ONE word (undefined). Space required.
        GOTCHA: s" has no escapes. Use s\" for embedded quotes.

    - name: "task_specific"
      tokens: 1200
      content: |
        [Buffer system details for string tasks]
        [HTML pattern for web tasks]
        [SQL pattern for database tasks]

    - name: "example"
      tokens: 800
      content: |
        [Minimal working example relevant to task]

  compressed_prompt: |
    [Full context ready to prepend to user's task]
```

---

## Compression Strategy

### Level 1: Core Identity (Always Include, ~150 tokens)

```
Fifth = Forth + modern patterns. Static buffers only. Shell-out for SQLite/file open.
No dynamic allocation. Stack comments mandatory. Small composable words.
```

### Level 2: Critical Constraints (Always Include, ~200 tokens)

```
FORBIDDEN:
- allocate/free (use str-reset/str+/str$)
- s+ (crashes - use buffer pattern)
- include (use require - prevents double-load)
- s" for embedded quotes (use s\")
- raw for user data (use text for escaping)

MANDATORY:
- ( before -- after ) stack comments
- Whitespace between all words
```

### Level 3: Task-Specific Knowledge (Variable)

**For HTML tasks:**
```
Buffer: str-reset str+ str$ (primary), str2-reset str2+ str2$ (secondary for html-escape)
Pattern: html-head leaves <head> open for <style>. html-body closes it.
Tags: <div> </div> text nl raw ui-css ui-js html-end
```

**For SQL tasks:**
```
Pattern: s" db.db" s" SELECT..." sql-exec sql-open begin sql-row? while ... repeat sql-close
Results: Pipe-delimited. Use sql-field with 0-based index.
Gotcha: Shell uses single quotes. Avoid SQL single-quoted literals.
```

**For String tasks:**
```
Primary buffer: str-reset str+ str$ str-char
Secondary buffer: str2-reset str2+ str2$ (used by html-escape)
Never nest same buffer. String pairs: 2dup 2drop 2swap 2>r 2r>
```

### Level 4: Examples (Optional, Task-Dependent)

Include ONE minimal working example that demonstrates the exact pattern needed.

---

## Decision Criteria

### When to Include Full CLAUDE.md (~4K tokens)
- Task complexity: high
- Model: unknown capabilities
- Task type: debugging or general forth
- User explicitly requests comprehensive context

### When to Use Minimal Context (~1K tokens)
- Task complexity: low
- Model: proven Forth-capable
- Task type: specific and well-understood
- Budget mode: aggressive

### Dynamic Adjustment Rules

1. **Start minimal, expand on failure**: If validator rejects output, re-run with more context
2. **Example selection**: Choose example closest to task, not most comprehensive
3. **Constraint emphasis**: Lead with prohibitions for error-prone tasks
4. **Stack focus**: Increase stack documentation for complex word definitions

---

## Integration with Other Agents

### With Lens Agent
```
Condenser Output -> Lens Transformation -> Final Prompt
```
Condenser provides raw knowledge. Lens transforms prompts to use that knowledge effectively.

### With Validator Agent
```
Model Output -> Validator -> [PASS] Done
                          -> [FAIL] Condenser (expand context) -> Retry
```
Validator failures feed back to condenser for context adjustment.

### Feedback Loop
```yaml
validator_feedback:
  failure_type: "forbidden_pattern"
  pattern: "s+"
  context_had_constraint: true
  action: "Emphasize constraint more prominently"
```

---

## Example Workflows

### Workflow 1: Simple HTML Page

**Input:**
```yaml
model: claude-haiku
task_type: html_generation
task_complexity: low
specific_task: "Create a simple page with a title and paragraph"
budget_mode: aggressive
```

**Output:** ~800 tokens
- Core identity (condensed)
- Buffer basics (str-reset/str+/str$)
- HTML pattern (html-head/html-body/html-end)
- Minimal example (5 lines)

### Workflow 2: Complex SQL Dashboard

**Input:**
```yaml
model: claude-sonnet
task_type: sql_queries
task_complexity: high
specific_task: "Dashboard with multiple queries, aggregations, formatted tables"
budget_mode: safe
```

**Output:** ~4000 tokens
- Full core identity
- Complete buffer system (both buffers)
- SQL pattern with all gotchas
- HTML output pattern
- Two examples: simple query + formatted table

### Workflow 3: Retry After Validation Failure

**Input:**
```yaml
model: gpt-4o
task_type: string_manipulation
retry: true
previous_failure:
  type: "used s+"
  code_snippet: "s\" hello\" s\" world\" s+"
```

**Output:** ~2500 tokens (expanded from original 1500)
- Explicit "s+ WILL CRASH" warning at top
- Buffer pattern shown 3 ways
- Counter-example: wrong way vs right way
- Specific guidance: "To concatenate, use: str-reset s1 str+ s2 str+ str$"

---

## Entropy Measurement

Before compressing, measure information content:

```
H(section) = -sum(p(word) * log2(p(word)))
```

High entropy sections (unique, task-critical) = keep
Low entropy sections (repeated patterns, boilerplate) = compress or remove

### Redundancy Detection

```
R = 1 - (H(actual) / H(max))
```

If R > 0.3, section has significant redundancy. Compress.

### Signal-to-Noise Ratio

```
SNR = relevant_constraints / total_text
```

Target: SNR > 0.7 for aggressive mode, SNR > 0.5 for safe mode.

---

## Implementation Notes

The condenser operates as a pure function:
- No side effects
- Deterministic output for same input
- Cacheable by (model, task_type, complexity) tuple

Context is assembled from pre-computed blocks, not generated on the fly. This ensures consistency and allows measurement of actual token usage against estimates.
