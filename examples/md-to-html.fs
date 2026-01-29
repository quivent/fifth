\ fifth/examples/md-to-html.fs - Generate styled HTML from markdown READMEs
\ Usage: fifth examples/md-to-html.fs <example-dir>
\ Reads README.md, outputs index.html with minimalist Fifth styling

\ ============================================================================
\ Configuration
\ ============================================================================

: category$ ( -- addr u )
  \ Detect category from path or default
  s" Agentic AI" ;

\ ============================================================================
\ HTML Template Parts
\ ============================================================================

: emit-head ( title$ -- )
  \ Emit HTML head with title
  s" <!DOCTYPE html>" type cr
  s" <html lang=\"en\">" type cr
  s" <head>" type cr
  s"   <meta charset=\"UTF-8\">" type cr
  s"   <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" type cr
  s"   <title>" type type s"  — Fifth</title>" type cr
  s"   <link rel=\"icon\" type=\"image/svg+xml\" href=\"../../brand/favicon.svg\">" type cr
  s"   <link href=\"https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=JetBrains+Mono:wght@400&display=swap\" rel=\"stylesheet\">" type cr
  s"   <style>" type cr
  \ Minified CSS
  s" :root{--black:#0a0a0a;--gray-950:#0f0f0f;--gray-900:#171717;--gray-800:#262626;--gray-700:#404040;--gray-500:#737373;--gray-400:#a3a3a3;--gray-100:#f5f5f5;--accent:#6366f1;--font-sans:'Inter',system-ui,sans-serif;--font-mono:'JetBrains Mono',monospace}" type cr
  s" *{box-sizing:border-box;margin:0;padding:0}" type cr
  s" body{font-family:var(--font-sans);background:var(--black);color:var(--gray-100);line-height:1.6}" type cr
  s" .nav{position:fixed;top:0;left:0;right:0;z-index:100;padding:1rem 1.5rem;background:rgba(10,10,10,.8);backdrop-filter:blur(12px);border-bottom:1px solid var(--gray-900);display:flex;align-items:center;justify-content:space-between}" type cr
  s" .nav-brand{display:flex;align-items:center;gap:.75rem;text-decoration:none;color:var(--gray-100)}" type cr
  s" .nav-brand svg{width:24px;height:24px}.nav-brand span{font-weight:600}" type cr
  s" .nav-back{font-size:.875rem;color:var(--gray-400);text-decoration:none;display:flex;align-items:center;gap:.5rem}" type cr
  s" .nav-back:hover{color:var(--gray-100)}" type cr
  s" .container{max-width:800px;margin:0 auto;padding:8rem 1.5rem 4rem}" type cr
  s" .badge{display:inline-block;padding:.25rem .75rem;background:var(--gray-900);border:1px solid var(--gray-800);border-radius:100px;font-size:.75rem;color:var(--gray-400);text-transform:uppercase;letter-spacing:.05em;margin-bottom:1.5rem}" type cr
  s" h1{font-size:2.5rem;font-weight:600;letter-spacing:-.03em;margin-bottom:1rem}" type cr
  s" .lead{font-size:1.125rem;color:var(--gray-400);margin-bottom:2rem;line-height:1.7}" type cr
  s" h2{font-size:.75rem;font-weight:500;text-transform:uppercase;letter-spacing:.1em;color:var(--gray-500);margin:2.5rem 0 1rem;padding-bottom:.75rem;border-bottom:1px solid var(--gray-800)}" type cr
  s" h3{font-size:1rem;font-weight:600;color:var(--gray-100);margin:1.5rem 0 .75rem}" type cr
  s" p{color:var(--gray-400);margin:.75rem 0}" type cr
  s" ul,ol{margin:1rem 0;padding-left:1.5rem}" type cr
  s" li{margin:.5rem 0;color:var(--gray-400)}" type cr
  s" li strong,li code{color:var(--gray-100)}" type cr
  s" strong{color:var(--gray-100);font-weight:500}" type cr
  s" a{color:var(--accent);text-decoration:none}" type cr
  s" a:hover{text-decoration:underline}" type cr
  s" pre{background:var(--gray-950);border:1px solid var(--gray-800);border-radius:8px;padding:1rem;overflow-x:auto;margin:1rem 0}" type cr
  s" code{font-family:var(--font-mono);font-size:.875rem}" type cr
  s" p code,li code{background:var(--gray-900);padding:.125rem .375rem;border-radius:4px;font-size:.8125rem}" type cr
  s" .run-command{display:flex;align-items:center;gap:1rem;padding:1rem 1.5rem;background:var(--gray-950);border:1px solid var(--gray-800);border-radius:8px;margin:1.5rem 0}" type cr
  s" .run-command code{flex:1;color:var(--gray-100)}" type cr
  s" .run-command .hint{font-size:.75rem;color:var(--gray-500)}" type cr
  s" .feature-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:1rem;margin:1.5rem 0}" type cr
  s" .feature{padding:1.25rem;background:var(--gray-950);border:1px solid var(--gray-800);border-radius:8px}" type cr
  s" .feature h4{font-size:.875rem;font-weight:600;color:var(--gray-100);margin-bottom:.5rem}" type cr
  s" .feature p{font-size:.875rem;color:var(--gray-500);margin:0}" type cr
  s" .architecture{background:var(--gray-950);border:1px solid var(--gray-800);border-radius:8px;padding:1.5rem;margin:1.5rem 0}" type cr
  s" .architecture pre{background:transparent;border:none;padding:0;margin:0;font-size:.8125rem;line-height:1.5}" type cr
  s" .footer{margin-top:4rem;padding-top:2rem;border-top:1px solid var(--gray-800);text-align:center}" type cr
  s" .footer p{font-size:.875rem;color:var(--gray-500)}" type cr
  s" blockquote{border-left:3px solid var(--gray-700);padding-left:1rem;margin:1rem 0;color:var(--gray-400);font-style:italic}" type cr
  s"   </style>" type cr
  s" </head>" type cr ;

: emit-nav ( -- )
  s" <body>" type cr
  s"   <nav class=\"nav\">" type cr
  s"     <a href=\"../showcase/index.html\" class=\"nav-brand\">" type cr
  s"       <svg viewBox=\"0 0 32 32\" fill=\"none\"><path d=\"M6 8L16 24L26 8\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M10 8H22\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" opacity=\"0.3\"/></svg>" type cr
  s"       <span>Fifth</span>" type cr
  s"     </a>" type cr
  s"     <a href=\"../showcase/index.html\" class=\"nav-back\">" type cr
  s"       <svg width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\"><path d=\"M10 12L6 8L10 4\"/></svg>" type cr
  s"       All Examples" type cr
  s"     </a>" type cr
  s"   </nav>" type cr
  s"   <main class=\"container\">" type cr ;

: emit-badge ( category$ -- )
  s"     <div class=\"badge\">" type type s" </div>" type cr ;

: emit-footer ( -- )
  s"     <div class=\"footer\"><p>Part of <a href=\"../showcase/index.html\">Fifth Examples</a></p></div>" type cr
  s"   </main>" type cr
  s" </body>" type cr
  s" </html>" type cr ;

\ ============================================================================
\ Markdown Parsing State
\ ============================================================================

variable in-code-block
variable in-list
variable first-para    \ Track if we've seen first paragraph (for lead class)
variable seen-h1

: reset-state ( -- )
  0 in-code-block !
  0 in-list !
  -1 first-para !
  0 seen-h1 ! ;

\ ============================================================================
\ String Helpers
\ ============================================================================

: starts-with? ( addr u prefix$ -- flag )
  \ Check if string starts with prefix
  2>r 2dup 2r@ nip min 2r> drop -rot compare 0= ;

: skip-prefix ( addr u n -- addr' u' )
  \ Skip first n characters
  /string ;

: trim-leading ( addr u -- addr' u' )
  \ Skip leading whitespace
  begin dup 0> while
    over c@ dup bl = swap 9 = or if 1 /string else exit then
  repeat ;

: html-escape-char ( c -- )
  case
    [char] < of s" &lt;" type endof
    [char] > of s" &gt;" type endof
    [char] & of s" &amp;" type endof
    [char] " of s" &quot;" type endof
    dup emit
  endcase ;

: html-escape ( addr u -- )
  \ Escape HTML special characters
  0 ?do dup i + c@ html-escape-char loop drop ;

: process-inline ( addr u -- )
  \ Process inline markdown (bold, code, links) and output HTML
  \ Simple version: just escape and output
  \ TODO: Handle **bold**, `code`, [links](url)
  html-escape ;

\ ============================================================================
\ Line Processing
\ ============================================================================

: process-h1 ( addr u -- )
  \ # Title -> <h1>Title</h1>
  2 skip-prefix trim-leading
  s" <h1>" type process-inline s" </h1>" type cr
  -1 seen-h1 ! ;

: process-h2 ( addr u -- )
  \ ## Section -> <h2>Section</h2>
  3 skip-prefix trim-leading
  s" <h2>" type process-inline s" </h2>" type cr ;

: process-h3 ( addr u -- )
  \ ### Subsection -> <h3>Subsection</h3>
  4 skip-prefix trim-leading
  s" <h3>" type process-inline s" </h3>" type cr ;

: process-list-item ( addr u -- )
  \ - Item -> <li>Item</li>
  2 skip-prefix trim-leading
  in-list @ 0= if
    s" <ul>" type cr
    -1 in-list !
  then
  s" <li>" type process-inline s" </li>" type cr ;

: close-list ( -- )
  in-list @ if
    s" </ul>" type cr
    0 in-list !
  then ;

: process-code-fence ( addr u -- )
  \ ``` or ```lang
  in-code-block @ if
    s" </code></pre>" type cr
    0 in-code-block !
  else
    s" <pre><code>" type cr
    -1 in-code-block !
  then ;

: process-code-line ( addr u -- )
  \ Inside code block - escape and output
  html-escape cr ;

: process-paragraph ( addr u -- )
  \ Regular text -> <p>text</p>
  dup 0= if 2drop exit then
  first-para @ if
    s" <p class=\"lead\">" type process-inline s" </p>" type cr
    0 first-para !
  else
    s" <p>" type process-inline s" </p>" type cr
  then ;

: process-line ( addr u -- )
  \ Dispatch based on line content
  dup 0= if
    \ Empty line - close any open list
    close-list
    2drop exit
  then

  in-code-block @ if
    2dup s" ```" starts-with? if
      2drop process-code-fence
    else
      process-code-line
    then
    exit
  then

  2dup s" ```" starts-with? if process-code-fence 2drop exit then
  2dup s" ### " starts-with? if close-list process-h3 exit then
  2dup s" ## " starts-with? if close-list first-para @ if -1 first-para ! then process-h2 exit then
  2dup s" # " starts-with? if close-list process-h1 exit then
  2dup s" - " starts-with? if process-list-item 2drop exit then
  2dup s" * " starts-with? if process-list-item 2drop exit then

  \ Regular paragraph
  close-list
  process-paragraph ;

\ ============================================================================
\ File Processing
\ ============================================================================

256 constant line-max
create line-buf line-max allot

: read-line-safe ( fid -- addr u flag )
  \ Read a line, return string and flag (true if got line)
  line-buf line-max rot read-line throw
  line-buf swap rot ;

: get-title ( addr u -- addr' u' )
  \ Extract title from first # line
  begin
    dup 0> while
    2dup s" # " starts-with? if
      2 skip-prefix
      \ Find end of line
      2dup 10 scan drop over -
      nip swap
      exit
    then
    \ Skip to next line
    2dup 10 scan
    dup 0> if 1 /string then
    nip swap drop swap
  repeat ;

: process-readme ( addr u -- )
  \ Process entire README content
  reset-state
  begin dup 0> while
    \ Find end of current line
    2dup 10 scan
    2dup 2>r drop over -
    \ Process line (without newline)
    process-line
    \ Advance past newline
    2r> dup 0> if 1 /string then
    nip swap drop swap
  repeat
  close-list
  2drop ;

\ ============================================================================
\ Main Entry
\ ============================================================================

: slurp-file ( addr u -- content$ )
  \ Read entire file into memory
  r/o open-file throw >r
  r@ file-size throw drop  \ Get size (ignore high word)
  dup allocate throw       \ Allocate buffer
  swap 2dup r@ read-file throw drop
  r> close-file throw ;

: generate-html ( dir$ -- )
  \ Generate index.html from README.md in directory
  2dup 2>r

  \ Read README.md
  str-reset
  2r@ str+ s" /README.md" str+
  str$ slurp-file
  2dup 2>r

  \ Extract title
  2dup get-title 2>r

  \ Open output file
  str-reset
  2r> 2r> 2drop 2>r  \ Save content
  2r@ 2r> 2>r str+ s" /index.html" str+
  str$ w/o create-file throw >r

  \ Redirect output to file
  r@ to outfile-id

  \ Emit HTML
  2r@ get-title emit-head
  emit-nav
  category$ emit-badge
  2r> process-readme
  emit-footer

  \ Close file
  stdout to outfile-id
  r> close-file throw

  2r> 2drop  \ Clean up dir

  s" Generated index.html" type cr ;

: main ( -- )
  argc @ 2 < if
    s" Usage: fifth examples/md-to-html.fs <example-directory>" type cr
    bye
  then
  1 argv generate-html ;

main
