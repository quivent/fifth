\ fifth/lib/str.fs - String Utilities
\ Buffer-based string building without dynamic allocation

\ ============================================================
\ String Buffer
\ ============================================================

1024 constant str-max
create str-buf str-max allot
variable str-len

: str-reset ( -- ) 0 str-len ! ;

: str+ ( addr u -- )
  \ Append string to buffer
  dup str-len @ + str-max < if
    str-buf str-len @ + swap dup str-len +! move
  else
    2drop
  then ;

: str$ ( -- addr u )
  \ Get current buffer contents
  str-buf str-len @ ;

: str-char ( c -- )
  \ Append single character
  str-len @ str-max < if
    str-buf str-len @ + c!
    1 str-len +!
  else
    drop
  then ;

\ ============================================================
\ Second String Buffer (for nested operations)
\ ============================================================

1024 constant str2-max
create str2-buf str2-max allot
variable str2-len

: str2-reset ( -- ) 0 str2-len ! ;

: str2+ ( addr u -- )
  dup str2-len @ + str2-max < if
    str2-buf str2-len @ + swap dup str2-len +! move
  else
    2drop
  then ;

: str2$ ( -- addr u ) str2-buf str2-len @ ;

: str2-char ( c -- )
  str2-len @ str2-max < if
    str2-buf str2-len @ + c!
    1 str2-len +!
  else
    drop
  then ;

\ ============================================================
\ Line Buffer (for file I/O)
\ ============================================================

512 constant line-max
create line-buf line-max allot

\ ============================================================
\ Number to String
\ ============================================================

: n>str ( n -- addr u )
  \ Convert number to string
  0 <# #s #> ;

\ ============================================================
\ String Comparison
\ ============================================================

: str= ( addr1 u1 addr2 u2 -- flag )
  \ Compare two strings for equality
  rot over <> if 2drop drop false exit then
  0 ?do
    over i + c@ over i + c@ <> if
      2drop false unloop exit
    then
  loop
  2drop true ;

\ ============================================================
\ String Search
\ ============================================================

: str-find-char ( addr u c -- addr' u' | 0 0 )
  \ Find character in string, return position or 0 0 if not found
  >r
  begin
    dup 0> while
    over c@ r@ = if r> drop exit then
    1 /string
  repeat
  r> drop ;

\ ============================================================
\ Field Parsing (for delimited data)
\ ============================================================

: skip-to-delim ( addr u delim -- addr' u' )
  \ Skip to nth occurrence of delimiter
  >r
  begin
    dup 0> while
    over c@ r@ = if 1 /string r> drop exit then
    1 /string
  repeat
  r> drop ;

: field-length ( addr u delim -- len )
  \ Length until delimiter or end
  >r 0 -rot
  0 ?do
    dup i + c@ r@ = if drop r> drop unloop exit then
    swap 1+ swap
  loop
  drop r> drop ;

: parse-delim ( addr u n delim -- addr u field-addr field-u )
  \ Parse nth field (0-based) with given delimiter
  >r >r 2dup r>
  0 ?do
    r@ skip-to-delim
    dup 0> if 1 /string then
  loop
  2dup r> field-length >r drop r> ;

: parse-pipe ( addr u n -- addr u field-addr field-u )
  \ Parse pipe-delimited field
  [char] | parse-delim ;

: parse-tab ( addr u n -- addr u field-addr field-u )
  \ Parse tab-delimited field
  9 parse-delim ;

: parse-comma ( addr u n -- addr u field-addr field-u )
  \ Parse comma-delimited field
  [char] , parse-delim ;
