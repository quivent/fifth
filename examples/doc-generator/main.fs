\ fifth/examples/doc-generator/main.fs
\ Documentation generator from source comments

require ~/.fifth/lib/core.fs

\ Configuration
: output-dir ( -- addr u ) s" output/" ;

\ Data structures
256 constant max-word-name
256 constant max-stack-effect
1024 constant max-description

create current-word max-word-name allot
create current-stack max-stack-effect allot
create current-desc max-description allot

variable word-count

\ --- Comment Parsing ---

: starts-with? ( addr u prefix-addr prefix-u -- flag )
  2>r 2dup 2r@ nip <= if
    2r> 2drop 2drop false exit
  then
  2r@ compare 0= 2r> 2drop ;

: is-doc-comment? ( addr u -- flag )
  s" \\ " starts-with? ;

: extract-word-def ( addr u -- )
  \ Parse ": word-name" line
  \ TODO: Extract word name
  2drop ;

: extract-stack-effect ( addr u -- )
  \ Parse "( before -- after )" from line
  \ TODO: Extract stack comment
  2drop ;

: extract-description ( addr u -- )
  \ Accumulate description lines
  \ TODO: Append to current-desc
  2drop ;

\ --- HTML Generation ---

: doc-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 900px; margin: 0 auto; padding: 2rem; }" raw nl
  s" .word { margin-bottom: 2rem; padding: 1rem; border: 1px solid #ddd; border-radius: 4px; }" raw nl
  s" .word-name { font-family: monospace; font-size: 1.2rem; font-weight: bold; color: #0066cc; }" raw nl
  s" .stack-effect { font-family: monospace; color: #666; margin: 0.5rem 0; }" raw nl
  s" .description { line-height: 1.6; }" raw nl
  s" .toc { background: #f5f5f5; padding: 1rem; margin-bottom: 2rem; }" raw nl
  s" .toc a { display: block; padding: 0.25rem 0; }" raw nl
  </style> ;

: word-entry ( name-addr name-u stack-addr stack-u desc-addr desc-u -- )
  \ Generate HTML for a single word
  <div.> s" word" raw q s"  id=" raw q 2>r 2>r 2dup raw q s" >" raw nl
  <div.> s" word-name" raw q s" >" raw text </div> nl
  2r> <div.> s" stack-effect" raw q s" >" raw text </div> nl
  2r> <div.> s" description" raw q s" >" raw text </div> nl
  </div> nl ;

: generate-index ( -- )
  str-reset output-dir str+ s" index.html" str+ str$
  w/o create-file throw html>file

  s" API Documentation" html-head
  doc-styles
  html-body

  <h1> s" API Documentation" text </h1>

  <div.> s" toc" raw q s" >" raw nl
    <h2> s" Table of Contents" text </h2>
    \ TODO: Generate TOC from parsed words
    s" <a href=" raw q s" #str-reset" raw q s" >str-reset</a>" raw nl
    s" <a href=" raw q s" #str+" raw q s" >str+</a>" raw nl
    s" <a href=" raw q s" #str$" raw q s" >str$</a>" raw nl
  </div> nl

  <main>
    \ Example entries
    s" str-reset" s" ( -- )" s" Reset the primary string buffer to empty." word-entry
    s" str+" s" ( addr u -- )" s" Append string to primary buffer." word-entry
    s" str$" s" ( -- addr u )" s" Return contents of primary buffer as string." word-entry
  </main>

  html-end
  html-fid @ close-file throw ;

\ --- File Scanning ---

256 constant line-len
create line-buf line-len allot
variable scan-fid

: scan-file ( filename-addr filename-u -- )
  s" Scanning: " type 2dup type cr
  r/o open-file throw scan-fid !

  begin
    line-buf line-len scan-fid @ read-line throw
  while
    line-buf swap
    2dup is-doc-comment? if
      extract-description
    else
      2drop
    then
  repeat
  drop

  scan-fid @ close-file throw ;

: scan-directory ( path-addr path-u -- )
  \ Find all .fs files and scan them
  s" Scanning directory: " type type cr
  \ TODO: Shell to find and iterate
  ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: usage ( -- )
  s" Documentation Generator" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth doc-generator/main.fs <path>" type cr
  s" " type cr
  s" Generates HTML documentation from Forth source files." type cr ;

: main ( -- )
  ensure-output
  0 word-count !

  argc @ 2 < if
    \ Generate sample documentation
    generate-index
    s" Generated: output/index.html" type cr
  else
    1 argv scan-directory
    generate-index
    s" Generated documentation for " type word-count @ . s"  words" type cr
  then ;

main
bye
