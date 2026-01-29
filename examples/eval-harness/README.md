# Eval Harness - LLM Code Generation Benchmarking

A rigorous evaluation framework for measuring AI code generation quality, implemented in Fifth.

## Information-Theoretic Foundation

From Claude Shannon's perspective, code generation evaluation is fundamentally about measuring *channel capacity* between a natural language specification and working code. The key metrics:

- **Mutual Information**: How much of the specification's intent is preserved in generated code?
- **Redundancy**: How much of the model's output is irrelevant to the task?
- **Channel Noise**: What fraction of outputs contain errors introduced by the model?

The goal: measure the effective bits of problem-solving capability, not just surface-level text similarity.

## Why Evaluation Matters

### The Problem with Vibes-Based Assessment

Most LLM evaluation is dangerously imprecise:
- "It seems to work better" (selection bias)
- "The code looks cleaner" (subjective aesthetics)
- "It passed the test I tried" (insufficient coverage)

### What Rigorous Evaluation Provides

1. **Reproducibility**: Same benchmark, same results, any time
2. **Comparability**: Meaningful A/B tests between models/prompts
3. **Signal**: Distinguish capability from luck via statistical analysis
4. **Progress tracking**: Measure improvement over time

## Standard Benchmarks

### HumanEval

OpenAI's benchmark of 164 hand-written Python problems:
- Function signature provided
- Natural language docstring
- Test cases for verification
- Measures: pass@k (probability of passing with k attempts)

### MBPP (Mostly Basic Python Problems)

Google's benchmark of ~1000 crowd-sourced problems:
- Task description
- Reference solution
- Three test cases per problem
- Designed for simpler, practical tasks

### Custom Benchmarks

This framework supports custom problem sets:
- Domain-specific challenges (Forth, embedded, systems)
- Company-specific coding patterns
- Proprietary API usage
- Security-sensitive implementations

## Core Metrics

### pass@k

The probability that at least one of k samples passes all tests:

```
pass@k = 1 - C(n-c, k) / C(n, k)

Where:
  n = total samples generated
  c = number of correct samples
  k = number of samples considered
```

Higher k = easier to pass (more attempts).
pass@1 is the hardest, most realistic metric.

### Functional Correctness

Binary: does the code produce correct output for all test cases?

Components:
- Syntax validity (parses without error)
- Runtime success (executes without crash)
- Output correctness (matches expected results)
- Edge case handling (boundary conditions)

### Code Quality Metrics

Beyond correctness:
- **Lines of Code**: Brevity (information density)
- **Cyclomatic Complexity**: Control flow complexity
- **Stack Depth**: Forth-specific - maximum stack usage
- **Word Count**: Number of defined words
- **Reuse Ratio**: Library words vs inline code

### Timing Metrics

- **Generation Time**: API latency
- **Execution Time**: Runtime performance
- **Token Usage**: Input + output tokens
- **Cost**: USD per correct solution

## Test Case Design

### Oracle Functions

An oracle is a trusted source of truth for evaluating output:

```forth
\ Oracle: verify factorial implementation
: test-factorial ( xt -- pass? )
  0 over execute 1 = and   \ 0! = 1
  1 over execute 1 = and   \ 1! = 1
  5 over execute 120 = and \ 5! = 120
  10 swap execute 3628800 = and ;  \ 10! = 3628800
```

Oracle design principles:
1. Test edge cases (0, 1, negative, maximum)
2. Test typical cases
3. Test boundary conditions
4. Verify error handling

### Test Coverage Strategies

1. **Boundary Value Analysis**: Min, max, just inside/outside bounds
2. **Equivalence Partitioning**: One test per input class
3. **Error Guessing**: Common mistakes (off-by-one, null, overflow)
4. **Combinatorial**: Key parameter combinations

## Comparing Models and Prompts

### A/B Testing Framework

Variables to test:
- Model (GPT-4, Claude, Llama, etc.)
- Temperature (0.0-1.0)
- System prompt variations
- Few-shot examples (0, 1, 3, 5 shots)
- Prompt structure (instruction-first vs example-first)

### Statistical Significance

Don't trust small samples:
- Minimum 20 problems per benchmark
- Multiple runs per problem (for pass@k calculation)
- Calculate confidence intervals
- Use appropriate tests (binomial for pass rates)

### Systematic Comparison Protocol

1. Define hypothesis ("Model A has higher pass@1 than Model B")
2. Fix all variables except the one being tested
3. Run sufficient samples
4. Calculate metrics with confidence intervals
5. Report both positive and negative results

## Fifth's Advantages for Evaluation

### Simplicity

Forth code is unambiguous:
- No hidden imports
- No complex type systems
- Direct stack semantics
- Clear success/failure

### Shell-Out Architecture

Fifth's shell-out pattern is perfect for evaluation:
- API calls via curl (any provider)
- Sandboxed execution via subprocess
- SQLite for result storage
- jq for JSON parsing

### No Dynamic Allocation

Memory-safe execution:
- No buffer overflows from generated code
- Predictable resource usage
- Easier to sandbox

### Stack Discipline

Generated code quality is measurable:
- Stack effect comments are verifiable
- Stack underflow/overflow is detectable
- Clean stack = correct implementation

## Usage

### Quick Start

```bash
# Run evaluation with default settings
./fifth examples/eval-harness/main.fs run

# Evaluate specific model
./fifth examples/eval-harness/main.fs run --model claude-3-opus

# Compare two prompts
./fifth examples/eval-harness/main.fs compare prompt-a.txt prompt-b.txt

# Generate report
./fifth examples/eval-harness/main.fs report
```

### Problem Format (SQLite)

Problems are stored in a SQLite database:

```sql
CREATE TABLE problems (
  id TEXT PRIMARY KEY,
  name TEXT,
  description TEXT,      -- Natural language spec
  signature TEXT,        -- Function signature
  test_code TEXT,        -- Forth test code
  difficulty TEXT,       -- easy/medium/hard
  category TEXT          -- string/math/stack/etc
);
```

### Configuration

Environment variables:
- `ANTHROPIC_API_KEY` - Claude access
- `OPENAI_API_KEY` - GPT access
- `EVAL_DB` - Results database path
- `EVAL_SAMPLES` - Samples per problem (default: 5)
- `EVAL_TIMEOUT` - Execution timeout in ms (default: 5000)

### Result Format

Results stored in SQLite:

```sql
CREATE TABLE runs (
  id INTEGER PRIMARY KEY,
  timestamp TEXT,
  model TEXT,
  prompt_variant TEXT,
  config TEXT              -- JSON: temperature, max_tokens, etc
);

CREATE TABLE results (
  run_id INTEGER,
  problem_id TEXT,
  sample_num INTEGER,
  generated_code TEXT,
  passed INTEGER,          -- 0 or 1
  error_type TEXT,         -- null, syntax, runtime, wrong_output
  execution_ms INTEGER,
  tokens_in INTEGER,
  tokens_out INTEGER,
  FOREIGN KEY (run_id) REFERENCES runs(id)
);
```

## Sample Problems

### Problem: stack-dup

```
Name: stack-dup
Description: Implement DUP - duplicate the top stack item
Signature: ( n -- n n )
Tests:
  5 dup = should leave 5 5 on stack
  0 dup = should leave 0 0 on stack
  -1 dup = should leave -1 -1 on stack
```

### Problem: factorial

```
Name: factorial
Description: Compute n! (factorial) for non-negative integers
Signature: ( n -- n! )
Tests:
  0 factorial = 1
  1 factorial = 1
  5 factorial = 120
  10 factorial = 3628800
```

### Problem: fizzbuzz-word

```
Name: fizzbuzz-word
Description: Print fizzbuzz for numbers 1 to n
Signature: ( n -- )
Tests:
  15 fizzbuzz = outputs "1 2 fizz 4 buzz fizz 7 8 fizz buzz 11 fizz 13 14 fizzbuzz"
```

## Architecture

```
eval-harness/
  main.fs           -- Entry point, CLI handling
  problems.db       -- Benchmark problems (SQLite)
  results.db        -- Evaluation results (SQLite)
  prompts/          -- Prompt templates
    default.txt
    cot.txt         -- Chain of thought
    few-shot.txt    -- With examples
```

## Design Philosophy

### Measure Before Optimizing (Shannon)

Don't guess what makes prompts better. Measure:
1. Baseline performance
2. Single variable changes
3. Statistical significance
4. Actual improvement

### Zero-Overhead Abstraction (Stroustrup)

The evaluation framework should not interfere with what it measures:
- Minimal overhead per evaluation
- No hidden costs in the measurement
- Clear separation of concerns

### What You Don't Use, You Don't Pay For

The harness loads only what's needed:
- Problems loaded on demand
- Results streamed to database
- No in-memory accumulation of large result sets

## Extending the Framework

### Adding New Problems

```forth
s" problems.db" s" INSERT INTO problems VALUES (
  'my-problem',
  'My Problem Name',
  'Description of what to implement',
  '( inputs -- outputs )',
  ': test-my-problem ( xt -- pass? ) ... ;',
  'medium',
  'category'
)" sql-exec
```

### Adding New Providers

Implement the provider interface:
1. Build request JSON
2. Shell out to curl
3. Parse response JSON
4. Extract generated code

### Custom Metrics

Add metric calculation words:
```forth
: measure-stack-depth ( code$ -- depth )
  \ Parse code, track maximum stack usage
  ... ;
```

## References

- Chen et al. "Evaluating Large Language Models Trained on Code" (HumanEval paper)
- Austin et al. "Program Synthesis with Large Language Models" (MBPP paper)
- Shannon, C. "A Mathematical Theory of Communication"
- Stroustrup, B. "The C++ Programming Language" (zero-overhead principle)
