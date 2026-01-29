# Training Data Generator for Code Models

A Fifth/Forth implementation for extracting high-quality training data from codebases. This tool demonstrates how concatenative languages excel at text transformation pipelines -- precisely the kind of workload that training data curation demands.

## Why Training Data Quality Matters

From an information-theoretic perspective (channeling Shannon), training data quality determines the channel capacity between your codebase and the model's learned representations. Poor data introduces noise that degrades the signal:

**Entropy Considerations:**
- Redundant examples waste capacity (model learns the same pattern multiple times)
- Noisy examples (bad code, broken docstrings) corrupt the learned distribution
- Imbalanced examples bias the model toward overrepresented patterns

**Quality Signals:**
- Docstring completeness (H(code|docstring) should be low for good docs)
- Code coherence (functions that do one thing well)
- Test coverage (tests constrain interpretation of implementation)
- Commit message informativeness (high mutual information with diff)

From a language design perspective (channeling Stroustrup), the extraction process must respect the semantics of the source language. A docstring parser that works for Python will fail on C++; template metaprogramming requires different extraction logic than virtual functions.

## Extraction Strategies

### 1. Docstring-to-Code Pairs

The most common training format. Extract the docstring as "instruction" and the function body as "response."

```
Input:  def calculate_entropy(probabilities):
            """Calculate Shannon entropy from probability distribution."""
            return -sum(p * log2(p) for p in probabilities if p > 0)

Output: {"instruction": "Calculate Shannon entropy from probability distribution.",
         "output": "return -sum(p * log2(p) for p in probabilities if p > 0)"}
```

**Quality Filters:**
- Docstring must be >10 chars (reject empty/trivial)
- Function body must be >3 lines (reject trivial getters)
- No TODO/FIXME in docstring (incomplete documentation)
- Language-specific: reject `pass`-only Python, empty C++ bodies

### 2. Commit Message-to-Diff Pairs

Git history is a goldmine. Each commit is a human describing what they changed.

```
Commit: "Fix off-by-one error in buffer boundary check"
Diff:   -  if (pos >= buffer_size) {
        +  if (pos > buffer_size) {
```

**Quality Filters:**
- Reject merge commits (automated, uninformative)
- Reject commits with >500 lines changed (too coarse-grained)
- Reject single-word messages ("fix", "wip", "update")
- Prefer atomic commits (one logical change)

### 3. Test-to-Implementation Mappings

Tests specify behavior; implementation satisfies the spec. This is instruction-following in pure form.

```
Test:   assert parse_date("2024-01-15") == Date(2024, 1, 15)
        assert parse_date("invalid") raises ValueError

Impl:   def parse_date(s):
            parts = s.split('-')
            if len(parts) != 3:
                raise ValueError("Invalid date format")
            return Date(int(parts[0]), int(parts[1]), int(parts[2]))
```

**Quality Filters:**
- Test must be actual assertion, not setup code
- Implementation must be nearby (same file or clear import)
- Reject mocked tests (don't teach the model to mock)

## Data Cleaning and Deduplication

### Exact Deduplication

Hash-based. Two samples with identical (instruction, output) pairs are duplicates.

### Near-Deduplication

More important and harder. Use n-gram overlap or embedding similarity:

```
Sample A: "Calculate the sum of two numbers"
Sample B: "Compute the addition of two integers"

Similarity: ~0.85 (semantically equivalent, keep only one)
```

### Content Filtering

Remove samples containing:
- Personal information (emails, API keys, passwords)
- License headers (boilerplate, not instructive)
- Auto-generated code (often low quality)
- Language the model shouldn't learn (slurs, hate speech in comments)

### Length Filtering

Shannon's source coding theorem tells us: samples should have high information density.

- Too short: `def f(): pass` -- no information
- Too long: 1000-line functions -- too much to fit in context
- Sweet spot: 10-200 lines with clear structure

## Output Formats

### Alpaca Format (Instruction Tuning)

```json
{
  "instruction": "Write a function to calculate factorial",
  "input": "n = 5",
  "output": "def factorial(n):\n    if n <= 1:\n        return 1\n    return n * factorial(n-1)"
}
```

### ShareGPT Format (Chat)

```json
{
  "conversations": [
    {"from": "human", "value": "How do I implement binary search?"},
    {"from": "gpt", "value": "Here's a binary search implementation:\n\n```python\ndef binary_search...```"}
  ]
}
```

### Completion Format (Pre-training)

```json
{
  "text": "def binary_search(arr, target):\n    \"\"\"Find target in sorted array.\"\"\"\n    left, right = 0, len(arr) - 1\n    ..."
}
```

### JSONL Output

All formats output as JSONL (one JSON object per line) for streaming processing:

```
{"instruction": "...", "output": "..."}
{"instruction": "...", "output": "..."}
```

## Why Fifth for Training Data Generation?

### 1. Natural Text Pipeline

Fifth's buffer system (`str-reset`, `str+`, `str$`) maps directly to text transformation:

```forth
: extract-docstring ( line$ -- )
  s" \"\"\"" prefix? if
    str-reset str+ str$
    \ ...
  then ;
```

### 2. Shell-Out Pattern

Git, grep, find, jq -- the Unix text processing toolkit is available:

```forth
: git-commits ( n -- addr u )
  str-reset
  s" git log --format='%H|%s' -" str+
  n>str str+
  str$ evaluate-shell ;
```

### 3. No Dynamic Allocation

Training data curation processes millions of samples. Fifth's static buffers avoid allocation overhead and fragmentation. When you need to reset, you reset -- no garbage collection pause.

### 4. Composable Filters

Each filter is a word. Compose them:

```forth
: quality-check ( sample$ -- flag )
  dup min-length-ok?
  swap has-docstring?
  and
  swap no-todo?
  and ;
```

### 5. Stack-Based State

Processing state lives on the stack, making parallelization trivial (each thread gets its own stack).

## Usage

```bash
# Extract from current directory
./fifth examples/training-data-gen/main.fs .

# Extract from specific path
./fifth examples/training-data-gen/main.fs /path/to/repo

# Extract with format option
./fifth examples/training-data-gen/main.fs /path/to/repo --format=sharegpt

# Extract only from git history
./fifth examples/training-data-gen/main.fs /path/to/repo --git-only
```

## Statistics Tracking

The tool tracks:

- **Total samples extracted**: Raw count before filtering
- **Samples after dedup**: Count after exact deduplication
- **Samples after quality filter**: Final count
- **By source**: Docstrings, commits, tests
- **By language**: Python, JavaScript, etc.

Output summary:

```
Training Data Generation Complete
=================================
Source files scanned:     1,234
Git commits processed:      567

Samples extracted:        8,901
  - Docstring pairs:      4,123
  - Commit pairs:         3,456
  - Test pairs:           1,322

After deduplication:      7,234 (81%)
After quality filter:     5,891 (66%)

Output: training_data.jsonl
Format: alpaca
```

## Integration with ML Pipelines

The JSONL output integrates directly with:

- **Hugging Face datasets**: `datasets.load_dataset("json", data_files="output.jsonl")`
- **axolotl**: Point config at the JSONL file
- **LitGPT**: Use as fine-tuning dataset
- **Custom trainers**: Stream JSONL line by line

## Information-Theoretic Quality Metrics

For the Shannon-minded, we compute:

- **Compression ratio**: High ratio = repetitive = low quality
- **Token entropy**: Measured via tiktoken, samples should be >4.5 bits/token
- **Mutual information**: I(instruction; output) should be high

These metrics appear in the final report and can be used for automated filtering.

## Extending the Extractor

To add support for a new language:

1. Add a parser in `parsers/`
2. Register file extensions
3. Define docstring/function extraction rules
4. Add to the main dispatch

To add a new extraction strategy:

1. Create extraction word
2. Add to the main loop
3. Add statistics tracking

## License

MIT. Use this to train whatever you want.
