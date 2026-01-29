\ fifth/examples/agentic-coder/tools/file-ops.fs
\ File operations for agentic coder

\ --- Read File ---

4096 constant file-buf-size
create file-buf file-buf-size allot
variable file-len

: read-file-content ( path-addr path-u -- content-addr content-u success? )
  \ Read entire file into buffer
  r/o open-file if
    2drop s" " false exit
  then
  >r

  file-buf file-buf-size r@ read-file if
    r> close-file drop
    s" " false exit
  then
  file-len !

  r> close-file drop
  file-buf file-len @ true ;

: tool-read-file ( path-addr path-u -- json-addr json-u )
  \ Read file and return as JSON result
  2dup read-file-content if
    str-reset
    s" {\"status\": \"success\", \"path\": \"" str+
    2swap str+
    s" \", \"content\": \"" str+
    \ TODO: JSON escape content
    str+
    s" \", \"lines\": " str+
    \ TODO: Count lines
    s" 0}" str+
    str$
  else
    2drop
    str-reset
    s" {\"status\": \"error\", \"path\": \"" str+
    str+
    s" \", \"error\": \"Could not read file\"}" str+
    str$
  then ;

\ --- Write File ---

: write-file-content ( content-addr content-u path-addr path-u -- success? )
  \ Write content to file
  w/o create-file if
    2drop 2drop false exit
  then
  >r
  r@ write-file if
    r> close-file drop false exit
  then
  r> close-file drop true ;

: tool-write-file ( path-addr path-u content-addr content-u -- json-addr json-u )
  \ Write content to file and return result
  2swap write-file-content if
    s" {\"status\": \"success\", \"message\": \"File written\"}"
  else
    s" {\"status\": \"error\", \"error\": \"Could not write file\"}"
  then ;

\ --- Edit File (Line-based) ---

: replace-line ( path-addr path-u line-num new-content-addr new-content-u -- success? )
  \ Replace specific line in file
  \ TODO: Implement line-by-line replacement
  2drop drop 2drop false ;

: insert-after-line ( path-addr path-u line-num content-addr content-u -- success? )
  \ Insert content after specific line
  \ TODO: Implement
  2drop drop 2drop false ;

: delete-lines ( path-addr path-u start-line end-line -- success? )
  \ Delete range of lines
  \ TODO: Implement
  2drop 2drop false ;

\ --- Patch Application ---

: apply-patch ( path-addr path-u patch-addr patch-u -- success? )
  \ Apply unified diff patch to file
  str-reset
  s" patch " str+
  2swap str+
  s"  <<'PATCH'\n" str+
  str+
  s" \nPATCH" str+
  str$ system 0= ;

: tool-patch-file ( path-addr path-u patch-addr patch-u -- json-addr json-u )
  apply-patch if
    s" {\"status\": \"success\", \"message\": \"Patch applied\"}"
  else
    s" {\"status\": \"error\", \"error\": \"Patch failed\"}"
  then ;

\ --- File Info ---

: file-exists? ( path-addr path-u -- flag )
  r/o open-file if
    2drop false
  else
    close-file drop true
  then ;

: tool-file-info ( path-addr path-u -- json-addr json-u )
  2dup file-exists? if
    str-reset
    s" {\"status\": \"success\", \"path\": \"" str+
    str+
    s" \", \"exists\": true}" str+
    str$
  else
    str-reset
    s" {\"status\": \"success\", \"path\": \"" str+
    str+
    s" \", \"exists\": false}" str+
    str$
  then ;

\ --- Directory Operations ---

: list-directory ( path-addr path-u -- )
  str-reset
  s" ls -la " str+
  str+
  str$ system drop ;

: tool-list-dir ( path-addr path-u -- json-addr json-u )
  s" {\"status\": \"success\", \"files\": []}"
  \ TODO: Parse ls output into JSON
  ;
