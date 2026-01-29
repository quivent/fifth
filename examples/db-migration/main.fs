\ fifth/examples/db-migration/main.fs
\ Database migration tool

require ~/.fifth/lib/core.fs

\ Configuration
: db-path         ( -- addr u ) s" app.db" ;
: migrations-dir  ( -- addr u ) s" migrations/" ;
: meta-table      ( -- addr u ) s" _migrations" ;

\ --- Migration Metadata ---

: init-meta-table ( -- )
  \ Create migrations tracking table if not exists
  str-reset
  s" sqlite3 " str+
  db-path str+
  s"  \"CREATE TABLE IF NOT EXISTS _migrations (id INTEGER PRIMARY KEY, name TEXT, applied_at TEXT);\"" str+
  str$ system drop ;

: record-migration ( name-addr name-u -- )
  \ Record that a migration was applied
  str-reset
  s" sqlite3 " str+
  db-path str+
  s"  \"INSERT INTO _migrations (name, applied_at) VALUES ('" str+
  str+  \ migration name
  s" ', datetime('now'));\"" str+
  str$ system drop ;

: migration-applied? ( name-addr name-u -- flag )
  \ Check if migration was already applied
  \ TODO: Query _migrations table
  2drop false ;

\ --- Migration Execution ---

: run-sql-file ( filename-addr filename-u -- )
  \ Execute SQL file against database
  str-reset
  s" sqlite3 " str+
  db-path str+
  s"  < " str+
  str+
  str$ system drop ;

: run-migration ( name-addr name-u -- )
  \ Run a single migration
  2dup migration-applied? if
    s" Skipping (already applied): " type type cr
  else
    s" Applying: " type 2dup type cr
    \ Build path: migrations/NAME
    str-reset
    migrations-dir str+
    2dup str+
    str$ run-sql-file
    record-migration
    s" Done." type cr
  then ;

\ --- Status Report ---

: list-applied ( -- )
  \ Show all applied migrations
  s" Applied migrations:" type cr
  str-reset
  s" sqlite3 " str+
  db-path str+
  s"  \"SELECT name, applied_at FROM _migrations ORDER BY id;\"" str+
  str$ system drop ;

: list-pending ( -- )
  \ Show pending migrations
  s" Pending migrations:" type cr
  \ TODO: Scan migrations/ dir and check against applied
  s" (scanning not implemented)" type cr ;

\ --- Commands ---

: cmd-migrate ( -- )
  s" Running migrations..." type cr
  init-meta-table
  \ TODO: Scan migrations/ and run each
  s" 001_init.sql" run-migration
  s" 002_add_users.sql" run-migration
  s" Migrations complete." type cr ;

: cmd-status ( -- )
  init-meta-table
  list-applied
  cr
  list-pending ;

: cmd-rollback ( -- )
  s" Rollback not yet implemented" type cr
  s" (requires parsing DOWN sections)" type cr ;

: cmd-help ( -- )
  s" Database Migration Tool" type cr
  s" " type cr
  s" Commands:" type cr
  s"   migrate   - Run pending migrations" type cr
  s"   status    - Show migration status" type cr
  s"   rollback  - Rollback last migration" type cr
  s"   help      - Show this help" type cr ;

\ --- Main ---

: main ( -- )
  argc @ 2 < if
    cmd-help
  else
    1 argv 2dup s" migrate" compare 0= if 2drop cmd-migrate exit then
    2dup s" status" compare 0= if 2drop cmd-status exit then
    2dup s" rollback" compare 0= if 2drop cmd-rollback exit then
    2drop cmd-help
  then ;

main
bye
