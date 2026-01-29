\ fifth/examples/api-client/main.fs
\ REST API client with caching

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" cache.db" ;
: default-ttl ( -- seconds ) 3600 ;  \ 1 hour

\ Response buffer
8192 constant max-response
create response-buf max-response allot
variable response-len

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"CREATE TABLE IF NOT EXISTS cache (" str+
  s"   url TEXT PRIMARY KEY," str+
  s"   response TEXT," str+
  s"   fetched_at TEXT DEFAULT CURRENT_TIMESTAMP," str+
  s"   ttl_seconds INTEGER" str+
  s" );\"" str+
  str$ system drop ;

\ --- HTTP Primitives ---

: http-get ( url-addr url-u -- )
  \ Fetch URL with curl, result in response-buf
  str-reset
  s" curl -s '" str+
  str+
  s" '" str+
  str$ system drop
  \ TODO: Capture output properly
  s" {}" response-buf swap move
  2 response-len ! ;

: http-post ( url-addr url-u data-addr data-u -- )
  \ POST with JSON body
  str-reset
  s" curl -s -X POST -H 'Content-Type: application/json' -d '" str+
  str+  \ data
  s" ' '" str+
  str+  \ url
  s" '" str+
  str$ system drop ;

\ --- Cache Operations ---

: cache-get ( url-addr url-u -- response-addr response-u found? )
  \ Check cache for URL
  \ TODO: Query database
  2drop s" " false ;

: cache-set ( url-addr url-u response-addr response-u ttl -- )
  \ Store response in cache
  \ TODO: Insert/update database
  drop 2drop 2drop ;

: cached-get ( url-addr url-u ttl -- )
  \ Get with caching
  >r 2dup cache-get if
    2swap 2drop r> drop
    s" (from cache)" type cr
  else
    2drop
    2dup http-get
    response-buf response-len @ r> cache-set
    s" (fetched)" type cr
  then ;

: clear-cache ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"DELETE FROM cache;\"" str+
  str$ system drop
  s" Cache cleared" type cr ;

: show-cache ( -- )
  s" Cached URLs:" type cr
  s" ============" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT url, ttl_seconds, fetched_at FROM cache;\"" str+
  str$ system drop ;

\ --- API: JSONPlaceholder (Demo API) ---

: api-posts ( -- )
  s" Fetching posts..." type cr
  s" https://jsonplaceholder.typicode.com/posts?_limit=5" default-ttl cached-get
  response-buf response-len @ type cr ;

: api-post ( id -- )
  s" Fetching post #" type dup . cr
  str-reset
  s" https://jsonplaceholder.typicode.com/posts/" str+
  0 <# #s #> str+
  str$ default-ttl cached-get
  response-buf response-len @ type cr ;

: api-users ( -- )
  s" Fetching users..." type cr
  s" https://jsonplaceholder.typicode.com/users?_limit=5" default-ttl cached-get
  response-buf response-len @ type cr ;

\ --- API: GitHub ---

: github-user ( username-addr username-u -- )
  s" Fetching GitHub user: " type 2dup type cr
  str-reset
  s" https://api.github.com/users/" str+
  str+
  str$ default-ttl cached-get
  response-buf response-len @ type cr ;

: github-repos ( username-addr username-u -- )
  s" Fetching repos for: " type 2dup type cr
  str-reset
  s" https://api.github.com/users/" str+
  str+
  s" /repos?per_page=5" str+
  str$ default-ttl cached-get
  response-buf response-len @ type cr ;

\ --- API: Weather (wttr.in) ---

: weather ( location-addr location-u -- )
  s" Weather for: " type 2dup type cr
  str-reset
  s" https://wttr.in/" str+
  str+
  s" ?format=3" str+
  str$ default-ttl cached-get
  response-buf response-len @ type cr ;

\ --- Main ---

: usage ( -- )
  s" API Client" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth api-client/main.fs posts              - List posts" type cr
  s"   ./fifth api-client/main.fs post <id>          - Get post by ID" type cr
  s"   ./fifth api-client/main.fs users              - List users" type cr
  s"   ./fifth api-client/main.fs github <username>  - GitHub user info" type cr
  s"   ./fifth api-client/main.fs repos <username>   - GitHub repos" type cr
  s"   ./fifth api-client/main.fs weather <city>     - Weather info" type cr
  s"   ./fifth api-client/main.fs cache              - Show cache" type cr
  s"   ./fifth api-client/main.fs clear              - Clear cache" type cr ;

: main ( -- )
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" posts" compare 0= if 2drop api-posts exit then
  2dup s" users" compare 0= if 2drop api-users exit then
  2dup s" cache" compare 0= if 2drop show-cache exit then
  2dup s" clear" compare 0= if 2drop clear-cache exit then
  2dup s" post" compare 0= if
    2drop
    argc @ 3 < if s" Usage: post <id>" type cr exit then
    2 argv drop c@ [char] 0 - api-post
    exit
  then
  2dup s" github" compare 0= if
    2drop
    argc @ 3 < if s" Usage: github <username>" type cr exit then
    2 argv github-user
    exit
  then
  2dup s" repos" compare 0= if
    2drop
    argc @ 3 < if s" Usage: repos <username>" type cr exit then
    2 argv github-repos
    exit
  then
  2dup s" weather" compare 0= if
    2drop
    argc @ 3 < if s" Usage: weather <city>" type cr exit then
    2 argv weather
    exit
  then
  2drop usage ;

main
bye
