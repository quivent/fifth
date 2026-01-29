\ fifth/examples/agentic-coder/main.fs
\ Agentic coding assistant - main entry point

require ~/.fifth/lib/core.fs

\ Load tool modules
\ require tools/file-ops.fs
\ require tools/shell.fs
\ require tools/llm.fs
\ require tools/git.fs
\ require tools/search.fs

\ --- Configuration ---

: db-file ( -- addr u ) s" memory.db" ;
: model ( -- addr u ) s" claude-3-sonnet-20240229" ;
: max-tokens ( -- n ) 4096 ;

\ --- State ---

variable agent-running
variable task-depth
create current-task 256 allot
variable current-task-len

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, role TEXT, content TEXT, timestamp TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  s" CREATE TABLE IF NOT EXISTS file_cache (path TEXT PRIMARY KEY, content TEXT, hash TEXT, cached_at TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  s" CREATE TABLE IF NOT EXISTS tasks (id INTEGER PRIMARY KEY, description TEXT, status TEXT DEFAULT 'pending', parent_id INTEGER, result TEXT, created_at TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  s" \"" str+
  str$ system drop ;

\ --- Message Logging ---

: log-message ( role-addr role-u content-addr content-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO messages (role, content) VALUES ('" str+
  2swap str+  \ role
  s" ', '" str+
  \ TODO: Escape content properly
  str+        \ content
  s" ');\"" str+
  str$ system drop ;

: log-user ( content-addr content-u -- )
  s" user" 2swap log-message ;

: log-assistant ( content-addr content-u -- )
  s" assistant" 2swap log-message ;

: log-tool ( content-addr content-u -- )
  s" tool" 2swap log-message ;

\ --- Context Management ---

: add-file-context ( path-addr path-u -- )
  s" Adding context: " type 2dup type cr
  \ Read file and store in cache
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT OR REPLACE INTO file_cache (path, content, hash) " str+
  s" SELECT '" str+
  2dup str+
  s" ', content, '' FROM (SELECT '" str+
  \ TODO: Actually read file content
  s" [file content]" str+
  s" ' as content);\"" str+
  str$ system drop ;

: get-context ( -- context-addr context-u )
  \ Gather relevant context from cache
  s" " ;  \ placeholder

: clear-context ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"DELETE FROM file_cache;\"" str+
  str$ system drop
  s" Context cleared" type cr ;

\ --- Task Management ---

variable next-task-id

: create-task ( desc-addr desc-u -- id )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO tasks (description, status) VALUES ('" str+
  str+
  s" ', 'pending'); SELECT last_insert_rowid();\"" str+
  str$ system drop
  next-task-id @ dup 1+ next-task-id !  ;

: update-task ( id status-addr status-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"UPDATE tasks SET status='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

: complete-task ( id result-addr result-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"UPDATE tasks SET status='done', result='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

: list-tasks ( -- )
  s" Tasks:" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT id, status, description FROM tasks ORDER BY id DESC LIMIT 10;\"" str+
  str$ system drop ;

\ --- Tool Dispatch ---

: dispatch-tool ( tool-name-addr tool-name-u args-addr args-u -- result-addr result-u )
  \ Route to appropriate tool handler
  2>r  \ save args
  2dup s" read" compare 0= if 2drop 2r> tool-read exit then
  2dup s" write" compare 0= if 2drop 2r> tool-write exit then
  2dup s" shell" compare 0= if 2drop 2r> tool-shell exit then
  2dup s" search" compare 0= if 2drop 2r> tool-search exit then
  2dup s" git" compare 0= if 2drop 2r> tool-git exit then
  2drop 2r> 2drop
  s" {\"error\": \"unknown tool\"}" ;

\ Tool stubs (would be in separate files)
: tool-read ( path-addr path-u -- result-addr result-u )
  s" [Reading file: " type type s" ]" type cr
  s" {\"status\": \"success\", \"content\": \"...\"}" ;

: tool-write ( args-addr args-u -- result-addr result-u )
  s" [Writing file...]" type cr
  2drop s" {\"status\": \"success\"}" ;

: tool-shell ( cmd-addr cmd-u -- result-addr result-u )
  s" [Executing: " type 2dup type s" ]" type cr
  system drop
  s" {\"status\": \"success\", \"output\": \"...\"}" ;

: tool-search ( pattern-addr pattern-u -- result-addr result-u )
  s" [Searching: " type type s" ]" type cr
  s" {\"status\": \"success\", \"matches\": []}" ;

: tool-git ( cmd-addr cmd-u -- result-addr result-u )
  s" [Git: " type type s" ]" type cr
  s" {\"status\": \"success\"}" ;

\ --- LLM Interaction ---

8192 constant response-buf-size
create response-buf response-buf-size allot
variable response-len

: build-prompt ( user-input-addr user-input-u -- prompt-addr prompt-u )
  \ Build full prompt with context and history
  str-reset
  s" You are an expert coding assistant. Help the user with their request.\n\n" str+
  s" User request: " str+
  str+
  s" \n\nRespond helpfully and concisely." str+
  str$ ;

: call-llm ( prompt-addr prompt-u -- response-addr response-u )
  \ Call Claude API via curl
  s" [Calling LLM...]" type cr

  \ Build curl command
  str-reset
  s" curl -s https://api.anthropic.com/v1/messages " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'x-api-key: '\"$ANTHROPIC_API_KEY\"'' " str+
  s" -H 'anthropic-version: 2023-06-01' " str+
  s" -d '{" str+
  s" \"model\": \"" str+ model str+ s" \"," str+
  s" \"max_tokens\": " str+ max-tokens 0 <# #s #> str+ s" ," str+
  s" \"messages\": [{\"role\": \"user\", \"content\": \"" str+
  \ TODO: Escape prompt properly
  2swap str+
  s" \"}]" str+
  s" }'" str+

  \ Execute and capture response
  str$ system drop

  \ For demo, return placeholder
  s" I'll help you with that. Let me analyze the code and suggest improvements." ;

\ --- Agent Loop ---

: process-input ( input-addr input-u -- )
  2dup log-user

  \ Build prompt and call LLM
  build-prompt
  call-llm

  \ Log and display response
  2dup log-assistant
  cr type cr ;

: agent-prompt ( -- )
  cr s" Agent> " type ;

: user-prompt ( -- )
  s" You> " type ;

256 constant input-buf-size
create input-buf input-buf-size allot

: read-user-input ( -- addr u )
  input-buf input-buf-size accept
  input-buf swap ;

: agent-repl ( -- )
  s" Agentic Coder v0.1" type cr
  s" Type 'help' for commands, 'quit' to exit" type cr
  cr

  true agent-running !

  begin
    agent-running @ while
    agent-prompt
    s" What would you like to do?" type cr
    user-prompt
    read-user-input

    \ Check for commands
    2dup s" quit" compare 0= if 2drop false agent-running ! else
    2dup s" exit" compare 0= if 2drop false agent-running ! else
    2dup s" help" compare 0= if 2drop show-help else
    2dup s" tasks" compare 0= if 2drop list-tasks else
    2dup s" clear" compare 0= if 2drop clear-context else
    2dup s" history" compare 0= if 2drop show-history else
      process-input
    then then then then then then
  repeat

  s" Goodbye!" type cr ;

: show-help ( -- )
  s" Commands:" type cr
  s"   help     - Show this help" type cr
  s"   tasks    - List current tasks" type cr
  s"   clear    - Clear context" type cr
  s"   history  - Show conversation history" type cr
  s"   quit     - Exit the agent" type cr
  s" " type cr
  s" Or just type your request and I'll help!" type cr ;

: show-history ( -- )
  s" Recent messages:" type cr
  str-reset
  s" sqlite3 -column " str+
  db-file str+
  s"  \"SELECT role, substr(content, 1, 60) FROM messages ORDER BY id DESC LIMIT 10;\"" str+
  str$ system drop ;

\ --- CLI Commands ---

: cmd-ask ( input-addr input-u -- )
  process-input ;

: cmd-context ( path-addr path-u -- )
  add-file-context ;

: cmd-plan ( desc-addr desc-u -- )
  s" Creating plan for: " type 2dup type cr
  create-task drop
  \ TODO: Decompose into subtasks
  s" Plan created" type cr ;

\ --- Main ---

: usage ( -- )
  s" Agentic Coder" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth agentic-coder/main.fs              - Interactive mode" type cr
  s"   ./fifth agentic-coder/main.fs ask <query>  - Single query" type cr
  s"   ./fifth agentic-coder/main.fs context <file> - Add file context" type cr
  s"   ./fifth agentic-coder/main.fs plan <task>  - Create task plan" type cr
  s"   ./fifth agentic-coder/main.fs tasks        - List tasks" type cr ;

: main ( -- )
  init-db
  0 next-task-id !
  0 task-depth !

  argc @ 2 < if
    agent-repl exit
  then

  1 argv
  2dup s" ask" compare 0= if
    2drop
    argc @ 3 < if s" Usage: ask <query>" type cr exit then
    2 argv cmd-ask
    exit
  then
  2dup s" context" compare 0= if
    2drop
    argc @ 3 < if s" Usage: context <file>" type cr exit then
    2 argv cmd-context
    exit
  then
  2dup s" plan" compare 0= if
    2drop
    argc @ 3 < if s" Usage: plan <description>" type cr exit then
    2 argv cmd-plan
    exit
  then
  2dup s" tasks" compare 0= if 2drop list-tasks exit then
  2drop usage ;

main
bye
