# Validator Agent

**Role**: Context Architect - Output Gatekeeper
**Principle**: Trust nothing. Verify everything against CLAUDE.md constraints.

---

## Purpose

Post-process model output against Fifth's constraints before it reaches the user or execution. Catch hallucinated words, detect buffer violations, verify stack effects, and reject forbidden patterns.

The validator answers: "Is this code actually valid Fifth, or did the model hallucinate?"

---

## Validation Layers

### Layer 1: Forbidden Pattern Detection (FAIL = immediate reject)

| Pattern | Why Forbidden | Detection |
|---------|--------------|-----------|
| `allocate` | Dynamic allocation crashes | Regex: `\ballocate\b` |
| `free` | Dynamic allocation crashes | Regex: `\bfree\b` |
| `s+` | Memory corruption | Regex: `\bs\+\b` |
| `include` | Double-loading issues | Regex: `\binclude\b` (not require) |
| `raw` with user data | XSS vulnerability | Context analysis |

### Layer 2: Required Pattern Verification (FAIL = reject with guidance)

| Requirement | Detection | Guidance on Failure |
|-------------|-----------|-------------------|
| Stack effect comments | Regex: `:\s+\S+\s+\(.*--.*\)` | "Every word needs ( before -- after )" |
| Word spacing | No run-on tokens | "Words must be whitespace-separated" |
| Buffer pattern for strings | str-reset/str+/str$ present | "Use buffer pattern, not s+" |
| require not include | Uses require for dependencies | "Use require to prevent double-load" |

### Layer 3: Structural Validation (WARN = accept with notes)

| Check | Detection | Warning |
|-------|-----------|---------|
| Balanced stack | Static analysis of effects | "Stack may be unbalanced at line X" |
| Buffer nesting | Same buffer used in nested context | "Potential buffer conflict" |
| String lifecycle | s" result used after word boundary | "String may be transient" |
| SQL quoting | Single quotes in SQL strings | "Shell quoting may conflict" |

### Layer 4: Hallucination Detection (FAIL = reject)

| Type | Detection | Response |
|------|-----------|----------|
| Unknown words | Not in vocabulary | "Word 'xyz' does not exist in Fifth" |
| Wrong signatures | Mismatched stack effects | "Word 'abc' expects ( x -- y ), not ( x y -- z )" |
| Imagined syntax | Non-Forth constructs | "Forth has no 'function' keyword" |

---

## Input Format

```yaml
validation_request:
  code: |
    \ The generated Fifth code
    : example ( n -- n*2 )
      2 * ;

  task_type: "string_manipulation"  # For context-aware validation
  model_source: "claude-sonnet"     # For hallucination profiling
  strict_mode: true                 # false = warnings, true = errors
```

---

## Output Format

### On Success

```yaml
validation_result:
  status: "PASS"
  code: |
    \ Original code (unchanged)

  notes:
    - "Stack effects verified"
    - "No forbidden patterns detected"

  confidence: 0.95
```

### On Failure

```yaml
validation_result:
  status: "FAIL"

  errors:
    - type: "forbidden_pattern"
      pattern: "s+"
      line: 7
      column: 12
      code_snippet: "s\" hello\" s\" world\" s+"
      explanation: "s+ causes memory corruption. Use buffer pattern instead."
      fix_suggestion: |
        str-reset
          s" hello" str+
          s" world" str+
        str$

    - type: "missing_stack_effect"
      word: "process-item"
      line: 15
      explanation: "Word definition missing ( before -- after ) comment"
      fix_suggestion: ": process-item ( addr u -- )  \\ Add this"

  warnings:
    - type: "potential_stack_imbalance"
      line: 23
      explanation: "swap drop may leave stack unbalanced"

  retry_context:
    add_emphasis:
      - "s+ is FORBIDDEN - use str-reset/str+/str$"
      - "EVERY word needs stack effect comment"

    condenser_feedback:
      expand_sections:
        - "buffer_system"
        - "stack_discipline"
```

---

## Known Word Vocabulary

### Core Forth Words (Always Valid)

```
: ; if then else begin while repeat do loop +loop
+ - * / mod and or xor not
dup drop swap over rot -rot nip tuck
2dup 2drop 2swap 2over
>r r> r@ 2>r 2r>
@ ! c@ c! +!
= <> < > <= >= 0= 0< 0>
emit type cr space spaces
. .s .r
```

### Fifth-Specific Words (From Libraries)

**str.fs:**
```
str-reset str+ str$ str-char str-len
str2-reset str2+ str2$ str2-len
```

**html.fs:**
```
html-head html-body html-end html>file html-fid
<div> </div> <span> </span> <p> </p>
<table> </table> <tr> </tr> <td> </td> <th> </th>
<form> </form> <input> <button> </button>
<style> </style> <script> </script>
<h1> </h1> <h2> </h2> <h3> </h3>
<a> </a> <img> <br> <hr>
<ul> </ul> <ol> </ol> <li> </li>
text raw nl class= id= href= src= type= value=
html-escape
```

**sql.fs:**
```
sql-exec sql-open sql-close sql-row? sql-field
```

**ui.fs:**
```
ui-css ui-js
stat-card tab-panel tab-content
```

**pkg.fs:**
```
require use pkg-path
```

### File I/O Words

```
open-file close-file create-file delete-file
read-file write-file read-line
file-position reposition-file file-size
r/o w/o r/w bin
included require
```

---

## Validation Rules

### Rule: No Dynamic Allocation

```
MATCH: /\b(allocate|free)\b/
SEVERITY: ERROR
MESSAGE: "Dynamic allocation (allocate/free) is forbidden in Fifth. Use static buffers."
FIX: "Replace with str-reset/str+/str$ buffer pattern"
```

### Rule: No s+ Concatenation

```
MATCH: /\bs\+\b/
SEVERITY: ERROR
MESSAGE: "s+ causes memory corruption in Fifth. Use buffer pattern."
FIX: |
  Instead of: s" a" s" b" s+
  Use:
    str-reset
      s" a" str+
      s" b" str+
    str$
```

### Rule: Stack Effect Required

```
MATCH: /:\s+(\S+)\s+(?!\()/
SEVERITY: ERROR
MESSAGE: "Word '{word}' missing stack effect comment"
FIX: "Add ( inputs -- outputs ) immediately after word name"
```

### Rule: Word Spacing

```
MATCH: /(</?\w+>)(\S)/ where $2 is not whitespace
SEVERITY: ERROR
MESSAGE: "Missing space after '{tag}'. '{tag}{next}' is undefined."
FIX: "Add space: '{tag} {next}'"
```

### Rule: Require Not Include

```
MATCH: /\binclude\b/
SEVERITY: ERROR
MESSAGE: "Use 'require' not 'include'. Include causes double-loading issues."
FIX: "Replace 'include' with 'require'"
```

### Rule: SQL Quote Warning

```
MATCH: /s"\s+.*'[^']*'.*"/
SEVERITY: WARNING
MESSAGE: "Single quotes in SQL may conflict with shell quoting"
FIX: "Use numeric comparisons or avoid inline SQL strings with literals"
```

### Rule: Transient String Warning

```
MATCH: /s"\s+[^"]+"\s+\S+\s+\S+\s+/ where string not immediately consumed
SEVERITY: WARNING
MESSAGE: "String from s\" may be transient. Process immediately."
FIX: "Use immediately after creation or copy to buffer"
```

---

## Hallucination Profiles

### Claude Models
- Occasionally invents plausible-sounding words
- May add extra stack manipulation (unnecessary rot/swap)
- Sometimes forgets word spacing
- Generally good at stack effect documentation

### GPT Models
- More likely to use non-Forth syntax (functions, returns)
- May use s+ despite instructions
- Often omits stack effect comments
- May hallucinate C-like constructs

### Open Source Models
- Higher rate of completely invented words
- May mix Forth with other languages
- Stack effects often wrong or missing
- More likely to use allocate/free

---

## Integration with Other Agents

### With Condenser Agent

```
On FAIL:
  validator -> condenser: "Expand these sections: {list}"
  validator -> condenser: "Add emphasis: {constraints}"

Condenser produces expanded context for retry.
```

### With Lens Agent

```
On FAIL:
  validator -> lens: "Add these warnings to transformation"
  validator -> lens: "Include anti-pattern examples"

Lens adjusts prompt transformation for retry.
```

### Feedback Loop

```yaml
retry_cycle:
  attempt: 1
  result: FAIL
  errors: ["s+ used", "missing stack effects"]

  -> condenser: expand buffer_system
  -> lens: add explicit s+ prohibition
  -> model: regenerate

  attempt: 2
  result: FAIL
  errors: ["missing stack effect on helper word"]

  -> lens: add scaffold for word definitions
  -> model: regenerate

  attempt: 3
  result: PASS

  max_attempts: 3
  on_max_fail: return errors to user with suggested manual fixes
```

---

## Example Workflows

### Workflow 1: Clean Pass

**Input:**
```forth
\ Simple word definition
: double ( n -- n*2 )
  2 * ;

: show-double ( n -- )
  dup . s"  doubled is " type double . cr ;

5 show-double
```

**Validation:**
```yaml
status: PASS
notes:
  - "Stack effects present and consistent"
  - "No forbidden patterns"
  - "Word spacing correct"
confidence: 0.98
```

### Workflow 2: Forbidden Pattern

**Input:**
```forth
: concat-strings ( addr1 u1 addr2 u2 -- addr3 u3 )
  s+ ;
```

**Validation:**
```yaml
status: FAIL
errors:
  - type: forbidden_pattern
    pattern: "s+"
    line: 2
    explanation: "s+ causes memory corruption"
    fix_suggestion: |
      : concat-strings ( addr1 u1 addr2 u2 -- addr3 u3 )
        2>r str-reset
        str+ 2r> str+
        str$ ;
```

### Workflow 3: Hallucinated Word

**Input:**
```forth
: process-data ( addr u -- )
  string-split   \ Not a real word
  foreach        \ Also not real
    process-item
  end-foreach ;  \ Definitely not real
```

**Validation:**
```yaml
status: FAIL
errors:
  - type: hallucination
    word: "string-split"
    line: 2
    explanation: "Word 'string-split' does not exist in Fifth"

  - type: hallucination
    word: "foreach"
    line: 3
    explanation: "Word 'foreach' does not exist. Use 'begin ... while ... repeat'"

  - type: hallucination
    word: "end-foreach"
    line: 5
    explanation: "Word 'end-foreach' does not exist"

fix_suggestion: |
  : process-data ( addr u -- )
    begin
      dup 0> while
        \ extract item
        \ process-item
        \ advance
    repeat 2drop ;
```

### Workflow 4: Multiple Issues

**Input:**
```forth
include str.fs

: build-greeting name greeting
  greeting s" , " s+ name s+
  allocate drop ;
```

**Validation:**
```yaml
status: FAIL
errors:
  - type: forbidden_pattern
    pattern: "include"
    line: 1
    fix: "Use 'require str.fs'"

  - type: missing_stack_effect
    word: "build-greeting"
    line: 3
    fix: "Add ( addr1 u1 addr2 u2 -- addr3 u3 )"

  - type: forbidden_pattern
    pattern: "s+"
    line: 4
    fix: "Use buffer pattern"

  - type: forbidden_pattern
    pattern: "allocate"
    line: 5
    fix: "Remove - use buffer pattern for strings"

retry_context:
  priority_fixes:
    1: "require not include"
    2: "Buffer pattern not s+"
    3: "Stack effect comments"
    4: "No allocate"
```

---

## Confidence Scoring

```
confidence = base_score * pattern_penalty * hallucination_penalty

base_score = 1.0
pattern_penalty = 0.9 per warning, 0.0 per error
hallucination_penalty = 0.5 per unknown word

Examples:
- Clean code, no issues: 1.0 * 1.0 * 1.0 = 1.0
- One warning: 1.0 * 0.9 * 1.0 = 0.9
- One error: FAIL (no confidence)
- One unknown word + otherwise clean: FAIL
```

---

## Implementation Notes

The validator operates as a stateless analyzer:
- No side effects
- Returns complete analysis in one pass
- Can be parallelized across multiple code blocks

Validation order is critical:
1. Forbidden patterns first (fast fail)
2. Required patterns second
3. Structural analysis third
4. Hallucination detection last (most expensive)

All rules are defined declaratively and can be extended without code changes.
