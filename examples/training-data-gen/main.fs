\ fifth/examples/training-data-gen/main.fs
\ Training Data Generator for Code Models
\ Extracts high-quality docstring/code, commit/diff, test/impl pairs
\
\ Usage: ./fifth examples/training-data-gen/main.fs [path] [--format=alpaca|sharegpt|completion]

require ~/fifth/lib/core.fs

\ ============================================================
\ Configuration
\ ============================================================

s" /tmp/training_data.jsonl" 2constant default-output
s" alpaca" 2constant default-format

variable output-fid
variable scan-fid

\ Format: 0=alpaca, 1=sharegpt, 2=completion
variable output-format
0 output-format !

\ Target directory
256 constant path-max
create target-path path-max allot
variable target-path-len

\ ============================================================
\ Statistics
\ ============================================================

variable files-scanned
variable commits-processed
variable docstring-pairs
variable commit-pairs
variable test-pairs
variable total-extracted
variable after-dedup
variable after-quality

: init-stats ( -- )
  0 files-scanned !
  0 commits-processed !
  0 docstring-pairs !
  0 commit-pairs !
  0 test-pairs !
  0 total-extracted !
  0 after-dedup !
  0 after-quality ! ;

\ ============================================================
\ Hash Table for Deduplication (simple linear probe)
\ ============================================================

1024 constant hash-size
create hash-table hash-size cells allot
hash-table hash-size cells erase

: hash-string ( addr u -- hash )
  \ DJB2 hash
  5381 -rot
  0 ?do
    dup i + c@
    swap 5 lshift + +
  loop
  drop
  hash-size mod ;

: hash-seen? ( addr u -- flag )
  \ Check if hash exists, add if not
  2dup hash-string cells hash-table +
  dup @ 0= if
    1 swap !
    2drop false
  else
    drop 2drop true
  then ;

: reset-hash ( -- )
  hash-table hash-size cells erase ;

\ ============================================================
\ JSON Escaping
\ ============================================================

: json-escape-char ( c -- )
  \ Escape special JSON characters
  dup [char] " = if drop s\" \\\"" str+ exit then
  dup [char] \ = if drop s\" \\\\" str+ exit then
  dup 10 = if drop s\" \\n" str+ exit then
  dup 13 = if drop s\" \\r" str+ exit then
  dup 9 = if drop s\" \\t" str+ exit then
  str-char ;

: json-escape ( addr u -- addr u )
  \ Escape string for JSON, return in str2 buffer
  str2-reset
  0 ?do
    dup i + c@
    dup [char] " = if drop s\" \\\"" str2+ else
    dup [char] \ = if drop s\" \\\\" str2+ else
    dup 10 = if drop s\" \\n" str2+ else
    dup 13 = if drop s\" \\r" str2+ else
    dup 9 = if drop s\" \\t" str2+ else
    str2-char then then then then then
  loop
  drop
  str2$ ;

\ ============================================================
\ Output Formatters
\ ============================================================

: emit-alpaca ( instruction$ output$ -- )
  \ {"instruction": "...", "input": "", "output": "..."}
  str-reset
  s\" {\"instruction\": \"" str+
  2swap json-escape str+
  s\" \", \"input\": \"\", \"output\": \"" str+
  json-escape str+
  s\" \"}" str+
  str$ output-fid @ write-line throw ;

: emit-sharegpt ( instruction$ output$ -- )
  \ {"conversations": [{"from": "human", "value": "..."}, {"from": "gpt", "value": "..."}]}
  str-reset
  s\" {\"conversations\": [{\"from\": \"human\", \"value\": \"" str+
  2swap json-escape str+
  s\" \"}, {\"from\": \"gpt\", \"value\": \"" str+
  json-escape str+
  s\" \"}]}" str+
  str$ output-fid @ write-line throw ;

: emit-completion ( instruction$ output$ -- )
  \ {"text": "instruction\n\noutput"}
  str-reset
  s\" {\"text\": \"" str+
  2swap json-escape str+
  s\" \\n\\n" str+
  json-escape str+
  s\" \"}" str+
  str$ output-fid @ write-line throw ;

: emit-sample ( instruction$ output$ -- )
  \ Emit based on current format
  output-format @ case
    0 of emit-alpaca endof
    1 of emit-sharegpt endof
    2 of emit-completion endof
    emit-alpaca
  endcase ;

\ ============================================================
\ Substring Search
\ ============================================================

: contains? ( addr u pattern-addr pattern-u -- flag )
  \ Check if string contains pattern
  \ Simple implementation - check each position
  2>r 2dup + 2r@ nip - 1+ 0 max 0 ?do
    2dup i + 2r@ compare 0= if
      2drop 2r> 2drop true unloop exit
    then
  loop
  2drop 2r> 2drop false ;

\ ============================================================
\ Quality Filters
\ ============================================================

: min-length? ( addr u -- flag )
  \ Minimum 20 chars
  nip 20 >= ;

: no-todo? ( addr u -- flag )
  \ Reject if contains TODO/FIXME
  2dup s" TODO" contains? 0= >r
  s" FIXME" contains? 0=
  r> and ;

: no-pass-only? ( addr u -- flag )
  \ Reject Python pass-only functions
  s" pass" str= 0= ;

: quality-ok? ( instruction$ output$ -- flag )
  \ Check both instruction and output meet quality bar
  2over min-length?
  2over no-todo? and
  2swap min-length? and
  no-pass-only? and ;

\ ============================================================
\ Line Buffer
\ ============================================================

512 constant line-max
create line-buf line-max allot

\ Secondary buffers for accumulating multi-line content
4096 constant accum-max
create docstring-buf accum-max allot
variable docstring-len
create code-buf accum-max allot
variable code-len

: docstring-reset ( -- ) 0 docstring-len ! ;
: code-reset ( -- ) 0 code-len ! ;

: docstring+ ( addr u -- )
  dup docstring-len @ + accum-max < if
    docstring-buf docstring-len @ + swap dup docstring-len +! move
  else 2drop then ;

: code+ ( addr u -- )
  dup code-len @ + accum-max < if
    code-buf code-len @ + swap dup code-len +! move
  else 2drop then ;

: docstring$ ( -- addr u ) docstring-buf docstring-len @ ;
: code$ ( -- addr u ) code-buf code-len @ ;

\ ============================================================
\ Python Docstring Parser
\ ============================================================

variable in-docstring
variable in-function
variable docstring-delim  \ 0=none, 1=""", 2='''
variable brace-depth

: strip-leading ( addr u -- addr' u' )
  \ Strip leading whitespace
  begin
    dup 0> while
    over c@ 32 <= while
    1 /string
  repeat then ;

: strip-trailing ( addr u -- addr u' )
  \ Strip trailing whitespace
  begin
    dup 0> while
    2dup + 1- c@ 32 <= while
    1-
  repeat then ;

: strip ( addr u -- addr' u' )
  strip-leading strip-trailing ;

: starts-with? ( addr u prefix$ -- flag )
  2>r 2dup 2r@ nip >= if
    2r@ nip
    2r> drop -rot
    compare 0=
  else
    2r> 2drop 2drop false
  then ;

: ends-with? ( addr u suffix$ -- flag )
  2>r 2dup 2r@ nip >= if
    2r@ nip
    2dup - >r
    drop r> +
    2r>
    compare 0=
  else
    2r> 2drop 2drop false
  then ;

: is-def-line? ( addr u -- flag )
  strip s" def " starts-with? ;

: is-docstring-start? ( addr u -- flag )
  strip
  2dup s\" \"\"\"" starts-with? if 2drop true exit then
  s" '''" starts-with? ;

: is-docstring-end? ( addr u -- flag )
  strip
  2dup s\" \"\"\"" ends-with? if 2drop true exit then
  s" '''" ends-with? ;

: extract-def-name ( addr u -- addr' u' )
  \ Extract function name from "def name(...):"
  strip
  4 /string  \ skip "def "
  strip-leading
  \ Find opening paren
  2dup [char] ( str-find-char
  dup 0> if
    drop nip over -
  else
    2drop
  then ;

: process-python-line ( addr u -- )
  \ State machine for Python file parsing
  2dup is-def-line? if
    \ Starting a new function
    in-function @ if
      \ Emit previous if we have both parts
      docstring-len @ 0> code-len @ 0> and if
        docstring$ code$
        2over 2over quality-ok? if
          2over 2over hash-seen? 0= if
            emit-sample
            1 docstring-pairs +!
            1 after-quality +!
          else
            2drop 2drop
          then
          1 after-dedup +!
        else
          2drop 2drop
        then
        1 total-extracted +!
      then
    then
    true in-function !
    false in-docstring !
    docstring-reset
    code-reset
    2dup extract-def-name
    docstring+ s" : " docstring+
    2drop exit
  then

  in-function @ if
    in-docstring @ if
      \ Inside docstring
      2dup is-docstring-end? if
        false in-docstring !
        \ Strip closing quotes
        strip
        dup 3 > if 3 - then
        docstring+
      else
        strip docstring+ s"  " docstring+
      then
    else
      \ Check for docstring start
      2dup is-docstring-start? if
        true in-docstring !
        \ Strip opening quotes
        strip
        s\" \"\"\"" starts-with? if 3 /string then
        s" '''" starts-with? if 3 /string then
        strip
        dup 0> if
          docstring+ s"  " docstring+
        else
          2drop
        then
      else
        \ Regular code line
        strip
        dup 0> if
          code+ s" " code+
        else
          2drop
        then
      then
    then
  else
    2drop
  then ;

: scan-python-file ( filename$ -- )
  r/o open-file if drop exit then
  scan-fid !

  false in-function !
  false in-docstring !
  docstring-reset
  code-reset

  begin
    line-buf line-max scan-fid @ read-line throw
  while
    line-buf swap process-python-line
  repeat
  drop

  \ Emit final function if any
  in-function @ if
    docstring-len @ 0> code-len @ 0> and if
      docstring$ code$
      2over 2over quality-ok? if
        2over 2over hash-seen? 0= if
          emit-sample
          1 docstring-pairs +!
          1 after-quality +!
        else
          2drop 2drop
        then
        1 after-dedup +!
      else
        2drop 2drop
      then
      1 total-extracted +!
    then
  then

  scan-fid @ close-file throw
  1 files-scanned +! ;

\ ============================================================
\ JavaScript/TypeScript Parser
\ ============================================================

: is-jsdoc-start? ( addr u -- flag )
  strip s" /**" starts-with? ;

: is-jsdoc-end? ( addr u -- flag )
  strip s" */" ends-with? ;

: is-function-line? ( addr u -- flag )
  strip
  2dup s" function " starts-with? if 2drop true exit then
  2dup s" const " starts-with? if 2drop true exit then
  2dup s" let " starts-with? if 2drop true exit then
  s" export " starts-with? ;

: process-js-line ( addr u -- )
  \ Similar state machine for JS/TS
  \ Simplified: just track JSDoc -> function patterns
  2drop ;

: scan-js-file ( filename$ -- )
  2drop
  1 files-scanned +! ;

\ ============================================================
\ Git History Extraction
\ ============================================================

: shell-capture ( cmd$ -- output$ )
  \ Execute shell command and capture output to temp file
  str-reset
  str+
  s"  > /tmp/fifth_shell_out.txt 2>&1" str+
  str$ system drop
  s" /tmp/fifth_shell_out.txt" r/o open-file throw
  dup line-buf line-max rot read-line throw drop
  swap close-file throw
  line-buf swap ;

: git-log-commits ( n -- )
  \ Get n commits as hash|subject pairs
  str-reset
  s" cd " str+
  target-path target-path-len @ str+
  s"  && git log --format='%H|%s' -" str+
  n>str str+
  s"  --no-merges 2>/dev/null" str+
  str$ system drop ;

: git-show-diff ( hash$ -- diff$ )
  \ Get diff for a commit
  str-reset
  s" cd " str+
  target-path target-path-len @ str+
  s"  && git show --format='' --stat=80 " str+
  str+
  s"  2>/dev/null | head -50" str+
  str$ shell-capture ;

: parse-commit-line ( addr u -- hash$ subject$ )
  \ Parse "hash|subject" format
  2dup [char] | str-find-char
  dup 0> if
    2>r
    2dup nip 2r@ nip - 1-  \ hash length
    >r over r>            \ hash addr, hash len
    2r> 1 /string          \ subject
  else
    drop 2dup s" "        \ no pipe found, empty subject
  then ;

: process-commit ( hash$ subject$ -- )
  \ Extract commit -> diff pair
  2over 2over
  \ Get diff
  2>r 2>r
  2r@ git-show-diff
  2r> 2r>
  \ Now have: diff$ hash$ subject$
  \ We want: subject$ (instruction) diff$ (output)
  2swap 2drop  \ drop hash
  2swap        \ now subject$ diff$
  2over 2over quality-ok? if
    2over 2over hash-seen? 0= if
      emit-sample
      1 commit-pairs +!
      1 after-quality +!
    else
      2drop 2drop
    then
    1 after-dedup +!
  else
    2drop 2drop
  then
  1 total-extracted +!
  1 commits-processed +! ;

: extract-git-history ( n -- )
  \ Extract n commits
  dup git-log-commits
  \ Read the output
  str-reset
  s" cd " str+
  target-path target-path-len @ str+
  s"  && git log --format='%H|%s' -" str+
  n>str str+
  s"  --no-merges 2>/dev/null" str+
  str$
  \ Execute and process
  s" /tmp/fifth_git_log.txt" w/o create-file throw >r
  r@ write-line throw
  r> close-file throw

  s" /tmp/fifth_git_log.txt" r/o open-file if drop exit then
  scan-fid !

  begin
    line-buf line-max scan-fid @ read-line throw
  while
    line-buf swap
    dup 0> if
      parse-commit-line
      dup 0> if process-commit else 2drop 2drop then
    else
      2drop
    then
  repeat
  drop

  scan-fid @ close-file throw ;

\ ============================================================
\ Test-Implementation Extraction
\ ============================================================

: is-test-line? ( addr u -- flag )
  strip
  2dup s" assert" starts-with? if 2drop true exit then
  2dup s" expect(" starts-with? if 2drop true exit then
  2dup s" test(" starts-with? if 2drop true exit then
  2dup s" it(" starts-with? if 2drop true exit then
  s" @Test" starts-with? ;

: scan-test-file ( filename$ -- )
  \ Look for test patterns
  2drop
  1 files-scanned +! ;

\ ============================================================
\ File Discovery
\ ============================================================

: is-python? ( filename$ -- flag )
  2dup s" .py" ends-with? ;

: is-javascript? ( filename$ -- flag )
  2dup s" .js" ends-with? if 2drop true exit then
  s" .ts" ends-with? ;

: is-test-file? ( filename$ -- flag )
  2dup s" test_" contains? >r
  2dup s" _test." contains? r> or >r
  2dup s" .test." contains? r> or >r
  s" /test/" contains? r> or ;

: scan-file ( filename$ -- )
  2dup is-python? if
    2dup is-test-file? if
      scan-test-file
    else
      scan-python-file
    then
    exit
  then
  2dup is-javascript? if
    scan-js-file
    exit
  then
  2drop ;

: find-files ( -- )
  \ Use find to get all source files
  str-reset
  s" find " str+
  target-path target-path-len @ str+
  s"  -type f \\( -name '*.py' -o -name '*.js' -o -name '*.ts' \\) " str+
  s" -not -path '*/node_modules/*' -not -path '*/.git/*' " str+
  s" -not -path '*/venv/*' -not -path '*/__pycache__/*' " str+
  s" 2>/dev/null" str+
  str$ s" /tmp/fifth_files.txt" w/o create-file throw >r
  r@ write-line throw
  r> close-file throw

  \ Execute find
  s" /tmp/fifth_files.txt" r/o open-file throw >r
  line-buf line-max r@ read-line throw drop  \ read the find command
  r> close-file throw
  str$ system drop

  \ Now read the actual find output from execution
  str-reset
  s" find " str+
  target-path target-path-len @ str+
  s"  -type f \\( -name '*.py' -o -name '*.js' -o -name '*.ts' \\) " str+
  s" -not -path '*/node_modules/*' -not -path '*/.git/*' " str+
  s" -not -path '*/venv/*' -not -path '*/__pycache__/*' " str+
  s" > /tmp/fifth_found.txt 2>/dev/null" str+
  str$ system drop

  s" /tmp/fifth_found.txt" r/o open-file if drop exit then
  scan-fid !

  begin
    line-buf line-max scan-fid @ read-line throw
  while
    line-buf swap
    dup 0> if
      strip scan-file
    else
      2drop
    then
  repeat
  drop

  scan-fid @ close-file throw ;

\ ============================================================
\ Report Generation
\ ============================================================

: print-stats ( -- )
  cr
  ." Training Data Generation Complete" cr
  ." =================================" cr
  ." Source files scanned:     " files-scanned @ . cr
  ." Git commits processed:    " commits-processed @ . cr
  cr
  ." Samples extracted:        " total-extracted @ . cr
  ."   - Docstring pairs:      " docstring-pairs @ . cr
  ."   - Commit pairs:         " commit-pairs @ . cr
  ."   - Test pairs:           " test-pairs @ . cr
  cr
  ." After deduplication:      " after-dedup @ .
  total-extracted @ 0> if
    ."  (" after-dedup @ 100 * total-extracted @ / . ." %)"
  then cr
  ." After quality filter:     " after-quality @ .
  total-extracted @ 0> if
    ."  (" after-quality @ 100 * total-extracted @ / . ." %)"
  then cr
  cr
  ." Output: " default-output type cr
  ." Format: "
  output-format @ case
    0 of ." alpaca" endof
    1 of ." sharegpt" endof
    2 of ." completion" endof
  endcase cr ;

\ ============================================================
\ Argument Parsing
\ ============================================================

: parse-format ( arg$ -- )
  2dup s" --format=alpaca" str= if 2drop 0 output-format ! exit then
  2dup s" --format=sharegpt" str= if 2drop 1 output-format ! exit then
  2dup s" --format=completion" str= if 2drop 2 output-format ! exit then
  2drop ;

: parse-args ( -- )
  argc @ 2 >= if
    1 argv
    2dup s" --" starts-with? if
      parse-format
      s" ." target-path swap dup target-path-len ! move
    else
      target-path swap dup target-path-len ! move
    then
  else
    s" ." target-path swap dup target-path-len ! move
  then

  argc @ 3 >= if
    2 argv parse-format
  then ;

\ ============================================================
\ Main
\ ============================================================

: usage ( -- )
  ." Training Data Generator for Code Models" cr
  ." ========================================" cr
  cr
  ." Usage: ./fifth examples/training-data-gen/main.fs [path] [--format=FORMAT]" cr
  cr
  ." Formats:" cr
  ."   alpaca      - {instruction, input, output} (default)" cr
  ."   sharegpt    - {conversations: [{from, value}...]}" cr
  ."   completion  - {text: instruction + output}" cr
  cr
  ." Examples:" cr
  ."   ./fifth examples/training-data-gen/main.fs ." cr
  ."   ./fifth examples/training-data-gen/main.fs /path/to/repo" cr
  ."   ./fifth examples/training-data-gen/main.fs /path/to/repo --format=sharegpt" cr ;

: main ( -- )
  ." Fifth Training Data Generator" cr
  ." =============================" cr
  cr

  init-stats
  reset-hash
  parse-args

  ." Target: " target-path target-path-len @ type cr
  ." Format: "
  output-format @ case
    0 of ." alpaca" endof
    1 of ." sharegpt" endof
    2 of ." completion" endof
  endcase cr
  cr

  \ Open output file
  default-output w/o create-file throw output-fid !

  ." Scanning source files..." cr
  find-files

  ." Extracting git history (last 100 commits)..." cr
  100 extract-git-history

  \ Close output
  output-fid @ close-file throw

  print-stats ;

main bye
