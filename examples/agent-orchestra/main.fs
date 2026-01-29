\ fifth/examples/agent-orchestra/main.fs
\ Agent Orchestra - Demonstrates multi-agent collaboration
\
\ This example simulates an agent system where:
\ - Conductor: Breaks down tasks into subtasks
\ - Porter: Converts Python code to Fifth
\ - Critic: Validates the conversion
\
\ Usage: ./fifth examples/agent-orchestra/main.fs

\ Note: This example demonstrates the agent pattern without
\ requiring the full core.fs library. It works standalone.

\ ============================================================
\ Minimal String Buffer (standalone)
\ ============================================================

4096 constant str-max
create str-buf str-max allot
variable str-len

: str-reset ( -- ) 0 str-len ! ;

: str+ ( addr u -- )
  dup str-len @ + str-max < if
    str-buf str-len @ + swap dup str-len +! move
  else 2drop then ;

: str$ ( -- addr u ) str-buf str-len @ ;

: str= ( addr1 u1 addr2 u2 -- flag )
  rot over <> if 2drop drop false exit then
  0 ?do
    over i + c@ over i + c@ <> if
      2drop false unloop exit
    then
  loop
  2drop true ;

\ ============================================================
\ Agent State Management
\ ============================================================

\ Each agent has: name, role, status, current-task
variable current-agent
variable task-count
variable completed-count

\ Task queue (simple circular buffer simulation)
8 constant max-tasks
create task-queue max-tasks 256 * allot
variable task-head
variable task-tail

\ Agent execution log
create exec-log 8192 allot
variable exec-log-len

: log-reset ( -- ) 0 exec-log-len ! ;

: log+ ( addr u -- )
  \ Append to execution log
  exec-log-len @ 8000 < if
    exec-log exec-log-len @ + swap
    dup exec-log-len +!
    move
  else
    2drop
  then ;

: log-nl ( -- ) s\" \n" log+ ;

: log$ ( -- addr u ) exec-log exec-log-len @ ;

\ ============================================================
\ Agent Definitions
\ ============================================================

\ Agent structure: name (32) | role (64) | status (16)
112 constant agent-size
3 constant num-agents
create agents num-agents agent-size * allot

: agent-addr ( n -- addr ) agent-size * agents + ;
: agent-name ( n -- addr ) agent-addr ;
: agent-role ( n -- addr ) agent-addr 32 + ;
: agent-status ( n -- addr ) agent-addr 96 + ;

: set-agent-name ( addr u n -- )
  agent-name swap
  dup 31 > if drop 31 then
  move ;

: set-agent-role ( addr u n -- )
  agent-role swap
  dup 63 > if drop 63 then
  move ;

: set-agent-status ( addr u n -- )
  agent-status swap
  dup 15 > if drop 15 then
  move ;

: agent-name$ ( n -- addr u )
  agent-name dup 32 0 do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 32 ;

: agent-role$ ( n -- addr u )
  agent-role dup 64 0 do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 64 ;

: agent-status$ ( n -- addr u )
  agent-status dup 16 0 do
    dup i + c@ 0= if drop i unloop exit then
  loop
  drop 16 ;

: init-agents ( -- )
  \ Agent 0: Conductor
  s" Conductor" 0 set-agent-name
  s" Task decomposition and orchestration" 0 set-agent-role
  s" idle" 0 set-agent-status

  \ Agent 1: Porter
  s" Porter" 1 set-agent-name
  s" Code translation between languages" 1 set-agent-role
  s" idle" 1 set-agent-status

  \ Agent 2: Critic
  s" Critic" 2 set-agent-name
  s" Code review and validation" 2 set-agent-role
  s" idle" 2 set-agent-status ;

\ ============================================================
\ Conductor Agent - Task Decomposition
\ ============================================================

: conductor-banner ( -- )
  s" [CONDUCTOR] Analyzing task..." log+ log-nl ;

: conductor-decompose ( task$ -- )
  \ Conductor breaks down the task
  conductor-banner

  s" [CONDUCTOR] Breaking task into subtasks:" log+ log-nl

  \ Simulated task breakdown (in real system, this would be LLM-driven)
  s"   1. Parse Python syntax" log+ log-nl
  s"   2. Identify data structures (list -> fixed buffer)" log+ log-nl
  s"   3. Convert control flow (for -> begin-while-repeat)" log+ log-nl
  s"   4. Map function calls to Forth words" log+ log-nl
  s"   5. Validate stack effects" log+ log-nl

  s" [CONDUCTOR] Delegating to Porter agent..." log+ log-nl log-nl

  2drop ;

\ ============================================================
\ Porter Agent - Code Translation
\ ============================================================

: porter-banner ( -- )
  s" [PORTER] Beginning code translation..." log+ log-nl ;

: porter-show-input ( -- )
  s" [PORTER] Input Python code:" log+ log-nl
  s" ----------------------------------------" log+ log-nl
  s" def calculate_stats(numbers):" log+ log-nl
  s"     total = sum(numbers)" log+ log-nl
  s"     count = len(numbers)" log+ log-nl
  s"     average = total / count" log+ log-nl
  s"     return {'total': total, 'avg': average}" log+ log-nl
  s" ----------------------------------------" log+ log-nl log-nl ;

: porter-translate ( -- )
  \ Porter converts Python to Fifth
  porter-banner
  porter-show-input

  s" [PORTER] Translation analysis:" log+ log-nl
  s"   - numbers list -> fixed buffer with count" log+ log-nl
  s"   - sum() -> loop with accumulator" log+ log-nl
  s"   - len() -> stored count variable" log+ log-nl
  s"   - dict return -> multiple return values on stack" log+ log-nl
  log-nl

  s" [PORTER] Generated Fifth code:" log+ log-nl
  s" ----------------------------------------" log+ log-nl
  s" \\ calculate-stats ( addr count -- total avg )" log+ log-nl
  s" \\ Takes buffer address and element count" log+ log-nl
  s" \\ Returns sum and average on stack" log+ log-nl
  s" " log+ log-nl
  s" : calculate-stats ( addr count -- total avg )" log+ log-nl
  s"   2dup                \\ keep copy for average calc" log+ log-nl
  s"   0 -rot              \\ ( 0 addr count )" log+ log-nl
  s"   0 ?do               \\ loop count times" log+ log-nl
  s"     dup i cells + @   \\ get numbers[i]" log+ log-nl
  s"     rot + swap        \\ accumulate sum" log+ log-nl
  s"   loop" log+ log-nl
  s"   drop                \\ drop addr, keep sum" log+ log-nl
  s"   dup rot             \\ ( sum sum count )" log+ log-nl
  s"   / ;                 \\ ( sum avg )" log+ log-nl
  s" ----------------------------------------" log+ log-nl log-nl

  s" [PORTER] Translation complete. Passing to Critic..." log+ log-nl log-nl ;

\ ============================================================
\ Critic Agent - Validation
\ ============================================================

: critic-banner ( -- )
  s" [CRITIC] Reviewing translated code..." log+ log-nl ;

: critic-validate ( -- )
  critic-banner

  s" [CRITIC] Validation checklist:" log+ log-nl
  s"   [PASS] Stack comment present and accurate" log+ log-nl
  s"   [PASS] No dynamic allocation used" log+ log-nl
  s"   [PASS] Loop structure follows Forth conventions" log+ log-nl
  s"   [WARN] Integer division - may lose precision" log+ log-nl
  s"   [PASS] Word name uses kebab-case" log+ log-nl
  log-nl

  s" [CRITIC] Suggested improvement:" log+ log-nl
  s"   Consider using '*/mod' for fractional results:" log+ log-nl
  s"   : calculate-stats-precise ( addr count -- total rem quot )" log+ log-nl
  s"     ... sum count /mod ;" log+ log-nl
  log-nl

  s" [CRITIC] Overall: APPROVED with minor suggestions" log+ log-nl
  s" ----------------------------------------" log+ log-nl ;

\ ============================================================
\ Orchestration Flow
\ ============================================================

: show-agent-status ( n -- )
  dup agent-name$ s"   " type type s" : " type
  dup agent-role$ type s"  [" type
  agent-status$ type s" ]" type cr ;

: show-all-agents ( -- )
  ." Active Agents:" cr
  num-agents 0 do
    i show-agent-status
  loop cr ;

: update-status ( status$ agent-n -- )
  set-agent-status ;

: run-orchestra ( task$ -- )
  \ Main orchestration sequence
  log-reset

  s" ========================================" log+ log-nl
  s" AGENT ORCHESTRA - Execution Log" log+ log-nl
  s" ========================================" log+ log-nl log-nl

  s" Task: " log+ 2dup log+ log-nl log-nl

  \ Phase 1: Conductor analyzes and decomposes
  s" active" 0 update-status
  conductor-decompose

  \ Phase 2: Porter translates code
  s" idle" 0 update-status
  s" active" 1 update-status
  porter-translate

  \ Phase 3: Critic validates
  s" idle" 1 update-status
  s" active" 2 update-status
  critic-validate

  s" idle" 2 update-status

  s" " log+ log-nl
  s" ========================================" log+ log-nl
  s" Orchestration Complete" log+ log-nl
  s" ========================================" log+ log-nl ;

\ ============================================================
\ Report Generation (Console-only for portability)
\ ============================================================

: show-execution-log ( -- )
  ." " cr
  ." === EXECUTION LOG ===" cr
  log$ type
  ." " cr ;

\ ============================================================
\ Console Output Mode
\ ============================================================

: print-separator ( -- )
  ." ========================================" cr ;

: print-header ( -- )
  cr print-separator
  ." AGENT ORCHESTRA - Fifth Multi-Agent Demo" cr
  print-separator cr ;

: print-agents ( -- )
  ." Agents in this orchestra:" cr cr
  num-agents 0 do
    ."   " i 1+ . ." . " i agent-name$ type
    ."  - " i agent-role$ type cr
  loop cr ;

: print-log ( -- )
  ." Execution transcript:" cr
  print-separator cr
  log$ type cr ;

: run-console ( task$ -- )
  print-header
  print-agents
  run-orchestra
  print-log
  print-separator
  ." Orchestration complete." cr cr ;

\ ============================================================
\ Main Entry Point
\ ============================================================

: demo-task$ ( -- addr u )
  s" Convert Python calculate_stats function to Fifth" ;

: banner ( -- )
  ." Agent Orchestra - Multi-Agent Collaboration Demo" cr
  ." ================================================" cr cr
  ." This example demonstrates:" cr
  ."   - Conductor: Task decomposition" cr
  ."   - Porter: Python to Fifth translation" cr
  ."   - Critic: Code validation and review" cr cr ;

: main ( -- )
  init-agents
  banner
  demo-task$ run-console ;

main
bye
