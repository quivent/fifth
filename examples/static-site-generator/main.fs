\ fifth/examples/static-site-generator/main.fs
\ Static site generator - markdown to HTML

require ~/.fifth/lib/core.fs

\ Configuration
: posts-dir  ( -- addr u ) s" posts/" ;
: dist-dir   ( -- addr u ) s" dist/" ;
: site-title ( -- addr u ) s" My Site" ;

\ --- Template Helpers ---

: page-header ( title-addr title-u -- )
  \ Generate HTML header with title
  site-title html-head
  <style>
  s" body { font-family: system-ui; max-width: 800px; margin: 0 auto; padding: 2rem; }" raw nl
  s" nav { margin-bottom: 2rem; }" raw nl
  s" nav a { margin-right: 1rem; }" raw nl
  s" article { line-height: 1.6; }" raw nl
  </style>
  html-body
  <nav>
    s" <a href=" raw q s" index.html" raw q s" >" raw s" Home" text s" </a>" raw nl
  </nav> ;

: page-footer ( -- )
  <footer>
    s" <p>Generated with Fifth</p>" raw nl
  </footer>
  html-end ;

\ --- Markdown Processing ---

: md>html ( src-addr src-u dest-addr dest-u -- )
  \ Convert markdown file to HTML using pandoc
  \ pandoc -f markdown -t html src > dest
  str-reset
  s" pandoc -f markdown -t html " str+
  2swap str+  \ source file
  s"  > " str+
  str+        \ dest file
  str$ system drop ;

: process-post ( filename-addr filename-u -- )
  \ Convert single post: posts/foo.md -> dist/foo.html
  \ TODO: Extract title from frontmatter
  \ TODO: Generate with template wrapper
  2dup type s"  -> " type

  \ Build output path
  str-reset
  dist-dir str+
  2swap str+
  \ Replace .md with .html
  str$
  2dup + 3 - s" html" rot swap move  \ crude extension swap
  type cr ;

: scan-posts ( -- )
  \ Find all .md files in posts/
  \ TODO: Shell to find/ls and iterate
  s" Scanning posts directory..." type cr
  s" example.md" process-post ;

\ --- Index Generation ---

: generate-index ( -- )
  \ Create index.html with post listing
  str-reset dist-dir str+ s" index.html" str+ str$
  w/o create-file throw html>file

  s" Posts" page-header
  <main>
    <h1> s" Posts" text </h1>
    <ul>
      \ TODO: List all posts with links
      <li> s" <a href=" raw q s" example.html" raw q s" >" raw
           s" Example Post" text s" </a>" raw </li> nl
    </ul>
  </main>
  page-footer

  html-fid @ close-file throw
  s" Generated: dist/index.html" type cr ;

\ --- Main ---

: ensure-dist ( -- )
  s" mkdir -p dist" system drop ;

: build-site ( -- )
  s" Building static site..." type cr
  ensure-dist
  scan-posts
  generate-index
  s" Done!" type cr ;

build-site
bye
