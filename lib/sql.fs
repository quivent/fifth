\ fifth/lib/sql.fs - SQLite Shell Interface
\ Execute queries via sqlite3 CLI, parse results

require ~/fifth/lib/str.fs

\ ============================================================
\ Configuration
\ ============================================================

s" /tmp/fifth-query.txt" 2constant sql-output
s" /tmp/fifth-count.txt" 2constant sql-count-output

variable sql-fid      \ File descriptor for query results
variable sql-count-fid

\ ============================================================
\ Command Building
\ ============================================================

: sql-cmd-query ( db$ sql$ -- )
  \ Build: sqlite3 -separator '|' db 'sql' > output
  str-reset
  s" sqlite3 -separator '|' " str+
  2swap str+
  s"  '" str+
  str+
  s" ' > " str+
  sql-output str+ ;

: sql-cmd-count ( db$ sql$ -- )
  \ Build: sqlite3 db 'sql' > count-output
  str-reset
  s" sqlite3 " str+
  2swap str+
  s"  '" str+
  str+
  s" ' > " str+
  sql-count-output str+ ;

\ ============================================================
\ Query Execution
\ ============================================================

: sql-exec ( db$ sql$ -- )
  \ Execute query, results go to sql-output file
  sql-cmd-query str$ system ;

: sql-count ( db$ sql$ -- n )
  \ Execute COUNT query, return number
  sql-cmd-count str$ system
  sql-count-output r/o open-file throw sql-count-fid !
  line-buf line-max sql-count-fid @ read-line throw drop
  sql-count-fid @ close-file drop
  line-buf swap s>number? if drop else 0 then ;

\ ============================================================
\ Result Processing
\ ============================================================

: sql-open ( -- )
  \ Open query results for reading
  sql-output r/o open-file throw sql-fid ! ;

: sql-close ( -- )
  \ Close query results
  sql-fid @ close-file throw ;

: sql-row? ( -- addr u flag )
  \ Read next row, return string and flag (true if data)
  line-buf line-max sql-fid @ read-line throw
  line-buf -rot ;

: sql-field ( addr u n -- addr u field$ )
  \ Extract nth field (0-based) from pipe-delimited row
  parse-pipe ;

\ ============================================================
\ High-Level Iteration
\ ============================================================

\ Usage pattern:
\   db$ s" SELECT * FROM table" sql-exec
\   sql-open
\   begin sql-row? while
\     dup 0> if
\       \ process row: 2dup 0 sql-field ...
\     then 2drop
\   repeat 2drop
\   sql-close

\ ============================================================
\ Table Queries
\ ============================================================

: sql-tables ( db$ -- )
  \ List all tables
  s" SELECT name FROM sqlite_master WHERE type='table' ORDER BY name" sql-exec ;

: sql-schema ( db$ table$ -- )
  \ Get table schema
  str-reset
  s" SELECT sql FROM sqlite_master WHERE name='" str+
  str+
  s" '" str+
  str$ sql-exec ;

: sql-table-count ( db$ table$ -- n )
  \ Count rows in table
  str-reset
  s" SELECT COUNT(*) FROM " str+
  str+
  str$ sql-count ;

\ ============================================================
\ Convenience: Iterate with xt
\ ============================================================

\ Execute xt for each row
\ xt signature: ( addr u -- ) where addr u is the row string
: sql-each ( db$ sql$ xt -- )
  >r sql-exec sql-open
  begin sql-row? while
    dup 0> if
      r@ execute
    else
      2drop
    then
  repeat 2drop
  sql-close r> drop ;

\ ============================================================
\ Debug Helpers
\ ============================================================

: sql-dump ( db$ sql$ -- )
  \ Execute and print results to stdout
  sql-exec sql-open
  begin sql-row? while
    dup 0> if type cr else 2drop then
  repeat 2drop
  sql-close ;

: .sql-field ( addr u n -- addr u )
  \ Print nth field, keep row on stack
  2>r 2dup 2r> sql-field type ;

: .sql-fields ( addr u n -- )
  \ Print first n fields, tab separated
  0 ?do
    i 0> if 9 emit then
    2dup i sql-field type
  loop 2drop ;
