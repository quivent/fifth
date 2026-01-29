\ fifth/examples/bookmark-manager/main.fs
\ Bookmark manager with full-text search

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" bookmarks.db" ;

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS bookmarks (" str+
  s"   id INTEGER PRIMARY KEY," str+
  s"   url TEXT NOT NULL," str+
  s"   title TEXT," str+
  s"   description TEXT," str+
  s"   tags TEXT," str+
  s"   created_at TEXT DEFAULT CURRENT_TIMESTAMP" str+
  s" );" str+
  s" CREATE VIRTUAL TABLE IF NOT EXISTS bookmarks_fts USING fts5(title, description, tags, content=bookmarks, content_rowid=id);" str+
  s" \"" str+
  str$ system drop ;

\ --- Bookmark Operations ---

: add-bookmark ( url-addr url-u title-addr title-u tags-addr tags-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO bookmarks (url, title, tags) VALUES ('" str+
  2>r 2>r  \ save tags and title
  str+     \ url
  s" ', '" str+
  2r> str+ \ title
  s" ', '" str+
  2r> str+ \ tags
  s" ');\"" str+
  str$ system drop
  s" Bookmark added" type cr ;

: list-bookmarks ( -- )
  s" Bookmarks:" type cr
  s" ----------" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT id, title, url, tags FROM bookmarks ORDER BY created_at DESC LIMIT 20;\"" str+
  str$ system drop ;

: search-bookmarks ( query-addr query-u -- )
  s" Search results for: " type 2dup type cr
  s" -------------------" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT b.id, b.title, b.url FROM bookmarks b JOIN bookmarks_fts f ON b.id = f.rowid WHERE bookmarks_fts MATCH '" str+
  str+
  s" ' ORDER BY rank LIMIT 20;\"" str+
  str$ system drop ;

: list-by-tag ( tag-addr tag-u -- )
  s" Bookmarks tagged: " type 2dup type cr
  s" -----------------" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT id, title, url FROM bookmarks WHERE tags LIKE '%" str+
  str+
  s" %' ORDER BY created_at DESC;\"" str+
  str$ system drop ;

: delete-bookmark ( id -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"DELETE FROM bookmarks WHERE id=" str+
  0 <# #s #> str+
  s" ;\"" str+
  str$ system drop
  s" Bookmark deleted" type cr ;

\ --- HTML Export ---

: export-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 900px; margin: 0 auto; padding: 2rem; background: #f5f5f5; }" raw nl
  s" .bookmark { background: white; padding: 1rem; margin-bottom: 1rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }" raw nl
  s" .bookmark h3 { margin: 0 0 0.5rem 0; }" raw nl
  s" .bookmark a { color: #1976d2; text-decoration: none; }" raw nl
  s" .bookmark a:hover { text-decoration: underline; }" raw nl
  s" .url { color: #666; font-size: 0.85rem; word-break: break-all; }" raw nl
  s" .tags { margin-top: 0.5rem; }" raw nl
  s" .tag { display: inline-block; background: #e3f2fd; color: #1565c0; padding: 0.2rem 0.5rem; border-radius: 4px; font-size: 0.8rem; margin-right: 0.25rem; }" raw nl
  s" .search { width: 100%; padding: 0.75rem; margin-bottom: 1rem; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem; }" raw nl
  </style> ;

: bookmark-card ( title-addr title-u url-addr url-u tags-addr tags-u -- )
  <div.> s" bookmark" raw q s" >" raw nl
  <h3>
    s" <a href=" raw q 2>r 2>r 2dup raw q s"  target=" raw q s" _blank" raw q s" >" raw
    text
    s" </a>" raw
  </h3> nl
  <div.> s" url" raw q s" >" raw 2r> text </div> nl
  <div.> s" tags" raw q s" >" raw
    \ TODO: Split tags and render each
    2r> text
  </div> nl
  </div> nl ;

: export-html ( -- )
  s" output/bookmarks.html" w/o create-file throw html>file

  s" My Bookmarks" html-head
  export-styles
  html-body

  <h1> s" My Bookmarks" text </h1>

  s" <input type=" raw q s" text" raw q
  s"  class=" raw q s" search" raw q
  s"  placeholder=" raw q s" Search bookmarks..." raw q
  s"  id=" raw q s" search" raw q s" >" raw nl

  <div.> s" bookmarks" raw q s" id=" raw q s" bookmarks-list" raw q s" >" raw nl
    \ Sample bookmarks
    s" Forth Documentation" s" https://forth-standard.org" s" forth,reference" bookmark-card
    s" SQLite Tutorial" s" https://sqlite.org/lang.html" s" database,reference" bookmark-card
    s" Linux Kernel" s" https://kernel.org" s" linux,systems" bookmark-card
  </div> nl

  <script>
  s" document.getElementById('search').addEventListener('input', function(e) {" raw nl
  s"   const q = e.target.value.toLowerCase();" raw nl
  s"   document.querySelectorAll('.bookmark').forEach(b => {" raw nl
  s"     b.style.display = b.textContent.toLowerCase().includes(q) ? '' : 'none';" raw nl
  s"   });" raw nl
  s" });" raw nl
  </script>

  html-end
  html-fid @ close-file throw

  s" Exported: output/bookmarks.html" type cr ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: usage ( -- )
  s" Bookmark Manager" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth bookmark-manager/main.fs list                        - List bookmarks" type cr
  s"   ./fifth bookmark-manager/main.fs add <url> <title> <tags>    - Add bookmark" type cr
  s"   ./fifth bookmark-manager/main.fs search <query>              - Search" type cr
  s"   ./fifth bookmark-manager/main.fs tag <tag>                   - Filter by tag" type cr
  s"   ./fifth bookmark-manager/main.fs export                      - Export to HTML" type cr ;

: main ( -- )
  ensure-output
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" list" compare 0= if 2drop list-bookmarks exit then
  2dup s" export" compare 0= if 2drop export-html exit then
  2dup s" search" compare 0= if
    2drop
    argc @ 3 < if s" Usage: search <query>" type cr exit then
    2 argv search-bookmarks
    exit
  then
  2dup s" tag" compare 0= if
    2drop
    argc @ 3 < if s" Usage: tag <tagname>" type cr exit then
    2 argv list-by-tag
    exit
  then
  2dup s" add" compare 0= if
    2drop
    argc @ 5 < if s" Usage: add <url> <title> <tags>" type cr exit then
    2 argv 3 argv 4 argv add-bookmark
    exit
  then
  2drop usage ;

main
bye
