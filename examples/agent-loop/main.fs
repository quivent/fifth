\ fifth/examples/agent-loop/main.fs
\ Autonomous Agent Loop Framework
\ Implements ReAct pattern: Think -> Act -> Observe -> Repeat
\
\ Usage:
\   Set AGENT_TASK environment variable, then run:
\   AGENT_TASK="Find all TODO comments" ./fifth examples/agent-loop/main.fs
\
\   Or for dry run:
\   AGENT_MODE=dry AGENT_TASK="test" ./fifth examples/agent-loop/main.fs
\
\   Or just run for tools list:
\   ./fifth examples/agent-loop/main.fs

require ~/fifth/lib/core.fs

\ ============================================================
\ Configuration Constants
\ ============================================================

25 constant max-iterations       \ Maximum agent loop iterations
4096 constant max-tokens         \ LLM max response tokens
30000 constant tool-timeout-ms   \ Tool execution timeout
3 constant max-retries           \ Max retry attempts
100000 constant max-context      \ Approximate context token limit

\ Action types returned by parser
0 constant ACTION-TOOL           \ Tool call requested
1 constant ACTION-DONE           \ Agent completed task
2 constant ACTION-ERROR          \ Parse error

\ ============================================================
\ Error/Success Messages
\ ============================================================

: json-error-missing-cmd   s" error: missing cmd" ;
: json-error-cmd-failed    s" error: command failed" ;
: json-error-missing-path  s" error: missing path" ;
: json-error-file-not-found s" error: file not found" ;
: json-error-missing-content s" error: missing content" ;
: json-error-cannot-create s" error: cannot create file" ;
: json-error-missing-pattern s" error: missing pattern" ;
: json-error-ls-failed     s" error: ls failed" ;
: json-error-unknown-tool  s" error: unknown tool" ;
: json-success-written     s" success: file written" ;

\ ============================================================
\ Response Buffer (for capturing shell output)
\ ============================================================

16384 constant response-max
create response-buf response-max allot
variable response-len

s" /tmp/fifth-agent-response.txt" 2constant response-file

\ ============================================================
\ JSON Escape Buffer (separate from str2 to avoid conflicts)
\ ============================================================

8192 constant escape-buf-max
create escape-buf escape-buf-max allot
variable escape-len

: escape-reset ( -- ) 0 escape-len ! ;
: escape-char ( c -- )
  escape-len @ escape-buf-max < if
    escape-buf escape-len @ + c!
    1 escape-len +!
  else drop then ;
: escape$ ( -- addr u ) escape-buf escape-len @ ;

: capture-output ( cmd$ -- result$ success? )
  \ Execute command, redirect to file, read back
  str-reset
  str+
  s"  > " str+
  response-file str+
  s"  2>&1" str+
  str$ system drop
  \ Read result file
  response-file slurp-file
  dup 0= if
    2drop s" " false exit
  then
  dup response-max > if drop response-max then
  dup response-len !
  response-buf swap move
  response-buf response-len @ true ;

\ ============================================================
\ Tool Registry
\ ============================================================

16 constant tools-max
variable tools-count

32 128 + cell+ constant tool-entry-size
create tools-buf tools-max tool-entry-size * allot

: tool-entry ( n -- addr )
  tool-entry-size * tools-buf + ;

: tool-name@ ( entry-addr -- addr u )
  dup 32 0 ?do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 32 ;

: tool-desc@ ( entry-addr -- addr u )
  32 + dup 128 0 ?do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 128 ;

: tool-xt@ ( entry-addr -- xt )
  32 128 + + @ ;

: tool-name! ( addr u entry-addr -- )
  \ Store name at entry, null-terminate
  \ move expects ( src dest n -- )
  -rot                        \ entry-addr src-addr src-len
  dup 31 > if drop 31 then    \ entry-addr src-addr len
  >r                          \ entry-addr src-addr | R: len
  over                        \ entry-addr src-addr entry-addr
  r@ move                     \ copy len bytes: src -> entry
  r> + 0 swap c! ;            \ null at entry+len

: tool-desc! ( addr u entry-addr -- )
  \ Store desc at entry+32, null-terminate
  32 + -rot                   \ (entry+32) src-addr src-len
  dup 127 > if drop 127 then  \ max 127
  >r                          \ (entry+32) src-addr | R: len
  over                        \ (entry+32) src-addr (entry+32)
  r@ move                     \ copy
  r> + 0 swap c! ;            \ null-terminate

: tool-xt! ( xt entry-addr -- )
  32 128 + + ! ;

: tool: ( name$ desc$ xt -- )
  tools-count @ tools-max < 0= if
    drop 2drop 2drop
    ." Error: Tool registry full" cr exit
  then
  tools-count @ tool-entry >r
  r@ tool-xt!
  r@ tool-desc!
  r> tool-name!
  1 tools-count +! ;

: tools-find ( name$ -- entry-addr | 0 )
  tools-count @ 0 ?do
    i tool-entry
    dup tool-name@ 2>r
    2over 2r> str= if
      2drop unloop exit
    then
    drop
  loop
  2drop 0 ;

: tools-list ( -- )
  ." Registered tools:" cr
  tools-count @ 0 ?do
    ."   " i tool-entry dup tool-name@ type
    ."  - " tool-desc@ type cr
  loop ;

\ ============================================================
\ Tool Implementations
\ ============================================================

: parse-json-field ( json$ field$ -- value$ )
  str-reset
  s" echo '" str+
  2swap str+
  s" ' | jq -r '." str+
  str+
  s"  // empty'" str+
  str$ capture-output drop ;

: tool-shell ( args$ -- result$ success? )
  s" cmd" parse-json-field
  dup 0= if 2drop json-error-missing-cmd false exit then
  capture-output if
    str-reset
    s" output: " str+
    response-buf response-len @ 1000 min str+
    str$ true
  else
    json-error-cmd-failed false
  then ;

: tool-read ( args$ -- result$ success? )
  s" path" parse-json-field
  dup 0= if 2drop json-error-missing-path false exit then
  slurp-file
  dup 0= if
    2drop json-error-file-not-found false exit
  then
  dup response-max > if drop response-max then
  dup response-len !
  response-buf swap move
  str-reset
  s" content: " str+
  response-buf response-len @ 2000 min str+
  str$ true ;

: tool-write ( args$ -- result$ success? )
  2dup s" path" parse-json-field
  dup 0= if 2drop 2drop json-error-missing-path false exit then
  2>r
  s" content" parse-json-field
  dup 0= if 2drop 2r> 2drop json-error-missing-content false exit then
  2r> w/o create-file if
    drop 2drop json-error-cannot-create false exit
  then
  >r
  r@ write-file drop
  r> close-file drop
  json-success-written true ;

: tool-grep ( args$ -- result$ success? )
  2dup s" pattern" parse-json-field
  dup 0= if 2drop 2drop json-error-missing-pattern false exit then
  2>r
  s" path" parse-json-field
  dup 0= if
    2drop 2r> 2drop
    json-error-missing-path false exit
  then
  str-reset
  s" grep -rn '" str+
  2r> str+
  s" ' " str+
  str+
  s"  2>/dev/null | head -20" str+
  str$ capture-output if
    str-reset
    s" matches: " str+
    response-buf response-len @ 2000 min str+
    str$ true
  else
    s" matches: (none)" true
  then ;

: tool-ls ( args$ -- result$ success? )
  s" path" parse-json-field
  dup 0= if 2drop s" ." then
  str-reset
  s" ls -la " str+
  str+
  s"  2>/dev/null" str+
  str$ capture-output if
    str-reset
    s" listing: " str+
    response-buf response-len @ 2000 min str+
    str$ true
  else
    json-error-ls-failed false
  then ;

: tool-find ( args$ -- result$ success? )
  2dup s" pattern" parse-json-field
  dup 0= if 2drop 2drop json-error-missing-pattern false exit then
  2>r
  s" path" parse-json-field
  dup 0= if 2drop s" ." then
  str-reset
  s" find " str+
  str+
  s"  -name '" str+
  2r> str+
  s" ' 2>/dev/null | head -30" str+
  str$ capture-output if
    str-reset
    s" files: " str+
    response-buf response-len @ 2000 min str+
    str$ true
  else
    s" files: (none)" true
  then ;

: register-tools ( -- )
  0 tools-count !
  s" shell" s" Execute shell command. Args: {cmd: string}" ['] tool-shell tool:
  s" read"  s" Read file contents. Args: {path: string}" ['] tool-read tool:
  s" write" s" Write to file. Args: {path: string, content: string}" ['] tool-write tool:
  s" grep"  s" Search in files. Args: {pattern: string, path: string}" ['] tool-grep tool:
  s" ls"    s" List directory. Args: {path: string}" ['] tool-ls tool:
  s" find"  s" Find files by name. Args: {pattern: string, path: string}" ['] tool-find tool: ;

\ ============================================================
\ Conversation History
\ ============================================================

64 constant history-max
1280 constant history-entry-size
create history-buf history-max history-entry-size * allot
variable history-count
variable history-head

: history-entry ( n -- addr )
  history-entry-size * history-buf + ;

: history-init ( -- )
  0 history-count !
  0 history-head !
  history-buf history-max history-entry-size * erase ;

: history-add ( role$ content$ -- )
  history-head @ history-entry >r
  2swap dup 15 > if drop 15 then
  r@ swap move
  dup 1255 > if drop 1255 then
  r> 16 + swap move
  history-head @ 1+ history-max mod history-head !
  history-count @ history-max < if
    1 history-count +!
  then ;

: history-role@ ( entry-addr -- addr u )
  dup 16 0 ?do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 16 ;

: history-content@ ( entry-addr -- addr u )
  16 + dup 1256 0 ?do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 1256 ;

: history-show ( -- )
  ." === Conversation History ===" cr
  history-count @ 0 ?do
    history-head @ history-count @ - i + history-max + history-max mod
    history-entry
    dup history-role@ ." [" type ." ] "
    history-content@ 80 min type
    history-content@ nip 80 > if ." ..." then
    cr
  loop
  ." ===========================" cr ;

\ ============================================================
\ Retry Logic
\ ============================================================

variable retry-count
variable retry-delay

: retry-init ( -- )
  0 retry-count !
  1000 retry-delay ! ;

: retry-next ( -- delay-ms )
  retry-delay @
  retry-delay @ dup + 30000 min retry-delay !
  1 retry-count +! ;

: sleep-ms ( ms -- )
  str-reset
  s" sleep " str+
  1000 / 0 <# #s #> str+
  str$ system drop ;

\ ============================================================
\ LLM API Interface
\ ============================================================

s" /tmp/fifth-agent-llm-request.json" 2constant request-file
s" /tmp/fifth-agent-llm-response.json" 2constant llm-response-file

: api-key$ ( -- addr u )
  s" ANTHROPIC_API_KEY" getenv ;

: escape-json-char ( c -- )
  dup 34 = if drop 92 escape-char 34 escape-char exit then
  dup 92 = if drop 92 escape-char 92 escape-char exit then
  dup 10 = if drop 92 escape-char 110 escape-char exit then
  dup 13 = if drop 92 escape-char 114 escape-char exit then
  dup 9 = if drop 92 escape-char 116 escape-char exit then
  escape-char ;

: escape-json ( addr u -- addr u )
  escape-reset
  0 ?do
    dup i + c@ escape-json-char
  loop
  drop
  escape$ ;

: build-messages-json ( task$ -- )
  s" [{" str+ 34 str-char s" role" str+ 34 str-char s" : " str+
  34 str-char s" user" str+ 34 str-char s" , " str+
  34 str-char s" content" str+ 34 str-char s" : " str+
  34 str-char escape-json str+ 34 str-char s" }" str+
  history-count @ 0 ?do
    s" , {" str+ 34 str-char s" role" str+ 34 str-char s" : " str+
    34 str-char
    history-head @ history-count @ - i + history-max + history-max mod
    history-entry
    dup history-role@ str+
    34 str-char s" , " str+
    34 str-char s" content" str+ 34 str-char s" : " str+
    34 str-char history-content@ escape-json str+ 34 str-char s" }" str+
  loop
  s" ]" str+ ;

: system-prompt$ ( -- addr u )
  str2-reset
  s" You are an autonomous coding agent with access to tools." str2+
  10 str2-char 10 str2-char
  s" AVAILABLE TOOLS:" str2+ 10 str2-char
  s" - shell: Execute shell command. Args: {cmd: command}" str2+ 10 str2-char
  s" - read: Read file contents. Args: {path: filepath}" str2+ 10 str2-char
  s" - write: Write to file. Args: {path: filepath, content: text}" str2+ 10 str2-char
  s" - grep: Search in files. Args: {pattern: regex, path: dir}" str2+ 10 str2-char
  s" - ls: List directory. Args: {path: dir}" str2+ 10 str2-char
  s" - find: Find files. Args: {pattern: glob, path: dir}" str2+ 10 str2-char
  10 str2-char
  s" RESPONSE FORMAT (strict JSON):" str2+ 10 str2-char
  s" To use a tool: {thought: reasoning, tool: name, args: {...}}" str2+ 10 str2-char
  s" When finished: {thought: summary, done: true, result: answer}" str2+ 10 str2-char
  10 str2-char
  s" RULES:" str2+ 10 str2-char
  s" 1. Always include thought field explaining your reasoning" str2+ 10 str2-char
  s" 2. Use exactly one tool per response" str2+ 10 str2-char
  s" 3. Wait for tool output before next action" str2+ 10 str2-char
  s" 4. Use done:true only when task is complete" str2+ 10 str2-char
  s" 5. Be concise in results" str2+
  str2$ ;

: build-request ( task$ -- )
  str-reset
  s" {" str+
  34 str-char s" model" str+ 34 str-char s" : " str+
  34 str-char s" claude-sonnet-4-20250514" str+ 34 str-char s" , " str+
  34 str-char s" max_tokens" str+ 34 str-char s" : " str+
  0 max-tokens <# #s #> str+ drop s" , " str+
  34 str-char s" system" str+ 34 str-char s" : " str+
  34 str-char system-prompt$ escape-json str+ 34 str-char s" , " str+
  34 str-char s" messages" str+ 34 str-char s" : " str+
  build-messages-json
  s" }" str+ ;

: write-request ( -- )
  request-file w/o create-file throw >r
  str$ r@ write-file throw
  r> close-file throw ;

: call-llm-api ( -- success? )
  str-reset
  s" curl -s -X POST 'https://api.anthropic.com/v1/messages' " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'x-api-key: " str+ api-key$ str+ s" ' " str+
  s" -H 'anthropic-version: 2023-06-01' " str+
  s" -d @" str+ request-file str+
  s"  > " str+ llm-response-file str+
  s"  2>&1" str+
  str$ system drop
  llm-response-file slurp-file
  dup 0= if 2drop false exit then
  nip 0> ;

: extract-content ( -- addr u )
  str-reset
  s" cat " str+ llm-response-file str+
  s"  | jq -r '.content[0].text // .error.message // empty'" str+
  str$ capture-output drop
  response-buf response-len @ ;

: call-llm ( task$ -- response$ success? )
  build-request
  write-request
  retry-init
  begin
    call-llm-api if
      extract-content
      dup 0> if true exit then
      2drop
    then
    retry-count @ max-retries < 0= if
      s" API call failed after retries" false exit
    then
    retry-next sleep-ms
  again ;

\ ============================================================
\ Response Parsing
\ ============================================================

256 constant parse-buf-size
create parse-tool-buf parse-buf-size allot
create parse-args-buf 1024 allot
create parse-thought-buf 512 allot
create parse-result-buf 2048 allot
variable parse-tool-len
variable parse-args-len
variable parse-thought-len
variable parse-result-len

: parse-field ( response$ field$ buf bufsize -- len )
  2>r 2>r
  str-reset
  s" echo '" str+
  str+
  s" ' | jq -r '." str+
  2r> str+
  s"  // empty'" str+
  str$ capture-output drop
  response-len @ 2r> drop min
  dup >r
  response-buf swap rot swap move
  r> ;

: parse-action ( response$ -- action-type )
  2dup s" thought" parse-thought-buf 511 parse-thought-len !

  2dup s" done" parse-result-buf 16
  dup 0> if
    parse-result-buf swap s" true" str= if
      s" result" parse-result-buf 2047 parse-result-len !
      ACTION-DONE exit
    then
  else
    drop
  then

  2dup s" tool" parse-tool-buf 255 parse-tool-len !
  parse-tool-len @ 0> if
    s" args" parse-args-buf 1023 parse-args-len !
    ACTION-TOOL exit
  then

  2drop
  ACTION-ERROR ;

: get-parsed-tool ( -- addr u ) parse-tool-buf parse-tool-len @ ;
: get-parsed-args ( -- addr u ) parse-args-buf parse-args-len @ ;
: get-parsed-thought ( -- addr u ) parse-thought-buf parse-thought-len @ ;
: get-parsed-result ( -- addr u ) parse-result-buf parse-result-len @ ;

\ ============================================================
\ Tool Dispatch
\ ============================================================

: dispatch-tool ( -- result$ success? )
  get-parsed-tool tools-find
  dup 0= if
    drop
    json-error-unknown-tool false exit
  then
  tool-xt@
  get-parsed-args
  swap execute ;

\ ============================================================
\ Stuck Detection
\ ============================================================

variable last-tool-hash
variable stuck-count

: simple-hash ( addr u -- hash )
  0 -rot
  0 ?do
    dup i + c@ +
    dup 5 lshift +
  loop
  drop ;

: check-stuck ( -- stuck? )
  get-parsed-tool get-parsed-args +
  simple-hash
  dup last-tool-hash @ = if
    1 stuck-count +!
    stuck-count @ 3 > if
      drop true exit
    then
  else
    last-tool-hash !
    0 stuck-count !
  then
  false ;

: reset-stuck ( -- )
  0 last-tool-hash !
  0 stuck-count ! ;

\ ============================================================
\ Main Agent Loop
\ ============================================================

variable iteration
variable agent-done

: show-thought ( -- )
  get-parsed-thought dup 0> if
    ." [Thought] " type cr
  else
    2drop
  then ;

: show-action ( action-type -- )
  dup ACTION-TOOL = if
    drop
    ." [Action] Tool: " get-parsed-tool type
    ."  Args: " get-parsed-args type cr
    exit
  then
  dup ACTION-DONE = if
    drop
    ." [Done] " get-parsed-result type cr
    exit
  then
  drop
  ." [Error] Failed to parse response" cr ;

: agent-think ( task$ -- response$ success? )
  ." --- Iteration " iteration @ . ." ---" cr
  call-llm ;

: agent-act ( response$ -- action-type )
  parse-action
  dup show-thought
  dup show-action
  dup ACTION-TOOL = if
    check-stuck if
      ." [Warning] Agent appears stuck, injecting hint" cr
      s" assistant" s" I seem to be repeating. Let me try differently."
      history-add
    then
    dispatch-tool if
      ." [Observe] " 60 min type
      dup 60 > if ." ..." then cr
      s" tool" 2swap history-add
    else
      ." [Error] Tool failed: " type cr
      s" tool" s" Tool execution failed" history-add
    then
  then ;

: agent-observe ( response$ -- )
  s" assistant" 2swap history-add ;

: agent-loop ( task$ -- result$ )
  0 iteration !
  false agent-done !
  reset-stuck
  history-init

  begin
    iteration @ max-iterations < agent-done @ 0= and
  while
    1 iteration +!
    2dup agent-think if
      2dup agent-observe
      agent-act
      dup ACTION-DONE = if
        drop true agent-done !
      else
        ACTION-ERROR = if
          ." [Error] Parse failed, retrying..." cr
        then
      then
    else
      ." [Error] LLM call failed: " type cr
      s" API error" true agent-done !
    then
  repeat

  2drop
  agent-done @ if
    get-parsed-result
  else
    ." [Limit] Max iterations reached" cr
    s" Incomplete: max iterations reached"
  then ;

\ ============================================================
\ CLI Interface
\ ============================================================

: cmd-run ( task$ -- )
  ." Starting agent loop..." cr
  ." Task: " 2dup type cr cr
  agent-loop
  cr ." === Final Result ===" cr
  type cr ;

: cmd-dry ( task$ -- )
  ." === Dry Run ===" cr
  ." Task: " 2dup type cr cr
  ." System prompt:" cr system-prompt$ type cr cr
  ." Request JSON:" cr
  build-request
  str$ type cr ;

: usage ( -- )
  ." Agent Loop Framework" cr
  ." " cr
  ." Set environment variables to run:" cr
  ."   AGENT_TASK  - The task to execute (required for run)" cr
  ."   AGENT_MODE  - 'run' (default), 'dry', or 'tools'" cr
  ."   ANTHROPIC_API_KEY - Required for run mode" cr
  ." " cr
  ." Examples:" cr
  ."   AGENT_TASK='Find all TODO comments' ./fifth examples/agent-loop/main.fs" cr
  ."   AGENT_MODE=dry AGENT_TASK='test' ./fifth examples/agent-loop/main.fs" cr
  ."   AGENT_MODE=tools ./fifth examples/agent-loop/main.fs" cr ;

: get-task$ ( -- addr u )
  s" AGENT_TASK" getenv ;

: get-mode$ ( -- addr u )
  s" AGENT_MODE" getenv
  dup 0= if 2drop s" run" then ;

: main ( -- )
  register-tools

  get-mode$

  2dup s" tools" str= if
    2drop
    tools-list
    exit
  then

  2dup s" dry" str= if
    2drop
    get-task$ dup 0= if
      2drop ." Error: AGENT_TASK not set" cr usage exit
    then
    cmd-dry
    exit
  then

  2dup s" run" str= if
    2drop
    get-task$ dup 0= if
      2drop
      ." Agent Loop Framework" cr
      ." " cr
      ." No AGENT_TASK set. Showing tools list:" cr cr
      tools-list
      cr
      ." To run the agent:" cr
      ."   AGENT_TASK='Your task here' ./fifth examples/agent-loop/main.fs" cr
      exit
    then
    api-key$ dup 0= if
      2drop 2drop
      ." Error: ANTHROPIC_API_KEY not set" cr exit
    then
    2drop
    cmd-run
    exit
  then

  2drop
  usage ;

main
bye
