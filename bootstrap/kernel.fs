\ kernel.fs - Build Fifth from seed primitives
\
\ This file runs on the seed and defines all the words
\ needed for a complete Forth system.
\
\ Seed provides: @ ! c@ c! + - * / and or xor <
\               emit key dup drop swap over rot
\               >r r> r@ here , c, allot syscall

\ ============================================================================
\ Constants
\ ============================================================================

0 dup dup or invert constant -1   \ All bits set
-1 1 xor constant -2
0 1 - constant true
0 constant false

8 constant cell
1 constant char

\ ============================================================================
\ Stack manipulation (built from primitives)
\ ============================================================================

: nip   ( a b -- b )       swap drop ;
: tuck  ( a b -- b a b )   swap over ;
: 2dup  ( a b -- a b a b ) over over ;
: 2drop ( a b -- )         drop drop ;
: 2swap ( a b c d -- c d a b ) rot >r rot r> ;
: 2over ( a b c d -- a b c d a b ) >r >r 2dup r> r> 2swap ;

: ?dup  ( x -- x x | 0 )   dup 0 < invert if dup then ;

\ ============================================================================
\ Arithmetic (built from primitives)
\ ============================================================================

: 1+    ( n -- n+1 )    1 + ;
: 1-    ( n -- n-1 )    1 - ;
: 2+    ( n -- n+2 )    2 + ;
: 2-    ( n -- n-2 )    2 - ;
: 2*    ( n -- n*2 )    1 lshift ;
: 2/    ( n -- n/2 )    1 rshift ;

: negate ( n -- -n )    0 swap - ;
: abs    ( n -- |n| )   dup 0 < if negate then ;

: min   ( a b -- min )  2dup < if drop else nip then ;
: max   ( a b -- max )  2dup < if nip else drop then ;

: cells ( n -- n*cell ) cell * ;
: chars ( n -- n )      ;  \ chars are 1 byte

\ ============================================================================
\ Comparison (built from <)
\ ============================================================================

: 0<    ( n -- flag )   0 < ;
: 0=    ( n -- flag )   0 = ;
: 0>    ( n -- flag )   0 swap < ;
: =     ( a b -- flag ) - 0= ;
: <>    ( a b -- flag ) = invert ;
: >     ( a b -- flag ) swap < ;
: <=    ( a b -- flag ) > invert ;
: >=    ( a b -- flag ) < invert ;

: within ( n lo hi -- flag ) over - >r - r> < ;

\ ============================================================================
\ Bitwise (built from and or xor)
\ ============================================================================

: invert ( x -- ~x )    -1 xor ;
: lshift ( x n -- x<<n ) 0 ?do 2* loop ;
: rshift ( x n -- x>>n ) 0 ?do 2/ loop ;

\ ============================================================================
\ Memory
\ ============================================================================

: +!    ( n addr -- )   dup @ rot + swap ! ;
: c+!   ( c addr -- )   dup c@ rot + swap c! ;

: fill  ( addr n c -- )
    rot rot 0 ?do 2dup i + c! loop 2drop ;

: erase ( addr n -- )   0 fill ;

: move  ( src dst n -- )
    >r 2dup < if
        r> 0 ?do
            over i + c@
            over i + c!
        loop
    else
        r> dup >r 1- 0 swap do
            over i + c@
            over i + c!
        -1 +loop r>
    then 2drop ;

\ ============================================================================
\ I/O
\ ============================================================================

: cr    ( -- )          10 emit ;
: space ( -- )          32 emit ;
: bl    ( -- c )        32 ;

: type  ( addr n -- )
    0 ?do dup i + c@ emit loop drop ;

: count ( addr -- addr+1 n )
    dup 1+ swap c@ ;

\ Print number (simple version)
: .     ( n -- )
    dup 0 < if 45 emit negate then  \ minus sign
    dup 10 < if
        48 + emit
    else
        dup 10 / recurse
        10 mod 48 + emit
    then ;

\ ============================================================================
\ Control structures (need compiler support)
\ ============================================================================

\ These would be IMMEDIATE words that compile branches.
\ Sketched here for completeness:
\
\ : if    ( -- addr )    ['] 0branch , here 0 , ; immediate
\ : then  ( addr -- )    here swap ! ; immediate
\ : else  ( addr -- addr ) ['] branch , here 0 , swap here swap ! ; immediate
\ : begin ( -- addr )    here ; immediate
\ : until ( addr -- )    ['] 0branch , , ; immediate
\ : while ( addr -- addr addr ) ['] 0branch , here 0 , swap ; immediate
\ : repeat ( addr addr -- ) ['] branch , , here swap ! ; immediate

\ ============================================================================
\ String literals (need compiler support)
\ ============================================================================

\ : s"   ( -- addr n )  ... ; immediate
\ : ."   ( -- )         ... ; immediate

\ ============================================================================
\ Outer interpreter
\ ============================================================================

\ The seed provides a basic interpreter.
\ This would be expanded to support:
\ - Colon definitions
\ - IMMEDIATE words
\ - Compiling state
\ - Error handling

\ ============================================================================
\ File I/O (via syscall)
\ ============================================================================

\ Linux syscall numbers (x86-64)
0 constant sys_read
1 constant sys_write
2 constant sys_open
3 constant sys_close

: write-file ( addr n fd -- ior )
    >r swap r> sys_write syscall ;

: read-file ( addr n fd -- n2 ior )
    >r swap r> sys_read syscall dup 0 < ;

: emit-file ( c fd -- ior )
    >r sp@ 1 r> write-file nip ;

\ ============================================================================
\ Bootstrap message
\ ============================================================================

: banner
    cr
    ." Fifth kernel loaded" cr
    ." Ready for meta.fs" cr
    cr ;

banner
