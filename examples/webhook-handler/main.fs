\ fifth/examples/webhook-handler/main.fs
\ Webhook handler - process incoming events

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" events.db" ;

\ Payload buffer
4096 constant max-payload
create payload-buf max-payload allot
variable payload-len

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS events (" str+
  s"   id INTEGER PRIMARY KEY," str+
  s"   event_type TEXT," str+
  s"   payload TEXT," str+
  s"   status TEXT DEFAULT 'pending'," str+
  s"   created_at TEXT DEFAULT CURRENT_TIMESTAMP" str+
  s" );\"" str+
  str$ system drop ;

: store-event ( type-addr type-u payload-addr payload-u -- id )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO events (event_type, payload) VALUES ('" str+
  2swap str+  \ event type
  s" ', '" str+
  \ TODO: Escape payload for SQL
  str+        \ payload (simplified - needs escaping)
  s" '); SELECT last_insert_rowid();\"" str+
  str$ system drop
  0 ;  \ placeholder ID

: update-status ( id status-addr status-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"UPDATE events SET status='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

\ --- JSON Processing (via jq) ---

: jq-extract ( json-addr json-u query-addr query-u -- result-addr result-u )
  \ Run jq query on JSON
  \ TODO: Implement with temp files
  2drop 2drop s" " ;

: get-event-type ( json-addr json-u -- type-addr type-u )
  s" .event // .type // \"unknown\"" jq-extract ;

: get-repo-name ( json-addr json-u -- name-addr name-u )
  s" .repository.full_name // \"\"" jq-extract ;

\ --- Event Handlers ---

: handle-push ( payload-addr payload-u -- )
  s" GitHub Push Event" type cr
  2dup get-repo-name
  s"   Repository: " type type cr
  s" .commits | length" jq-extract
  s"   Commits: " type type cr
  2drop ;

: handle-pull-request ( payload-addr payload-u -- )
  s" GitHub Pull Request Event" type cr
  2dup s" .action" jq-extract
  s"   Action: " type type cr
  s" .pull_request.title" jq-extract
  s"   Title: " type type cr
  2drop ;

: handle-issue ( payload-addr payload-u -- )
  s" GitHub Issue Event" type cr
  2dup s" .action" jq-extract
  s"   Action: " type type cr
  s" .issue.title" jq-extract
  s"   Title: " type type cr
  2drop ;

: handle-unknown ( payload-addr payload-u -- )
  s" Unknown event type" type cr
  2drop ;

\ --- Event Router ---

: route-event ( type-addr type-u payload-addr payload-u -- )
  2>r  \ save payload
  2dup s" push" compare 0= if 2drop 2r> handle-push exit then
  2dup s" pull_request" compare 0= if 2drop 2r> handle-pull-request exit then
  2dup s" issues" compare 0= if 2drop 2r> handle-issue exit then
  2drop 2r> handle-unknown ;

\ --- Main Processing ---

: read-payload-file ( filename-addr filename-u -- )
  r/o open-file throw >r
  payload-buf max-payload r@ read-file throw payload-len !
  r> close-file throw ;

: process-payload ( -- )
  payload-buf payload-len @
  2dup get-event-type          \ ( payload type )
  2>r 2dup 2r@ store-event drop  \ store and get ID
  2r@ 2swap route-event        \ route to handler
  2r> 2drop
  s" Event processed successfully" type cr ;

: process-file ( filename-addr filename-u -- )
  s" Processing: " type 2dup type cr
  read-payload-file
  process-payload ;

\ --- History & Replay ---

: show-history ( -- )
  s" Recent Events:" type cr
  s" ==============" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT id, event_type, status, created_at FROM events ORDER BY id DESC LIMIT 20;\"" str+
  str$ system drop ;

: replay-failed ( -- )
  s" Replaying failed events..." type cr
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"SELECT id, payload FROM events WHERE status='failed' ORDER BY id;\"" str+
  str$ system drop
  \ TODO: Parse output and reprocess each
  s" Replay complete" type cr ;

\ --- Main ---

: usage ( -- )
  s" Webhook Handler" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth webhook-handler/main.fs process <file>  - Process payload file" type cr
  s"   ./fifth webhook-handler/main.fs history         - View event history" type cr
  s"   ./fifth webhook-handler/main.fs replay          - Replay failed events" type cr ;

: main ( -- )
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" process" compare 0= if
    2drop
    argc @ 3 < if s" Usage: process <file>" type cr exit then
    2 argv process-file
    exit
  then
  2dup s" history" compare 0= if 2drop show-history exit then
  2dup s" replay" compare 0= if 2drop replay-failed exit then
  2drop usage ;

main
bye
