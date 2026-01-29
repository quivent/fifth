\ fifth/lib/pkg.fs - Package System
\ Portable require with lib: and pkg: prefixes
\
\ Usage:
\   use lib:core.fs        \ Load from $FIFTH_HOME/lib/
\   use pkg:claude-tools   \ Load $FIFTH_HOME/packages/claude-tools/package.fs
\   use ./local.fs         \ Relative to current file (standard require)

require ~/fifth/lib/str.fs

\ ============================================================
\ Path Buffer (third buffer to avoid conflicts)
\ ============================================================

512 constant path-max
create path-buf path-max allot
variable path-len

: path-reset ( -- ) 0 path-len ! ;
: path+ ( addr u -- )
  dup path-len @ + path-max < if
    path-buf path-len @ + swap dup path-len +! move
  else 2drop then ;
: path$ ( -- addr u ) path-buf path-len @ ;
: path-char ( c -- )
  path-len @ path-max < if
    path-buf path-len @ + c! 1 path-len +!
  else drop then ;

\ ============================================================
\ FIFTH_HOME Resolution
\ ============================================================

: default-fifth-home ( -- addr u )
  \ Default: ~/.fifth
  s" HOME" getenv dup 0= if
    2drop s" /tmp/.fifth"  \ Fallback if no HOME
  else
    path-reset path+ s" /.fifth" path+ path$
  then ;

: fifth-home$ ( -- addr u )
  \ Get FIFTH_HOME from environment or use default
  s" FIFTH_HOME" getenv dup 0= if
    2drop default-fifth-home
  then ;

\ ============================================================
\ Prefix Detection
\ ============================================================

: has-prefix? ( addr u prefix$ -- addr u flag )
  \ Check if string starts with prefix
  2>r 2dup 2r@ nip min 2r> drop -rot
  2over 2over str= >r 2drop r> ;

: strip-prefix ( addr u n -- addr' u' )
  \ Remove first n characters from string
  /string ;

\ ============================================================
\ Path Builders
\ ============================================================

: lib-path ( name$ -- path$ )
  \ Build: $FIFTH_HOME/lib/NAME
  path-reset
  fifth-home$ path+
  s" /lib/" path+
  path+
  path$ ;

: pkg-path ( name$ -- path$ )
  \ Build: $FIFTH_HOME/packages/NAME/package.fs
  path-reset
  fifth-home$ path+
  s" /packages/" path+
  path+
  s" /package.fs" path+
  path$ ;

\ ============================================================
\ Package Loading
\ ============================================================

: use-lib ( name$ -- )
  \ Load library from $FIFTH_HOME/lib/
  lib-path
  2dup file-exists? if
    included
  else
    ." ERROR: Library not found: " type cr
    abort
  then ;

: use-pkg ( name$ -- )
  \ Load package from $FIFTH_HOME/packages/NAME/
  pkg-path
  2dup file-exists? if
    included
  else
    ." ERROR: Package not found: " type cr
    abort
  then ;

: use ( "name" -- )
  \ Portable require with prefix support
  \ lib:NAME  -> $FIFTH_HOME/lib/NAME
  \ pkg:NAME  -> $FIFTH_HOME/packages/NAME/package.fs
  \ other     -> standard require behavior
  parse-name
  2dup s" lib:" has-prefix? if
    4 strip-prefix use-lib exit
  then
  2dup s" pkg:" has-prefix? if
    4 strip-prefix use-pkg exit
  then
  \ No prefix - use standard require
  required ;

\ ============================================================
\ Package Info
\ ============================================================

: .fifth-home ( -- )
  ." FIFTH_HOME: " fifth-home$ type cr ;

: .lib-path ( -- )
  ." Library path: " fifth-home$ type ." /lib/" cr ;

: .pkg-path ( -- )
  ." Package path: " fifth-home$ type ." /packages/" cr ;

: list-packages ( -- )
  \ List installed packages
  str-reset
  s" ls -1 " str+
  fifth-home$ str+
  s" /packages/ 2>/dev/null || echo '(none)'" str+
  str$ system ;

