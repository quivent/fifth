\ fifth/lib/template.fs - Template & Layout System
\ Deferred slots, layouts, component composition

require ~/fifth/lib/html.fs

\ ============================================================
\ Deferred Slots (for template inheritance)
\ ============================================================

\ Create a slot that can be filled later
: slot: ( "name" -- )
  create ['] noop ,
  does> @ execute ;

\ Fill a slot with an xt
: ->slot ( xt "name" -- )
  ' >body ! ;

\ ============================================================
\ Content Blocks (capture HTML to string)
\ ============================================================

\ For capturing output to a variable instead of file
\ (Useful for reordering content or conditional output)

variable capture-mode
variable capture-fid
s" /tmp/fifth-capture.html" 2constant capture-file

: begin-capture ( -- )
  capture-file w/o create-file throw capture-fid !
  html-fid @ >r capture-fid @ html>file
  r> capture-mode ! ;

: end-capture ( -- addr u )
  capture-fid @ close-file throw
  capture-mode @ html>file
  capture-file slurp-file ;

\ ============================================================
\ Conditional Rendering
\ ============================================================

: ?render ( flag xt -- )
  \ Execute xt only if flag is true
  swap if execute else drop then ;

: ?text ( flag addr u -- )
  \ Output text only if flag is true
  rot if text else 2drop then ;

: ?raw ( flag addr u -- )
  \ Output raw only if flag is true
  rot if raw else 2drop then ;

\ ============================================================
\ Iteration Helpers
\ ============================================================

: times ( n xt -- )
  \ Execute xt n times, with index on stack
  swap 0 ?do dup i swap execute loop drop ;

\ ============================================================
\ Component Definition Pattern
\ ============================================================

\ Components are just words that output HTML
\ Convention: component names end with -c

\ Example component definition:
\ : card-c ( title$ content-xt -- )
\   s" card" <div.>
\     <h3> 2swap text </h3>
\     execute
\   </div>nl ;

\ Usage:
\ s" My Title" ['] my-content card-c

\ ============================================================
\ Layout Definition Pattern
\ ============================================================

\ Layouts use slots for injectable content
\ Convention: layout names end with -layout

\ Example:
\ slot: @header
\ slot: @main
\ slot: @footer
\
\ : page-layout ( -- )
\   <body>
\     <header> @header </header>
\     <main> @main </main>
\     <footer> @footer </footer>
\   </body> ;
\
\ : my-header s" Welcome" h1. ;
\ ' my-header ->slot @header

\ ============================================================
\ Fragment Helper
\ ============================================================

\ Define a fragment (reusable HTML snippet)
: fragment: ( "name" -- )
  : ;

\ ============================================================
\ Data-Driven Rendering
\ ============================================================

\ Render list with separator
: join-render ( n xt sep$ -- )
  \ Render n items using xt, with separator between
  2>r >r
  r@ 0 ?do
    i 0> if 2r@ raw then
    dup i swap execute
  loop
  drop r> drop 2r> 2drop ;
