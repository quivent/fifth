\ fifth/examples/context-manager/main.fs
\ Intelligent context window management for LLM agents
\ Implements: sliding window, summarization, priority scoring, retrieval

require ~/.fifth/lib/core.fs

\ ============================================================
\ Configuration
\ ============================================================

s" ~/.fifth/context.db" 2constant ctx-db
s" /tmp/ctx-query.txt" 2constant ctx-output
s" /tmp/ctx-tokens.txt" 2constant token-output
s" /tmp/ctx-summary.txt" 2constant summary-output

\ Token budgets per level
2000 constant level1-budget   \ Permanent (system prompts, rules)
4000 constant level2-budget   \ Session (task summaries, decisions)
8000 constant level3-budget   \ Working (recent conversation)
16000 constant total-budget   \ Total context window

\ Compression thresholds
300 constant age-threshold    \ Seconds before eligible for compression
80 constant capacity-trigger  \ Compress when context > 80% full

\ Priority levels
100 constant priority-critical
75 constant priority-high
50 constant priority-medium
25 constant priority-low

\ ============================================================
\ State
\ ============================================================

variable ctx-fid              \ File descriptor for query results
variable total-tokens         \ Current total tokens in context
variable compression-count    \ Number of compressions performed

create current-task 256 allot
variable current-task-len

\ ============================================================
\ Database Setup
\ ============================================================

: ctx-init-db ( -- )
  \ Create context database with all tables
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"" str+
  \ Messages table - raw conversation history
  s" CREATE TABLE IF NOT EXISTS messages (" str+
  s" id INTEGER PRIMARY KEY," str+
  s" role TEXT NOT NULL," str+
  s" content TEXT NOT NULL," str+
  s" tokens INTEGER DEFAULT 0," str+
  s" priority INTEGER DEFAULT 50," str+
  s" level INTEGER DEFAULT 3," str+
  s" created_at TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  \ Summaries table - compressed context
  s" CREATE TABLE IF NOT EXISTS summaries (" str+
  s" id INTEGER PRIMARY KEY," str+
  s" level INTEGER NOT NULL," str+
  s" content TEXT NOT NULL," str+
  s" tokens INTEGER DEFAULT 0," str+
  s" source_ids TEXT," str+
  s" created_at TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  \ Keywords table - retrieval index
  s" CREATE TABLE IF NOT EXISTS keywords (" str+
  s" id INTEGER PRIMARY KEY," str+
  s" keyword TEXT NOT NULL," str+
  s" message_id INTEGER," str+
  s" summary_id INTEGER);" str+
  \ Metrics table - compression stats
  s" CREATE TABLE IF NOT EXISTS metrics (" str+
  s" id INTEGER PRIMARY KEY," str+
  s" original_tokens INTEGER," str+
  s" compressed_tokens INTEGER," str+
  s" compression_ratio REAL," str+
  s" timestamp TEXT DEFAULT CURRENT_TIMESTAMP);" str+
  s" \"" str+
  str$ system drop ;

\ ============================================================
\ Token Counting
\ ============================================================

: estimate-tokens ( chars -- tokens )
  \ Rough estimate: ~4 chars per token (fast, approximate)
  4 / 1 max ;

: count-tokens-exact ( addr u -- tokens )
  \ Use tiktoken via Python for accurate count
  \ Falls back to estimate if tiktoken unavailable
  str-reset
  s" echo '" str+
  str+
  s" ' | python3 -c \"" str+
  s" import sys; " str+
  s" try:" str+
  s"   import tiktoken; " str+
  s"   enc = tiktoken.get_encoding('cl100k_base'); " str+
  s"   print(len(enc.encode(sys.stdin.read())));" str+
  s" except: print(-1)\" > " str+
  token-output str+
  str$ system drop
  \ Read result
  token-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if
    drop dup 0< if
      drop estimate-tokens  \ Fallback to estimate
    then
  else
    drop estimate-tokens
  then ;

: count-tokens ( addr u -- tokens )
  \ Use estimate for speed, exact for important content
  dup 1000 > if
    count-tokens-exact
  else
    estimate-tokens
  then ;

\ ============================================================
\ Message Storage
\ ============================================================

: escape-sql ( addr u -- )
  \ Escape single quotes for SQL by doubling them
  \ Appends to str-buf
  0 ?do
    dup i + c@
    dup [char] ' = if
      drop s" ''" str+
    else
      str-char
    then
  loop drop ;

: ctx-add-message ( role-addr role-u content-addr content-u priority level -- )
  \ Add message to database with metadata
  2>r >r                    \ save level, priority
  2>r                       \ save content
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"INSERT INTO messages (role, content, tokens, priority, level) VALUES ('" str+
  str+                      \ role
  s" ', '" str+
  2r> 2dup 2>r escape-sql   \ content (escaped)
  s" ', " str+
  2r> count-tokens n>str str+  \ tokens
  s" , " str+
  r> n>str str+             \ priority
  s" , " str+
  2r> drop n>str str+       \ level
  s" );\"" str+
  str$ system drop ;

: ctx-add-user ( content-addr content-u -- )
  \ Add user message at working level
  s" user" 2swap priority-high 3 ctx-add-message ;

: ctx-add-assistant ( content-addr content-u -- )
  \ Add assistant message at working level
  s" assistant" 2swap priority-high 3 ctx-add-message ;

: ctx-add-tool ( content-addr content-u -- )
  \ Add tool output at volatile level (low priority)
  s" tool" 2swap priority-low 4 ctx-add-message ;

: ctx-add-system ( content-addr content-u -- )
  \ Add system message at permanent level
  s" system" 2swap priority-critical 1 ctx-add-message ;

\ ============================================================
\ Token Accounting
\ ============================================================

: ctx-count-level ( level -- tokens )
  \ Count tokens at specific level
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT COALESCE(SUM(tokens), 0) FROM messages WHERE level=" str+
  n>str str+
  s" ;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-count-summaries ( level -- tokens )
  \ Count tokens in summaries at level
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT COALESCE(SUM(tokens), 0) FROM summaries WHERE level=" str+
  n>str str+
  s" ;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-total-tokens ( -- tokens )
  \ Sum tokens across all levels
  0
  1 ctx-count-level +
  2 ctx-count-level +
  3 ctx-count-level +
  1 ctx-count-summaries +
  2 ctx-count-summaries +
  3 ctx-count-summaries + ;

: ctx-capacity ( -- percent )
  \ Return current capacity as percentage
  ctx-total-tokens 100 * total-budget / ;

\ ============================================================
\ Priority Scoring
\ ============================================================

: ctx-age-minutes ( id -- minutes )
  \ Get age of message in minutes
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT CAST((julianday('now') - julianday(created_at)) * 1440 AS INTEGER) FROM messages WHERE id=" str+
  n>str str+
  s" ;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-recency-score ( id -- score )
  \ Score based on recency (0-100)
  \ Score = 100 / (1 + minutes)
  ctx-age-minutes 1+ 100 swap / ;

: ctx-update-priority ( id new-priority -- )
  \ Update message priority
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"UPDATE messages SET priority=" str+
  swap n>str str+
  s"  WHERE id=" str+
  n>str str+
  s" ;\"" str+
  str$ system drop ;

\ ============================================================
\ Summarization
\ ============================================================

: summarization-prompt ( -- addr u )
  s" Summarize the following conversation concisely. Preserve: key decisions, action items, errors encountered, and current task state. Output only the summary, no preamble." ;

: ctx-get-old-messages ( level age-minutes -- addr u )
  \ Get messages older than threshold at level
  str-reset
  s" sqlite3 -separator '|' " str+
  ctx-db str+
  s"  \"SELECT id, content FROM messages WHERE level=" str+
  swap n>str str+
  s"  AND CAST((julianday('now') - julianday(created_at)) * 1440 AS INTEGER) > " str+
  n>str str+
  s"  ORDER BY created_at ASC LIMIT 10;\" > " str+
  ctx-output str+
  str$ system drop
  \ Return file path for processing
  ctx-output ;

: ctx-call-summarizer ( content-addr content-u -- summary-addr summary-u )
  \ Call LLM to summarize content
  \ Uses curl to call Claude API
  str-reset
  s" curl -s https://api.anthropic.com/v1/messages " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'x-api-key: '\"$ANTHROPIC_API_KEY\"'' " str+
  s" -H 'anthropic-version: 2023-06-01' " str+
  s" -d '{" str+
  s" \"model\": \"claude-3-haiku-20240307\"," str+
  s" \"max_tokens\": 500," str+
  s" \"messages\": [{\"role\": \"user\", \"content\": \"" str+
  summarization-prompt str+
  s" \\n\\n" str+
  \ Escape content for JSON
  2swap str+
  s" \"}]" str+
  s" }' | jq -r '.content[0].text' > " str+
  summary-output str+
  str$ system drop
  \ Read summary result
  summary-output r/o open-file throw ctx-fid !
  str-reset
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap str+
    10 str-char  \ newline
  repeat drop
  ctx-fid @ close-file drop
  str$ ;

: ctx-store-summary ( level content-addr content-u source-ids-addr source-ids-u -- )
  \ Store summary in database
  2>r 2>r
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"INSERT INTO summaries (level, content, tokens, source_ids) VALUES (" str+
  n>str str+  \ level
  s" , '" str+
  2r> 2dup 2>r escape-sql  \ content
  s" ', " str+
  2r> count-tokens n>str str+  \ tokens
  s" , '" str+
  2r> str+  \ source_ids
  s" ');\"" str+
  str$ system drop ;

: ctx-delete-messages ( ids-addr ids-u -- )
  \ Delete messages by comma-separated IDs
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"DELETE FROM messages WHERE id IN (" str+
  str+
  s" );\"" str+
  str$ system drop ;

: ctx-compress-level ( level -- )
  \ Compress old messages at given level
  dup age-threshold 60 / ctx-get-old-messages
  \ Check if file has content
  r/o open-file throw ctx-fid !
  str-reset
  0  \ ID accumulator string
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap dup 0> if
      \ Parse ID and content
      2dup 0 parse-pipe 2drop  \ Get ID
      2dup s>number? if
        drop dup 0> if
          \ Add ID to list
          str-len @ 0> if s" ," str+ then
          n>str str+
        else drop then
      else 2drop then
      \ Add content to summary source
      2dup 1 parse-pipe str2-reset str2+
      s" \n" str2+
      2drop
    else 2drop then
  repeat drop
  ctx-fid @ close-file drop
  \ If we have content to summarize
  str-len @ 0> if
    str$  \ IDs
    2dup 2>r
    str2$  \ Content to summarize
    ctx-call-summarizer  \ Get summary
    swap 2>r swap 2r> 2r>  \ ( level summary source-ids )
    ctx-store-summary
    \ Delete original messages
    ctx-delete-messages
    1 compression-count +!
  else
    drop 2drop
  then ;

: ctx-auto-compress ( -- )
  \ Automatically compress when over threshold
  ctx-capacity capacity-trigger > if
    s" [Compressing context...]" type cr
    3 ctx-compress-level  \ Compress working memory first
    ctx-capacity capacity-trigger > if
      2 ctx-compress-level  \ Then session if still over
    then
  then ;

\ ============================================================
\ Context Retrieval
\ ============================================================

: ctx-get-recent ( n level -- )
  \ Get n most recent items at level, output to ctx-output
  str-reset
  s" sqlite3 -separator '|' " str+
  ctx-db str+
  s"  \"SELECT id, role, content FROM messages WHERE level=" str+
  swap n>str str+
  s"  ORDER BY created_at DESC LIMIT " str+
  n>str str+
  s" ;\" > " str+
  ctx-output str+
  str$ system drop ;

: ctx-get-summaries ( level -- )
  \ Get summaries at level
  str-reset
  s" sqlite3 -separator '|' " str+
  ctx-db str+
  s"  \"SELECT content FROM summaries WHERE level=" str+
  n>str str+
  s"  ORDER BY created_at DESC;\" > " str+
  ctx-output str+
  str$ system drop ;

: ctx-search-keyword ( keyword-addr keyword-u -- )
  \ Search for messages containing keyword
  str-reset
  s" sqlite3 -separator '|' " str+
  ctx-db str+
  s"  \"SELECT id, role, content FROM messages WHERE content LIKE '%" str+
  str+
  s" %' ORDER BY created_at DESC LIMIT 5;\" > " str+
  ctx-output str+
  str$ system drop ;

\ ============================================================
\ Context Building
\ ============================================================

create context-buf 32768 allot
variable context-len

: ctx-reset ( -- ) 0 context-len ! ;

: ctx+ ( addr u -- )
  \ Append to context buffer
  dup context-len @ + 32768 < if
    context-buf context-len @ + swap dup context-len +! move
  else
    2drop
  then ;

: ctx$ ( -- addr u )
  context-buf context-len @ ;

: ctx-build ( -- addr u )
  \ Build optimized context for LLM call
  ctx-reset

  \ Level 1: Permanent (system prompts)
  s" === SYSTEM ===\n" ctx+
  1 ctx-get-summaries
  ctx-output r/o open-file throw ctx-fid !
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap ctx+
    10 context-buf context-len @ + c! 1 context-len +!
  repeat drop
  ctx-fid @ close-file drop

  10 1 ctx-get-recent
  ctx-output r/o open-file throw ctx-fid !
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap dup 0> if
      \ Parse: id|role|content
      2dup 1 parse-pipe ctx+  \ role
      s" : " ctx+
      2dup 2 parse-pipe ctx+  \ content
      10 context-buf context-len @ + c! 1 context-len +!
      2drop
    else 2drop then
  repeat drop
  ctx-fid @ close-file drop

  \ Level 2: Session (summaries and key decisions)
  s" \n=== SESSION CONTEXT ===\n" ctx+
  2 ctx-get-summaries
  ctx-output r/o open-file throw ctx-fid !
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap ctx+
    10 context-buf context-len @ + c! 1 context-len +!
  repeat drop
  ctx-fid @ close-file drop

  \ Level 3: Working (recent conversation)
  s" \n=== RECENT CONVERSATION ===\n" ctx+
  20 3 ctx-get-recent
  ctx-output r/o open-file throw ctx-fid !
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap dup 0> if
      2dup 1 parse-pipe ctx+  \ role
      s" : " ctx+
      2dup 2 parse-pipe ctx+  \ content
      10 context-buf context-len @ + c! 1 context-len +!
      2drop
    else 2drop then
  repeat drop
  ctx-fid @ close-file drop

  ctx$ ;

\ ============================================================
\ Statistics & Metrics
\ ============================================================

: ctx-message-count ( -- n )
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT COUNT(*) FROM messages;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-summary-count ( -- n )
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT COUNT(*) FROM summaries;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-avg-compression ( -- ratio )
  str-reset
  s" sqlite3 " str+
  ctx-db str+
  s"  \"SELECT COALESCE(AVG(compression_ratio), 0) FROM metrics;\" > " str+
  ctx-output str+
  str$ system drop
  ctx-output r/o open-file throw ctx-fid !
  line-buf line-max ctx-fid @ read-line throw drop
  ctx-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

: ctx-show-stats ( -- )
  cr
  s" Context Manager Statistics" type cr
  s" ===========================" type cr
  cr
  s" Token Usage:" type cr
  s"   Total:     " type ctx-total-tokens . s"  / " type total-budget . s"  (" type ctx-capacity . s" %)" type cr
  s"   Level 1:   " type 1 ctx-count-level . s"  / " type level1-budget . s"  (permanent)" type cr
  s"   Level 2:   " type 2 ctx-count-level . s"  / " type level2-budget . s"  (session)" type cr
  s"   Level 3:   " type 3 ctx-count-level . s"  / " type level3-budget . s"  (working)" type cr
  cr
  s" Storage:" type cr
  s"   Messages:   " type ctx-message-count . cr
  s"   Summaries:  " type ctx-summary-count . cr
  s"   Compressions: " type compression-count @ . cr
  cr ;

\ ============================================================
\ Keyword Extraction (Simple)
\ ============================================================

: is-stopword? ( addr u -- flag )
  \ Check if word is a common stopword
  2dup s" the" str= if 2drop true exit then
  2dup s" a" str= if 2drop true exit then
  2dup s" an" str= if 2drop true exit then
  2dup s" is" str= if 2drop true exit then
  2dup s" are" str= if 2drop true exit then
  2dup s" was" str= if 2drop true exit then
  2dup s" were" str= if 2drop true exit then
  2dup s" be" str= if 2drop true exit then
  2dup s" been" str= if 2drop true exit then
  2dup s" to" str= if 2drop true exit then
  2dup s" of" str= if 2drop true exit then
  2dup s" and" str= if 2drop true exit then
  2dup s" or" str= if 2drop true exit then
  2dup s" in" str= if 2drop true exit then
  2dup s" on" str= if 2drop true exit then
  2dup s" at" str= if 2drop true exit then
  2dup s" for" str= if 2drop true exit then
  2dup s" with" str= if 2drop true exit then
  2dup s" that" str= if 2drop true exit then
  2dup s" this" str= if 2drop true exit then
  2dup s" it" str= if 2drop true exit then
  2dup s" I" str= if 2drop true exit then
  2drop false ;

: ctx-index-message ( id content-addr content-u -- )
  \ Extract keywords and store in index
  \ Simple: store words > 4 chars that aren't stopwords
  2drop drop  \ TODO: Implement proper keyword extraction
  ;

\ ============================================================
\ Interactive Commands
\ ============================================================

: ctx-cmd-add ( -- )
  \ Add message interactively
  s" Role (user/assistant/system): " type
  line-buf 32 accept line-buf swap
  2dup s" user" str= if
    2drop
    s" Content: " type
    line-buf 256 accept line-buf swap
    ctx-add-user
  else 2dup s" assistant" str= if
    2drop
    s" Content: " type
    line-buf 256 accept line-buf swap
    ctx-add-assistant
  else 2dup s" system" str= if
    2drop
    s" Content: " type
    line-buf 256 accept line-buf swap
    ctx-add-system
  else
    2drop s" Unknown role" type cr
  then then then ;

: ctx-cmd-build ( -- )
  \ Build and display context
  ctx-build type ;

: ctx-cmd-compress ( -- )
  \ Force compression
  s" Compressing level 3 (working)..." type cr
  3 ctx-compress-level
  s" Done. " type compression-count @ . s"  total compressions." type cr ;

: ctx-cmd-search ( -- )
  \ Search for keyword
  s" Keyword: " type
  line-buf 64 accept line-buf swap
  ctx-search-keyword
  s" Results:" type cr
  ctx-output r/o open-file throw ctx-fid !
  begin
    line-buf line-max ctx-fid @ read-line throw
  while
    line-buf swap dup 0> if
      s"   " type
      2dup 1 parse-pipe type  \ role
      s" : " type
      2dup 2 parse-pipe type  \ content
      cr
      2drop
    else 2drop then
  repeat drop
  ctx-fid @ close-file drop ;

: ctx-cmd-clear ( -- )
  \ Clear all context
  s" Are you sure? (yes/no): " type
  line-buf 8 accept line-buf swap
  s" yes" str= if
    str-reset
    s" sqlite3 " str+
    ctx-db str+
    s"  \"DELETE FROM messages; DELETE FROM summaries; DELETE FROM keywords;\"" str+
    str$ system drop
    0 total-tokens !
    0 compression-count !
    s" Context cleared." type cr
  else
    s" Cancelled." type cr
  then ;

: ctx-repl ( -- )
  \ Interactive context manager
  s" Context Manager v1.0" type cr
  s" Commands: add, build, stats, compress, search, clear, quit" type cr
  cr
  begin
    s" ctx> " type
    line-buf 64 accept line-buf swap
    2dup s" quit" str= if 2drop false else
    2dup s" exit" str= if 2drop false else
    2dup s" add" str= if 2drop ctx-cmd-add true else
    2dup s" build" str= if 2drop ctx-cmd-build true else
    2dup s" stats" str= if 2drop ctx-show-stats true else
    2dup s" compress" str= if 2drop ctx-cmd-compress true else
    2dup s" search" str= if 2drop ctx-cmd-search true else
    2dup s" clear" str= if 2drop ctx-cmd-clear true else
    2dup s" help" str= if
      2drop
      s" Commands:" type cr
      s"   add      - Add a message" type cr
      s"   build    - Build and show context" type cr
      s"   stats    - Show statistics" type cr
      s"   compress - Force compression" type cr
      s"   search   - Search by keyword" type cr
      s"   clear    - Clear all context" type cr
      s"   quit     - Exit" type cr
      true
    else
      s" Unknown command: " type type cr
      true
    then then then then then then then then then
  until
  s" Goodbye!" type cr ;

\ ============================================================
\ CLI Interface
\ ============================================================

: usage ( -- )
  s" Context Manager - Intelligent context window management" type cr
  cr
  s" Usage:" type cr
  s"   ./fifth context-manager/main.fs              - Interactive mode" type cr
  s"   ./fifth context-manager/main.fs add <role> <content>" type cr
  s"   ./fifth context-manager/main.fs stats        - Show statistics" type cr
  s"   ./fifth context-manager/main.fs build        - Build context" type cr
  s"   ./fifth context-manager/main.fs compress     - Force compression" type cr
  s"   ./fifth context-manager/main.fs search <kw>  - Search keyword" type cr
  s"   ./fifth context-manager/main.fs clear        - Clear all" type cr ;

: main ( -- )
  ctx-init-db
  0 compression-count !

  argc @ 2 < if
    ctx-repl exit
  then

  1 argv
  2dup s" add" str= if
    2drop
    argc @ 4 < if
      s" Usage: add <role> <content>" type cr exit
    then
    2 argv 3 argv
    2over s" user" str= if 2drop ctx-add-user else
    2over s" assistant" str= if 2drop ctx-add-assistant else
    2over s" system" str= if 2drop ctx-add-system else
    2over s" tool" str= if 2drop ctx-add-tool else
      2drop 2drop s" Unknown role. Use: user, assistant, system, tool" type cr
    then then then then
    exit
  then
  2dup s" stats" str= if 2drop ctx-show-stats exit then
  2dup s" build" str= if 2drop ctx-build type exit then
  2dup s" compress" str= if 2drop 3 ctx-compress-level s" Done." type cr exit then
  2dup s" search" str= if
    2drop
    argc @ 3 < if s" Usage: search <keyword>" type cr exit then
    2 argv ctx-search-keyword
    ctx-output r/o open-file throw ctx-fid !
    begin
      line-buf line-max ctx-fid @ read-line throw
    while
      line-buf swap dup 0> if
        2dup 1 parse-pipe type s" : " type
        2dup 2 parse-pipe type cr
        2drop
      else 2drop then
    repeat drop
    ctx-fid @ close-file drop
    exit
  then
  2dup s" clear" str= if
    2drop
    str-reset
    s" sqlite3 " str+
    ctx-db str+
    s"  \"DELETE FROM messages; DELETE FROM summaries;\"" str+
    str$ system drop
    s" Cleared." type cr
    exit
  then
  2dup s" help" str= if 2drop usage exit then
  2dup s" -h" str= if 2drop usage exit then
  2dup s" --help" str= if 2drop usage exit then
  2drop usage ;

main
bye
