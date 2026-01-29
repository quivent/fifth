\ fifth/examples/eval-harness/main.fs
\ LLM Code Generation Evaluation Framework
\ Benchmarks pass@k, functional correctness, and code quality

require ~/.fifth/lib/core.fs

\ ============================================================
\ Configuration
\ ============================================================

: problems-db ( -- addr u ) s" problems.db" ;
: results-db ( -- addr u ) s" results.db" ;
: sandbox-dir ( -- addr u ) s" /tmp/eval-sandbox" ;
: default-samples ( -- n ) 5 ;
: default-timeout-ms ( -- n ) 5000 ;
: default-model ( -- addr u ) s" claude-3-sonnet-20240229" ;

\ Environment variable helpers
: get-api-key ( -- addr u )
  s" ANTHROPIC_API_KEY" getenv dup 0= if 2drop s" " then ;

: get-openai-key ( -- addr u )
  s" OPENAI_API_KEY" getenv dup 0= if 2drop s" " then ;

\ ============================================================
\ Note: Using slurp-file for reading files
\ Fifth's slurp-file reads entire file into allocated memory
\ ============================================================

\ ============================================================
\ Database Schema Setup
\ ============================================================

: init-problems-db ( -- )
  \ Create problems table if not exists
  \ Using single quotes for SQL in shell command
  str-reset
  s" sqlite3 " str+
  problems-db str+
  s"  'CREATE TABLE IF NOT EXISTS problems (id TEXT PRIMARY KEY, name TEXT NOT NULL, description TEXT NOT NULL, signature TEXT, test_code TEXT NOT NULL, difficulty TEXT, category TEXT)'" str+
  str$ system drop ;

: init-results-db ( -- )
  \ Create runs and results tables in one command
  str-reset
  s" sqlite3 " str+
  results-db str+
  s"  'CREATE TABLE IF NOT EXISTS runs (id INTEGER PRIMARY KEY AUTOINCREMENT, timestamp TEXT, model TEXT, prompt_variant TEXT); CREATE TABLE IF NOT EXISTS results (id INTEGER PRIMARY KEY AUTOINCREMENT, run_id INTEGER, problem_id TEXT, sample_num INTEGER, passed INTEGER)'" str+
  str$ system drop ;

: init-dbs ( -- )
  \ Note: Fifth's system word may have issues with multiple calls
  \ Combining operations where possible
  str-reset
  s" sqlite3 " str+
  problems-db str+
  s"  'CREATE TABLE IF NOT EXISTS problems (id TEXT PRIMARY KEY, name TEXT, description TEXT, signature TEXT, test_code TEXT, difficulty TEXT, category TEXT)' && sqlite3 " str+
  results-db str+
  s"  'CREATE TABLE IF NOT EXISTS runs (id INTEGER PRIMARY KEY AUTOINCREMENT, timestamp TEXT, model TEXT, prompt_variant TEXT); CREATE TABLE IF NOT EXISTS results (id INTEGER PRIMARY KEY AUTOINCREMENT, run_id INTEGER, problem_id TEXT, sample_num INTEGER, passed INTEGER)'" str+
  str$ system drop ;

\ ============================================================
\ Sandbox Setup
\ ============================================================

: setup-sandbox ( -- )
  \ Create isolated execution directory
  str-reset
  s" mkdir -p " str+
  sandbox-dir str+
  str$ system drop ;

: clear-sandbox ( -- )
  \ Clean sandbox between runs
  str-reset
  s" rm -f " str+
  sandbox-dir str+
  s" /*.fs " str+
  sandbox-dir str+
  s" /*.out " str+
  sandbox-dir str+
  s" /*.err" str+
  str$ system drop ;

\ ============================================================
\ Problem Management
\ ============================================================

: count-problems ( -- n )
  \ Count problems in database
  problems-db s" SELECT COUNT(*) FROM problems" sql-count ;

: list-problems ( -- )
  \ Display all problems
  s" Problems in benchmark:" type cr
  s" ======================" type cr
  problems-db s" SELECT id, name, difficulty, category FROM problems ORDER BY id"
  sql-exec sql-open
  begin sql-row? while
    dup 0> if
      2dup 0 sql-field type s"  - " type
      2dup 1 sql-field type s"  [" type
      2dup 2 sql-field type s" , " type
      3 sql-field type s" ]" type cr
    else 2drop then
  repeat 2drop
  sql-close ;

: get-problem-desc ( id$ -- desc$ )
  \ Fetch problem description by ID
  str-reset
  s" SELECT description FROM problems WHERE id='" str+
  str+
  s" '" str+
  str$ problems-db swap sql-exec
  sql-open
  sql-row? if
    dup 0> if
      0 sql-field
    else 2drop s" " then
  else s" " then
  sql-close ;

: get-problem-test ( id$ -- test$ )
  \ Fetch problem test code by ID
  str-reset
  s" SELECT test_code FROM problems WHERE id='" str+
  str+
  s" '" str+
  str$ problems-db swap sql-exec
  sql-open
  sql-row? if
    dup 0> if
      0 sql-field
    else 2drop s" " then
  else s" " then
  sql-close ;

: get-problem-sig ( id$ -- sig$ )
  \ Fetch problem signature by ID
  str-reset
  s" SELECT signature FROM problems WHERE id='" str+
  str+
  s" '" str+
  str$ problems-db swap sql-exec
  sql-open
  sql-row? if
    dup 0> if
      0 sql-field
    else 2drop s" " then
  else s" " then
  sql-close ;

\ ============================================================
\ Prompt Generation
\ ============================================================

\ Prompt template buffer
2048 constant prompt-max
create prompt-buf prompt-max allot
variable prompt-len

: prompt-reset ( -- )
  0 prompt-len ! ;

: prompt+ ( addr u -- )
  \ Append to prompt buffer
  dup prompt-len @ + prompt-max < if
    prompt-buf prompt-len @ + swap dup prompt-len +! move
  else
    2drop
  then ;

: prompt$ ( -- addr u )
  prompt-buf prompt-len @ ;

: build-code-prompt ( problem-id$ -- prompt$ )
  \ Build prompt for code generation
  prompt-reset
  s\" Generate Forth code for the following problem.\n\n" prompt+
  s" Problem: " prompt+
  2dup get-problem-desc prompt+
  s\" \n\nStack signature: " prompt+
  2dup get-problem-sig prompt+
  s\" \n\nRequirements:\n" prompt+
  s\" 1. Use standard Forth words\n" prompt+
  s\" 2. Include stack comments ( before -- after )\n" prompt+
  s\" 3. No dynamic allocation (allocate/free)\n" prompt+
  s\" 4. Return ONLY the Forth code, no explanation\n" prompt+
  s\" \nCode:\n" prompt+
  2drop
  prompt$ ;

: build-cot-prompt ( problem-id$ -- prompt$ )
  \ Build chain-of-thought prompt
  prompt-reset
  s\" Let's solve this Forth programming problem step by step.\n\n" prompt+
  s" Problem: " prompt+
  2dup get-problem-desc prompt+
  s\" \n\nStack signature: " prompt+
  2dup get-problem-sig prompt+
  s\" \n\nThink through:\n" prompt+
  s\" 1. What inputs does this word receive?\n" prompt+
  s\" 2. What should remain on the stack after?\n" prompt+
  s\" 3. What intermediate steps are needed?\n" prompt+
  s\" 4. What edge cases should be handled?\n" prompt+
  s\" \nAfter your analysis, provide ONLY the Forth code:\n" prompt+
  2drop
  prompt$ ;

\ ============================================================
\ LLM API Calls (Shell-out Pattern)
\ ============================================================

variable current-tokens-in
variable current-tokens-out

: escape-for-json ( addr u -- addr' u' )
  \ Escape string for JSON embedding
  \ Uses secondary buffer to avoid corrupting primary
  str2-reset
  0 ?do
    dup i + c@
    case
      [char] " of s\" \\\"" str2+ endof
      [char] \ of s" \\\\" str2+ endof
      10       of s\" \\n" str2+ endof   \ newline
      13       of s\" \\r" str2+ endof   \ carriage return
      9        of s\" \\t" str2+ endof   \ tab
      dup str2-char
    endcase
  loop drop
  str2$ ;

: call-anthropic ( prompt$ -- response$ success? )
  \ Call Claude API via curl, capture response
  escape-for-json

  \ Build curl command
  str-reset
  s" curl -s https://api.anthropic.com/v1/messages " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'x-api-key: '" str+
  get-api-key str+
  s" ' " str+
  s" -H 'anthropic-version: 2023-06-01' " str+
  s" -d '{" str+
  s\" \"model\": \"" str+
  default-model str+
  s\" \"," str+
  s\" \"max_tokens\": 2048," str+
  s\" \"messages\": [{\"role\": \"user\", \"content\": \"" str+
  str+  \ escaped prompt
  s\" \"}]" str+
  s" }' > " str+
  sandbox-dir str+
  s" /response.json 2>&1" str+

  str$ system drop

  \ Read response from file using slurp-file
  str-reset
  sandbox-dir str+
  s" /response.json" str+
  str$ slurp-file
  dup 0= if 2drop s" " false exit then
  true ;

: call-openai ( prompt$ -- response$ success? )
  \ Call OpenAI API via curl
  escape-for-json

  str-reset
  s" curl -s https://api.openai.com/v1/chat/completions " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'Authorization: Bearer '" str+
  get-openai-key str+
  s" ' " str+
  s" -d '{" str+
  s\" \"model\": \"gpt-4\"," str+
  s\" \"messages\": [{\"role\": \"user\", \"content\": \"" str+
  str+
  s\" \"}]" str+
  s" }' > " str+
  sandbox-dir str+
  s" /response.json 2>&1" str+

  str$ system drop

  \ Read response using slurp-file
  str-reset
  sandbox-dir str+
  s" /response.json" str+
  str$ slurp-file
  dup 0= if 2drop s" " false exit then
  true ;

\ ============================================================
\ Response Parsing (via jq)
\ ============================================================

: extract-claude-content ( response$ -- code$ )
  \ Extract text content from Claude response using jq
  \ Save response to temp file, run jq, read result
  str-reset
  sandbox-dir str+
  s" /response.json" str+
  str$ w/o create-file throw >r
  r@ write-file throw
  r> close-file throw

  str-reset
  s" jq -r '.content[0].text // empty' " str+
  sandbox-dir str+
  s" /response.json > " str+
  sandbox-dir str+
  s" /code.txt 2>/dev/null" str+
  str$ system drop

  \ Read extracted code using slurp-file
  str-reset
  sandbox-dir str+
  s" /code.txt" str+
  str$ slurp-file ;

: extract-openai-content ( response$ -- code$ )
  \ Extract content from OpenAI response
  str-reset
  sandbox-dir str+
  s" /response.json" str+
  str$ w/o create-file throw >r
  r@ write-file throw
  r> close-file throw

  str-reset
  s" jq -r '.choices[0].message.content // empty' " str+
  sandbox-dir str+
  s" /response.json > " str+
  sandbox-dir str+
  s" /code.txt 2>/dev/null" str+
  str$ system drop

  \ Read extracted code using slurp-file
  str-reset
  sandbox-dir str+
  s" /code.txt" str+
  str$ slurp-file ;

: extract-code-block ( text$ -- code$ )
  \ Extract code from markdown fence if present
  \ Look for ```forth or ``` and extract contents
  \ For now, return as-is (would use sed/awk)
  ;

\ ============================================================
\ Code Execution (Sandboxed)
\ ============================================================

variable exec-start-ms
variable exec-end-ms

: get-ms ( -- ms )
  \ Get current time in milliseconds (via date)
  str-reset
  s" date +%s%3N" str+
  str$ system drop
  \ Would need to capture output properly
  0 ;  \ placeholder

: write-line-to ( addr u fid -- )
  \ Write string followed by newline to file
  dup >r write-file drop
  10 r> emit-file ;

: write-test-file ( code$ test$ -- )
  \ Write combined code + test to sandbox file
  str-reset
  sandbox-dir str+
  s" /test.fs" str+
  str$ w/o create-file throw >r

  \ Write the generated code
  r@ write-line-to

  \ Write newline separator
  s" " r@ write-line-to

  \ Write test harness
  s" \ --- Test Harness ---" r@ write-line-to

  \ Write the test code
  r@ write-line-to

  \ Write test execution
  s\" test-result @ if .\" PASS\" else .\" FAIL\" then cr bye" r@ write-line-to

  r> close-file throw ;

: run-sandboxed ( -- output$ exitcode )
  \ Execute test file with timeout
  str-reset
  s" cd " str+
  sandbox-dir str+
  s"  && timeout " str+
  default-timeout-ms 1000 / 0 <# #s #> str+
  s" s ~/fifth/fifth test.fs > output.txt 2> error.txt ; echo $?" str+
  str$ system

  \ Read output using slurp-file
  str-reset
  sandbox-dir str+
  s" /output.txt" str+
  str$ slurp-file
  0 ;

: contains-pass? ( addr u -- flag )
  \ Check if string contains "PASS" by searching character by character
  dup 4 < if 2drop false exit then  \ string too short
  4 - 1+ 0 ?do
    dup i + c@ [char] P = if
      dup i 1+ + c@ [char] A = if
        dup i 2 + + c@ [char] S = if
          dup i 3 + + c@ [char] S = if
            drop true unloop exit
          then
        then
      then
    then
  loop
  drop false ;

: check-result ( output$ -- passed? )
  \ Check if output contains PASS
  contains-pass? ;

\ ============================================================
\ Result Recording
\ ============================================================

variable current-run-id

: create-run ( model$ prompt-variant$ samples -- run-id )
  \ Create new run record, return ID
  str-reset
  s\" sqlite3 " str+
  results-db str+
  s\" \" INSERT INTO runs (model, prompt_variant, samples_per_problem) VALUES ('" str+
  2>r 2swap str+  \ model
  s" ', '" str+
  2r> 2swap str+  \ prompt_variant
  s" ', " str+
  0 <# #s #> str+  \ samples
  s\" ); SELECT last_insert_rowid();\"" str+
  str$ system drop
  \ Would need to capture the ID
  1 ;

: record-result ( run-id problem-id$ sample-num code$ passed error-type$ exec-ms tokens-in tokens-out -- )
  \ Record single evaluation result
  str-reset
  s\" sqlite3 " str+
  results-db str+
  s\" \" INSERT INTO results (run_id, problem_id, sample_num, passed, error_type, execution_ms, tokens_in, tokens_out) VALUES (" str+

  \ run_id
  >r >r >r >r >r >r 2>r 2>r
  0 <# #s #> str+
  s" , '" str+

  \ problem_id
  2r> str+
  s" ', " str+

  \ sample_num
  2r> 0 <# #s #> str+
  s" , " str+

  \ passed (0 or 1)
  r> if s" 1" else s" 0" then str+
  s" , '" str+

  \ error_type
  r> r> str+
  s" ', " str+

  \ execution_ms
  r> 0 <# #s #> str+
  s" , " str+

  \ tokens_in
  r> 0 <# #s #> str+
  s" , " str+

  \ tokens_out
  r> 0 <# #s #> str+

  s\" );\"" str+
  str$ system drop ;

\ ============================================================
\ Pass@k Calculation
\ ============================================================

: eval-factorial ( n -- n! )
  dup 2 < if drop 1 exit then
  dup 1- recurse * ;

: combinations ( n k -- result )
  \ Calculate binomial coefficient C(n,k)
  2dup - 0< if 2drop 0 exit then
  2dup = if 2drop 1 exit then
  over 0= if 2drop 1 exit then
  dup 0= if 2drop 1 exit then
  over eval-factorial
  over eval-factorial
  rot rot - eval-factorial
  * / ;

: pass-at-k ( total-n correct-c k -- pass@k*1000 )
  \ Calculate pass@k, returns result * 1000 for integer math
  \ pass@k = 1 - C(n-c, k) / C(n, k)
  >r 2dup - r@ combinations  \ C(n-c, k)
  -rot r> combinations        \ C(n, k)
  dup 0= if 2drop 1000 exit then
  1000 swap */               \ (C(n-c,k) * 1000) / C(n,k)
  1000 swap - ;              \ 1000 - ratio

: .pass-at-k ( pass@k*1000 -- )
  \ Print pass@k as percentage (e.g., 750 -> 75.0%)
  10 /mod   \ split into integer and decimal part
  swap      \ now: int-part decimal-part
  n>str type [char] . emit
  n>str type s" %" type ;

\ ============================================================
\ Evaluation Loop
\ ============================================================

variable total-passed
variable total-failed
variable total-samples

: evaluate-problem-once ( problem-id$ sample-num -- passed? )
  \ Generate and test code for one problem, one sample
  2>r

  \ Build prompt
  2dup build-code-prompt

  \ Call LLM
  call-anthropic if
    extract-claude-content
  else
    2drop s" \ Error calling API"
  then

  \ Get test code
  2over get-problem-test

  \ Write test file
  2swap write-test-file

  \ Execute
  run-sandboxed drop
  check-result

  \ Record result
  current-run-id @
  2r> 2swap
  rot
  s" " 0 0 0  \ error-type, exec-ms, tokens
  record-result

  ;

: evaluate-problem ( problem-id$ num-samples -- passed-count )
  \ Evaluate a single problem with multiple samples
  0 >r  \ passed count
  0 ?do
    2dup i 1+ evaluate-problem-once
    if r> 1+ >r then
  loop
  2drop r> ;

: evaluate-all ( num-samples -- )
  \ Evaluate all problems in the database
  s" Evaluating all problems..." type cr
  s" ==========================" type cr

  0 total-passed !
  0 total-failed !
  0 total-samples !

  \ Create run record
  default-model s" default" rot create-run current-run-id !

  \ Iterate through problems
  problems-db s" SELECT id FROM problems ORDER BY id" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      2dup 0 sql-field
      s" Evaluating: " type 2dup type cr

      \ Copy problem ID (avoid buffer corruption)
      2dup

      \ Evaluate
      2swap drop  \ original num-samples on stack
      evaluate-problem

      dup total-passed +!
      2swap drop total-samples @ + total-samples !

      s"   Passed: " type . s" /" type total-samples @ . cr
      2drop
    else 2drop then
  repeat 2drop
  sql-close

  cr s" === Summary ===" type cr
  s" Total samples: " type total-samples @ . cr
  s" Total passed: " type total-passed @ . cr
  s" Pass@1: " type
  total-samples @ total-passed @ 1 pass-at-k .pass-at-k cr ;

\ ============================================================
\ Reporting
\ ============================================================

: report-run ( run-id -- )
  \ Generate report for a specific run
  s" Run #" type dup . cr
  s" ========" type cr

  str-reset
  s\" sqlite3 -column -header " str+
  results-db str+
  s\" \" SELECT problem_id, COUNT(*) as samples, SUM(passed) as passed, " str+
  s\" ROUND(100.0 * SUM(passed) / COUNT(*), 1) as pass_rate " str+
  s\" FROM results WHERE run_id=" str+
  0 <# #s #> str+
  s\" GROUP BY problem_id ORDER BY problem_id;\"" str+
  str$ system drop ;

: report-latest ( -- )
  \ Report on most recent run
  str-reset
  s\" sqlite3 " str+
  results-db str+
  s\" \" SELECT MAX(id) FROM runs;\"" str+
  str$ system drop
  \ Would capture and pass to report-run
  1 report-run ;

: report-comparison ( run1 run2 -- )
  \ Compare two runs side by side
  s" Comparison: Run #" type over . s"  vs Run #" type dup . cr
  s" ============================================" type cr

  str-reset
  s\" sqlite3 -column -header " str+
  results-db str+
  s\" \" SELECT " str+
  s\" r1.problem_id, " str+
  s\" ROUND(100.0 * SUM(r1.passed) / COUNT(r1.id), 1) as run1_pass, " str+
  s\" ROUND(100.0 * SUM(r2.passed) / COUNT(r2.id), 1) as run2_pass " str+
  s\" FROM results r1 " str+
  s\" JOIN results r2 ON r1.problem_id = r2.problem_id " str+
  s\" WHERE r1.run_id=" str+
  swap 0 <# #s #> str+
  s\" AND r2.run_id=" str+
  0 <# #s #> str+
  s\" GROUP BY r1.problem_id;\"" str+
  str$ system drop ;

: report-all-runs ( -- )
  \ List all runs with summary stats
  s" All Evaluation Runs" type cr
  s" ===================" type cr

  str-reset
  s\" sqlite3 -column -header " str+
  results-db str+
  s\" \" SELECT r.id, r.timestamp, r.model, r.prompt_variant, " str+
  s\" COUNT(res.id) as samples, " str+
  s\" SUM(res.passed) as passed, " str+
  s\" ROUND(100.0 * SUM(res.passed) / COUNT(res.id), 1) as pass_rate " str+
  s\" FROM runs r " str+
  s\" LEFT JOIN results res ON r.id = res.run_id " str+
  s\" GROUP BY r.id ORDER BY r.id DESC;\"" str+
  str$ system drop ;

\ ============================================================
\ A/B Testing
\ ============================================================

: ab-test ( prompt-a$ prompt-b$ num-samples -- )
  \ Run A/B comparison between two prompt variants
  s" A/B Test: Comparing prompts" type cr
  s" ===========================" type cr

  \ Would create two runs with different prompts
  \ Then call report-comparison
  drop 2drop 2drop
  s" A/B testing not fully implemented" type cr ;

\ ============================================================
\ Sample Problems (Bootstrap)
\ ============================================================

: add-sample-problems ( -- )
  \ Add basic problems for testing the harness
  s" Adding sample problems..." type cr

  \ Problem: stack-dup
  str-reset
  s\" sqlite3 " str+
  problems-db str+
  s\" \" INSERT OR REPLACE INTO problems VALUES (" str+
  s" 'stack-dup', " str+
  s" 'Stack DUP', " str+
  s" 'Implement DUP - duplicate the top stack item', " str+
  s" '( n -- n n )', " str+
  s" 'variable test-result true test-result ! : check 5 my-dup 5 = swap 5 = and test-result @ and test-result ! ; check', " str+
  s" 'easy', " str+
  s\" 'stack');\"" str+
  str$ system drop

  \ Problem: double
  str-reset
  s\" sqlite3 " str+
  problems-db str+
  s\" \" INSERT OR REPLACE INTO problems VALUES (" str+
  s" 'double', " str+
  s" 'Double a Number', " str+
  s" 'Create a word that doubles the top of stack', " str+
  s" '( n -- 2n )', " str+
  s" 'variable test-result true test-result ! : check 5 my-double 10 = test-result @ and test-result ! 0 my-double 0= test-result @ and test-result ! ; check', " str+
  s" 'easy', " str+
  s\" 'math');\"" str+
  str$ system drop

  \ Problem: max
  str-reset
  s\" sqlite3 " str+
  problems-db str+
  s\" \" INSERT OR REPLACE INTO problems VALUES (" str+
  s" 'max', " str+
  s" 'Maximum of Two', " str+
  s" 'Return the larger of two numbers', " str+
  s" '( a b -- max )', " str+
  s" 'variable test-result true test-result ! : check 3 5 my-max 5 = test-result @ and test-result ! 7 2 my-max 7 = test-result @ and test-result ! ; check', " str+
  s" 'easy', " str+
  s\" 'math');\"" str+
  str$ system drop

  \ Problem: factorial
  str-reset
  s\" sqlite3 " str+
  problems-db str+
  s\" \" INSERT OR REPLACE INTO problems VALUES (" str+
  s" 'factorial', " str+
  s" 'Factorial', " str+
  s" 'Compute n! for non-negative integers', " str+
  s" '( n -- n! )', " str+
  s" 'variable test-result true test-result ! : check 0 my-factorial 1 = test-result @ and test-result ! 5 my-factorial 120 = test-result @ and test-result ! ; check', " str+
  s" 'medium', " str+
  s\" 'math');\"" str+
  str$ system drop

  \ Problem: abs
  str-reset
  s\" sqlite3 " str+
  problems-db str+
  s\" \" INSERT OR REPLACE INTO problems VALUES (" str+
  s" 'abs', " str+
  s" 'Absolute Value', " str+
  s" 'Return the absolute value of a number', " str+
  s" '( n -- |n| )', " str+
  s" 'variable test-result true test-result ! : check 5 my-abs 5 = test-result @ and test-result ! -3 my-abs 3 = test-result @ and test-result ! 0 my-abs 0= test-result @ and test-result ! ; check', " str+
  s" 'easy', " str+
  s\" 'math');\"" str+
  str$ system drop

  s" Sample problems added." type cr ;

\ ============================================================
\ CLI Interface
\ ============================================================

: usage ( -- )
  s" Eval Harness - LLM Code Generation Benchmarking" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth eval-harness/main.fs init           - Initialize databases" type cr
  s"   ./fifth eval-harness/main.fs problems       - List all problems" type cr
  s"   ./fifth eval-harness/main.fs add-samples    - Add sample problems" type cr
  s"   ./fifth eval-harness/main.fs run [n]        - Run evaluation (n samples, default 5)" type cr
  s"   ./fifth eval-harness/main.fs report         - Show latest run report" type cr
  s"   ./fifth eval-harness/main.fs report-all     - Show all runs" type cr
  s"   ./fifth eval-harness/main.fs compare <a> <b> - Compare two runs" type cr
  s" " type cr
  s" Environment:" type cr
  s"   ANTHROPIC_API_KEY - Required for Claude evaluation" type cr
  s"   OPENAI_API_KEY    - Required for GPT evaluation" type cr ;

: cmd-init ( -- )
  s" Initializing evaluation harness..." type cr
  init-dbs
  setup-sandbox
  s" Done. Run add-samples to add benchmark problems." type cr ;

: cmd-problems ( -- )
  list-problems ;

: cmd-add-samples ( -- )
  add-sample-problems
  list-problems ;

: cmd-run ( n -- )
  s" Starting evaluation run with " type dup . s"  samples per problem..." type cr
  evaluate-all ;

: cmd-report ( -- )
  report-latest ;

: cmd-report-all ( -- )
  report-all-runs ;

: cmd-compare ( a b -- )
  report-comparison ;

\ --- Interactive Commands ---
\ Run these from the REPL or modify the auto-run section below

: help ( -- )
  usage ;

: init ( -- )
  cmd-init ;

: problems ( -- )
  cmd-problems ;

: add-samples ( -- )
  cmd-add-samples ;

: run ( -- )
  default-samples cmd-run ;

: run-n ( n -- )
  cmd-run ;

: report ( -- )
  cmd-report ;

: report-all ( -- )
  cmd-report-all ;

\ --- Auto-run: Show usage and initialize ---
\ Uncomment the line you want to auto-execute

usage
\ To get started, type: init add-samples
\ Then type: run  (to evaluate with LLM)
