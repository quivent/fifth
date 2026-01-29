\ fifth/lib/core.fs - Core Library Loader
\ Loads all Fifth libraries

require ~/fifth/lib/str.fs
require ~/fifth/lib/html.fs
require ~/fifth/lib/sql.fs

\ ============================================================
\ Fifth Version
\ ============================================================

: .fifth ( -- )
  ." Fifth - Forth Libraries for the Fifth Age" cr
  ." Version 0.1.0" cr
  ." https://github.com/..." cr ;

\ ============================================================
\ File Utilities
\ ============================================================

: file-exists? ( addr u -- flag )
  r/o open-file if drop false else close-file drop true then ;

: with-file ( addr u xt -- )
  \ Execute xt with file open, auto-close
  \ xt: ( fid -- )
  >r w/o create-file throw
  dup r> execute
  close-file throw ;

\ ============================================================
\ Shell Utilities
\ ============================================================

: $system ( addr u -- )
  \ Execute shell command from string
  system ;

: open-file-cmd ( addr u -- )
  \ Open file with system default app (macOS)
  str-reset
  s" open " str+
  str+
  str$ system ;

: open-url ( addr u -- )
  \ Open URL in browser
  open-file-cmd ;

\ ============================================================
\ Debug Utilities
\ ============================================================

: .s. ( -- )
  \ Pretty print stack
  ." Stack: " .s cr ;

: ??  ( flag -- )
  \ Assert with message
  0= if ." ASSERTION FAILED" cr abort then ;

\ ============================================================
\ Common Constants
\ ============================================================

: KB ( n -- n*1024 ) 1024 * ;
: MB ( n -- n*1024*1024 ) 1024 * 1024 * ;
