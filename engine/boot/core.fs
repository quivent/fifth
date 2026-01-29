\ boot/core.fs - Fifth Bootstrap
\ Defines high-level words on top of C primitives.
\ Loaded automatically at engine startup.

\ ============================================================
\ Defining Words
\ ============================================================

\ VARIABLE ( "name" -- ) Create a variable with initial value 0
: variable  create 0 , ;

\ CONSTANT ( x "name" -- ) Create a constant
: constant  create , does> @ ;

\ 2CONSTANT ( x1 x2 "name" -- ) Create a double constant
: 2constant  create , , does> dup cell+ @ swap @ ;

\ VALUE ( x "name" -- ) Like constant but mutable with TO
\ Simplified: just use variable semantics
: value  create , does> @ ;

\ DEFER ( "name" -- ) Create a deferred word
: defer  create ['] noop , does> @ execute ;

\ IS ( xt "name" -- ) Set a deferred word
: is  ' >body ! ;

\ ============================================================
\ String Comparison
\ ============================================================

\ str= ( addr1 u1 addr2 u2 -- flag )
\ Compare two strings for equality
: str=  ( a1 u1 a2 u2 -- flag )
  rot over <> if 2drop 2drop false exit then
  ( a1 a2 u2 )
  0 ?do
    over i + c@
    over i + c@
    <> if 2drop false unloop exit then
  loop
  2drop true ;

\ ============================================================
\ Numeric Utilities
\ ============================================================

\ Convert number to string ( n -- addr u )
: n>str  ( n -- addr u )
  dup abs
  0 <# #s rot sign #>
;

\ ============================================================
\ Output Helpers
\ ============================================================

\ Emit n newlines
: nls  ( n -- ) 0 ?do cr loop ;

\ Print a string followed by newline
: print  ( addr u -- ) type cr ;

\ ============================================================
\ Stack Utilities
\ ============================================================

\ 2ROT ( x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2 )
: 2rot  2>r 2swap 2r> 2swap ;

\ ============================================================
\ Memory Utilities
\ ============================================================

\ ERASE ( addr u -- ) Fill memory with zeros
: erase  0 fill ;

\ BLANK ( addr u -- ) Fill memory with spaces
: blank  bl fill ;

\ ============================================================
\ Boolean
\ ============================================================

\ NOT is an alias for 0=
: not  0= ;

\ ============================================================
\ Version
\ ============================================================

: .fifth  ." Fifth Engine v0.1.0" cr ;
