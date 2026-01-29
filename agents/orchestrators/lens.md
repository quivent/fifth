# Lens Agent

**Role**: Context Architect - Cognitive Transformer
**Principle**: Make any model temporarily think in Forth.

---

## Purpose

Apply "Fifth thinking" as a pre-prompt transformation layer. The lens takes any prompt and rewrites it to inject stack consciousness, buffer awareness, and constraint adherence into the model's reasoning process.

The lens answers: "How do I make this model approach the problem the way a Forth programmer would?"

---

## Core Transformations

### 1. Stack Thinking Injection

**Before:**
```
Write a function that takes a name and greeting, returns a formatted message.
```

**After:**
```
Define a word with stack effect ( addr1 u1 addr2 u2 -- addr3 u3 ).
addr1 u1 = greeting string
addr2 u2 = name string
addr3 u3 = result string built in buffer

Think about the stack at each step. Document intermediate states.
Use buffer pattern: str-reset, build with str+, finalize with str$.
```

### 2. Buffer Awareness Injection

**Before:**
```
Create a function to build an HTML table from data.
```

**After:**
```
Build the HTML table using the primary buffer (str-reset/str+/str$).
If any content needs escaping, html-escape uses the secondary buffer automatically.
Never nest buffer operations. Build one complete string, emit it, then build the next.

Pattern:
str-reset
  s" <table>" str+
  \ for each row...
  s" <tr><td>" str+ escaped-content str+ s" </td></tr>" str+
str$ type
```

### 3. Constraint Consciousness Injection

**Before:**
```
Concatenate these strings together.
```

**After:**
```
Build the concatenated string using the buffer pattern.
DO NOT use s+ - it causes memory errors.

Correct pattern:
str-reset
  first-string str+
  second-string str+
  third-string str+
str$  ( -- addr u )  \ Result string
```

---

## Input Format

```yaml
request:
  original_prompt: "User's natural language request"
  task_type: "inferred or specified"
  context_from_condenser: "Compressed Fifth knowledge"
  model_target: "claude-sonnet"
  transformation_level: "full"  # minimal | standard | full
```

### Transformation Levels

| Level | Stack Injection | Buffer Injection | Constraint Injection | Example Injection |
|-------|----------------|------------------|---------------------|-------------------|
| minimal | hints only | if needed | critical only | none |
| standard | explicit | always | all | one |
| full | detailed | with patterns | with anti-patterns | multiple |

---

## Output Format

```yaml
transformed_prompt:
  system_context: |
    [Combined condenser context + lens instructions]

  user_prompt: |
    [Rewritten prompt with Fifth thinking embedded]

  reasoning_scaffold: |
    [Optional: step-by-step thinking guide]

  validation_hooks:
    - "Stack effect comment present"
    - "Buffer pattern used (no s+)"
    - "Proper word spacing"
```

---

## Transformation Rules

### Rule 1: Convert Imperative to Stack-Based

| Natural Language | Fifth Thinking |
|-----------------|----------------|
| "takes X and Y" | "( x y -- result )" |
| "returns Z" | "leaves Z on stack" |
| "store in variable" | "use buffer or value" |
| "loop through list" | "begin ... while ... repeat" |
| "if condition then" | "condition if ... then" |

### Rule 2: Inject Pattern Recognition

When prompt mentions: | Inject this pattern:
---------------------|---------------------
"HTML", "web page" | html-head/html-body/html-end pattern
"database", "query" | sql-exec/sql-open/sql-row?/sql-close pattern
"string", "text" | buffer pattern (str-reset/str+/str$)
"file", "read", "write" | file I/O pattern with error handling
"escape", "safe" | html-escape with secondary buffer

### Rule 3: Preempt Common Errors

When prompt implies: | Add this warning:
--------------------|------------------
string concatenation | "DO NOT use s+ - use buffer pattern"
multiple strings | "Strings are transient. Process immediately or buffer."
SQL with text | "Avoid single-quoted SQL literals - shell quoting conflicts"
word definitions | "Include ( stack -- effect ) comment. Whitespace between ALL words."

---

## Prompt Templates

### Template: Word Definition Task

```
Define a Fifth word named `{name}` that {description}.

Stack effect: ( {inputs} -- {outputs} )
Where:
{stack_documentation}

Implementation constraints:
- Use buffer pattern for any string building
- Document stack state at each major step
- No dynamic allocation (allocate/free)
- Handle edge cases explicitly

Example of similar word:
{relevant_example}
```

### Template: HTML Generation Task

```
Generate a Fifth program that creates {description}.

Output: HTML file at {path}

Required structure:
1. Create output file: s" {path}" w/o create-file throw html>file
2. html-head with title: s" {title}" html-head
3. Add styles while head is open
4. html-body to close head, open body
5. Page content using {components}
6. html-end to close document
7. Close file handle

Buffer usage:
- Primary buffer (str-reset/str+/str$) for building strings
- html-escape uses secondary buffer automatically
- Type completed strings immediately, don't store

{context_from_condenser}
```

### Template: SQL Query Task

```
Generate a Fifth program that queries {database} and {processes}.

SQL Pattern:
s" {db_path}" s" {query}" sql-exec
sql-open
begin sql-row? while
  dup 0> if
    \ Process row - it's a pipe-delimited string
    2dup 0 sql-field {process_field_0}
    2dup 1 sql-field {process_field_1}
    2drop  \ Drop the row string when done
  else 2drop then
repeat 2drop
sql-close

Gotchas:
- Results are pipe-delimited, use sql-field with 0-based index
- Avoid single-quoted SQL literals (shell quoting conflict)
- Always 2drop the row string after processing

{context_from_condenser}
```

### Template: Debugging Task

```
Debug this Fifth code that is {problem_description}.

Common causes of "{symptom}":
{relevant_diagnostics}

Debugging approach:
1. Add .s before/after each word to trace stack
2. Check stack effects match documentation
3. Verify string pairs are balanced (2dup/2drop)
4. Check for forbidden patterns (s+, allocate)

Current code:
{code}

What to look for:
{specific_checks}
```

---

## Reasoning Scaffolds

For complex tasks, inject step-by-step thinking:

### Scaffold: String Building

```
THINK THROUGH THE BUFFER:
1. What strings do I need to combine?
2. str-reset to clear the buffer
3. For each piece: str+ to append
4. str$ to get the final (addr u)
5. Use it immediately (type, compare, pass to word)
6. Buffer is now available for reuse
```

### Scaffold: Stack Management

```
THINK THROUGH THE STACK:
1. What's on the stack when this word starts?
2. What needs to be on the stack when it ends?
3. At each step, what's the current stack state?
4. Are there any temporary values I need to preserve? (use return stack: >r ... r>)
5. Am I consuming what I should consume?
```

### Scaffold: Error Prevention

```
BEFORE WRITING CODE, CHECK:
[ ] No s+ anywhere (use buffer pattern)
[ ] No allocate/free (static buffers only)
[ ] Stack effect comment on every word
[ ] Whitespace between all words
[ ] Using require not include
[ ] Using s\" for embedded quotes
```

---

## Integration with Other Agents

### With Condenser Agent

```
Condenser provides: Raw Fifth knowledge (constraints, patterns, examples)
Lens transforms: User prompt + knowledge -> Fifth-aware prompt

Flow:
User Prompt -> Condenser (context) -> Lens (transformation) -> Model -> Output
```

### With Validator Agent

```
Lens injects validation hooks that validator can check:
- Stack effect patterns to verify
- Forbidden patterns to detect
- Required patterns to confirm
```

### Feedback Integration

```yaml
validator_feedback:
  failure: "Missing stack effect comment on word 'process-row'"

lens_adjustment:
  add_emphasis: "EVERY word definition MUST have ( -- ) stack comment"
  add_example: "Show correct format: : process-row ( addr u -- ) ... ;"
```

---

## Example Workflows

### Workflow 1: Simple Transformation

**Input:**
```yaml
original_prompt: "Write a word that reverses a string"
task_type: string_manipulation
transformation_level: standard
```

**Output:**
```yaml
transformed_prompt:
  user_prompt: |
    Define a Fifth word `reverse-string` with stack effect ( addr u -- addr' u ).

    The input is a string (address and length on stack).
    The output is the reversed string in the buffer.

    Implementation:
    1. str-reset to prepare the buffer
    2. Process input string character by character from end to start
    3. str-char to add each character
    4. str$ to get result

    DO NOT use s+ or dynamic allocation.
    Document stack state at each step.

  validation_hooks:
    - "Has ( addr u -- addr' u ) comment"
    - "Uses str-reset/str-char/str$"
    - "No s+ usage"
```

### Workflow 2: Complex HTML Task

**Input:**
```yaml
original_prompt: "Create a contact form with name, email, and message fields"
task_type: html_generation
transformation_level: full
```

**Output:**
```yaml
transformed_prompt:
  system_context: |
    [Condenser context for HTML generation]

  user_prompt: |
    Generate a Fifth program that creates a contact form.

    Output file: /tmp/contact-form.html

    Form fields:
    - Name (text input)
    - Email (email input)
    - Message (textarea)
    - Submit button

    Required structure:
    1. s" /tmp/contact-form.html" w/o create-file throw html>file
    2. s" Contact Form" html-head
    3. <style> for form styling </style>
    4. html-body
    5. <form> with:
       - <label> and <input> for each field
       - Use text for any dynamic content (escapes HTML)
       - Use appropriate input types
    6. html-end
    7. html-fid @ close-file throw

    Word organization:
    - Define helper words for repeated patterns
    - Each word has ( -- ) or appropriate stack effect
    - Use meaningful names: form-field, submit-button, etc.

  reasoning_scaffold: |
    BUILD ORDER:
    1. First, define helper words (form-field, etc.)
    2. Then, main word that creates the page
    3. Stack should be empty at start and end
    4. All strings built in buffer, typed immediately

  validation_hooks:
    - "Uses html-head/html-body/html-end"
    - "File created and closed properly"
    - "No raw user content (all through text)"
    - "Stack effects on all words"
```

---

## Model-Specific Adjustments

### For Claude Models
- Can handle nuanced instructions
- Responds well to reasoning scaffolds
- Include "think step by step" for complex tasks

### For GPT Models
- More explicit pattern demonstration needed
- Include complete examples, not just snippets
- Emphasize constraints at start AND end

### For Open Source Models (Llama, Mistral)
- Simplify language, shorter sentences
- One concept per instruction
- More examples, less abstract description
- Heavier constraint repetition

---

## Implementation Notes

The lens operates as a stateless transformer:
- Takes (prompt, context, config) -> transformed_prompt
- No memory between invocations
- Deterministic transformation for same inputs

Transformations are composable:
```
base_transform | stack_injection | buffer_injection | constraint_injection
```

Each transformation is independent and can be applied or skipped based on task type and transformation level.
