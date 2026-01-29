\ fifth/examples/agentic-coder/context.fs
\ Context and memory management for agentic coder

\ --- Configuration ---

: context-db ( -- addr u ) s" memory.db" ;
: max-context-tokens ( -- n ) 100000 ;  \ Token budget
: max-file-size ( -- n ) 50000 ;         \ Max chars per file

\ --- Context State ---

variable total-context-tokens
create context-files 10 cells allot  \ Array of file paths
variable context-file-count

\ --- Token Estimation ---

: estimate-tokens ( chars -- tokens )
  \ Rough estimate: ~4 chars per token
  4 / ;

: tokens-remaining ( -- n )
  max-context-tokens total-context-tokens @ - ;

\ --- File Context ---

: add-file-to-context ( path-addr path-u -- success? )
  \ Add file to context if within budget
  2dup s" [Adding to context: " type type s" ]" type cr

  \ Check if file fits in budget
  \ TODO: Actually check file size and token count

  \ Store in file list
  context-file-count @ 10 < if
    \ TODO: Store path
    1 context-file-count +!
    true
  else
    s" [Context full - too many files]" type cr
    false
  then ;

: remove-file-from-context ( path-addr path-u -- )
  \ Remove file from context
  s" [Removing from context: " type type s" ]" type cr
  \ TODO: Find and remove from list
  ;

: clear-file-context ( -- )
  0 context-file-count !
  0 total-context-tokens !
  s" [Context cleared]" type cr ;

: list-context-files ( -- )
  s" Files in context:" type cr
  context-file-count @ 0 ?do
    s"   - " type
    \ TODO: Print each file path
    i . cr
  loop ;

\ --- Conversation History ---

: get-recent-messages ( n -- )
  \ Get last n messages from database
  str-reset
  s" sqlite3 " str+
  context-db str+
  s"  \"SELECT role, content FROM messages ORDER BY id DESC LIMIT " str+
  0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

: get-conversation-context ( -- addr u )
  \ Build context string from recent conversation
  str-reset
  s" Previous conversation:\n" str+
  \ TODO: Add recent messages
  str$ ;

: summarize-conversation ( -- summary-addr summary-u )
  \ Use LLM to summarize long conversation
  s" [Conversation summary would go here]" ;

\ --- Semantic Memory ---

: store-embedding ( text-addr text-u vector-addr vector-u -- )
  \ Store text with its embedding vector
  \ TODO: Implement with vector DB or SQLite
  2drop 2drop ;

: search-by-similarity ( query-addr query-u k -- )
  \ Find k most similar stored items
  drop 2drop
  s" [Similarity search not implemented]" type cr ;

\ --- Working Memory (Current Task) ---

256 constant working-mem-size
create working-memory working-mem-size allot
variable working-memory-len

: set-working-memory ( addr u -- )
  working-mem-size min
  dup working-memory-len !
  working-memory swap move ;

: get-working-memory ( -- addr u )
  working-memory working-memory-len @ ;

: clear-working-memory ( -- )
  0 working-memory-len ! ;

\ --- Context Building ---

: build-full-context ( -- context-addr context-u )
  \ Assemble complete context for LLM call
  str-reset

  \ System context
  s" You are an expert coding assistant.\n\n" str+

  \ Working memory (current task)
  working-memory-len @ 0> if
    s" Current task:\n" str+
    get-working-memory str+
    s" \n\n" str+
  then

  \ File context
  context-file-count @ 0> if
    s" Relevant files:\n" str+
    \ TODO: Add file contents
    s" \n\n" str+
  then

  \ Conversation history (if fits)
  tokens-remaining 5000 > if
    get-conversation-context str+
  then

  str$ ;

\ --- Context Compression ---

: compress-context ( -- )
  \ Reduce context size when approaching limit
  s" [Compressing context...]" type cr

  \ Strategy 1: Summarize old conversation
  \ Strategy 2: Remove least relevant files
  \ Strategy 3: Truncate file contents to key sections

  ;

: auto-manage-context ( -- )
  \ Automatically manage context size
  tokens-remaining 10000 < if
    compress-context
  then ;

\ --- Persistence ---

: save-context-state ( -- )
  \ Save current context to database
  str-reset
  s" sqlite3 " str+
  context-db str+
  s"  \"INSERT INTO context_snapshots (files, tokens, timestamp) VALUES ('" str+
  \ TODO: Serialize file list
  s" ', " str+
  total-context-tokens @ 0 <# #s #> str+
  s" , datetime('now'));\"" str+
  str$ system drop ;

: restore-context-state ( snapshot-id -- )
  \ Restore context from database
  s" [Restore not implemented]" type cr
  drop ;

\ --- Debug ---

: show-context-stats ( -- )
  s" Context Statistics:" type cr
  s"   Files: " type context-file-count @ . cr
  s"   Tokens used: " type total-context-tokens @ . cr
  s"   Tokens remaining: " type tokens-remaining . cr ;
