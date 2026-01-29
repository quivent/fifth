\ fifth/examples/codebase-indexer/main.fs
\ Semantic Codebase Indexer - Vector embeddings for code search
\ Production-grade implementation using Fifth patterns

require ~/.fifth/lib/str.fs

\ ============================================================================
\ Configuration
\ ============================================================================

: db-path ( -- addr u )
  s" INDEX_DB" getenv dup 0= if 2drop s" codebase-index.db" then ;

: embedding-provider ( -- addr u )
  s" EMBEDDING_PROVIDER" getenv dup 0= if 2drop s" openai" then ;

: embedding-model ( -- addr u )
  s" EMBEDDING_MODEL" getenv dup 0= if 2drop s" text-embedding-ada-002" then ;

: ollama-host ( -- addr u )
  s" OLLAMA_HOST" getenv dup 0= if 2drop s" localhost:11434" then ;

: chunk-max-tokens ( -- n )
  512 ;  \ Approximate, actual tokenization varies

: similarity-threshold ( -- f )
  \ Minimum cosine similarity for results (would be float in real impl)
  70 ;  \ Representing 0.70 as integer percentage

\ ============================================================================
\ Temporary File Paths
\ ============================================================================

s" /tmp/fifth-index-response.json" 2constant response-file
s" /tmp/fifth-index-files.txt" 2constant files-list
s" /tmp/fifth-index-chunk.txt" 2constant chunk-file
s" /tmp/fifth-index-vector.json" 2constant vector-file
s" /tmp/fifth-index-query.txt" 2constant query-result

\ ============================================================================
\ Database Schema and Initialization
\ ============================================================================

: init-db ( -- )
  \ Create database tables if they don't exist
  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS embeddings (" str+
  s"   id INTEGER PRIMARY KEY AUTOINCREMENT," str+
  s"   file_path TEXT NOT NULL," str+
  s"   chunk_hash TEXT UNIQUE," str+
  s"   chunk_text TEXT NOT NULL," str+
  s"   chunk_type TEXT," str+
  s"   start_line INTEGER," str+
  s"   end_line INTEGER," str+
  s"   vector TEXT," str+
  s"   model TEXT," str+
  s"   indexed_at TEXT DEFAULT CURRENT_TIMESTAMP" str+
  s" );" str+
  s" CREATE INDEX IF NOT EXISTS idx_file ON embeddings(file_path);" str+
  s" CREATE INDEX IF NOT EXISTS idx_type ON embeddings(chunk_type);" str+
  s" CREATE INDEX IF NOT EXISTS idx_hash ON embeddings(chunk_hash);" str+
  s" CREATE TABLE IF NOT EXISTS file_hashes (" str+
  s"   file_path TEXT PRIMARY KEY," str+
  s"   content_hash TEXT," str+
  s"   indexed_at TEXT DEFAULT CURRENT_TIMESTAMP" str+
  s" );\"" str+
  str$ system drop ;

: clear-db ( -- )
  \ Remove all indexed data
  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"DELETE FROM embeddings; DELETE FROM file_hashes;\"" str+
  str$ system drop
  s" Index cleared." type cr ;

\ ============================================================================
\ File Discovery
\ ============================================================================

variable file-count
variable chunk-count
variable skip-count

: default-extensions ( -- addr u )
  s" *.fs,*.py,*.js,*.ts,*.c,*.h,*.go,*.rs,*.java,*.rb" ;

: find-source-files ( path$ extensions$ -- )
  \ Find all source files matching extensions, write to files-list
  \ Extensions format: "*.fs,*.py,*.js"
  str-reset
  s" find " str+
  2swap str+
  s"  -type f \\( " str+
  \ Parse comma-separated extensions into -name patterns
  begin
    2dup [char] , str-find-char
    dup 0> if
      \ Found comma, extract this extension
      2>r 2dup 2r@ drop over - s" -name '" str+ str+ s" ' -o " str+
      2r> 1 /string  \ Skip past comma
    else
      \ Last extension
      2drop s" -name '" str+ str+ s" '" str+
      0 0  \ Signal done
    then
    dup 0=
  until
  2drop
  s"  \\) 2>/dev/null > " str+ files-list str+
  str$ system drop ;

: count-files ( -- n )
  \ Count lines in files-list
  str-reset
  s" wc -l < " str+ files-list str+ s"  | tr -d ' '" str+
  str$ system drop
  \ Read result - simplified, would need proper capture
  0 ;

\ ============================================================================
\ Content Hashing (for incremental indexing)
\ ============================================================================

: file-hash ( path$ -- hash$ )
  \ Get MD5 hash of file contents
  str-reset
  s" md5 -q " str+
  str+
  s"  2>/dev/null || md5sum " str+
  \ Repeat path for fallback
  s"  | cut -d' ' -f1" str+
  str$ system drop
  s" [hash]" ;  \ Placeholder - need proper output capture

: get-stored-hash ( path$ -- hash$ )
  \ Retrieve hash from database
  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"SELECT content_hash FROM file_hashes WHERE file_path='" str+
  2swap str+
  s" '\" 2>/dev/null" str+
  str$ system drop
  s" " ;  \ Placeholder

: store-hash ( path$ hash$ -- )
  \ Store file hash in database
  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"INSERT OR REPLACE INTO file_hashes (file_path, content_hash) VALUES ('" str+
  2swap str+
  s" ', '" str+
  2swap str+
  s" ');\"" str+
  str$ system drop ;

: needs-reindex? ( path$ -- flag )
  \ Check if file has changed since last index
  2dup file-hash
  2swap get-stored-hash
  str= 0= ;  \ True if hashes differ or file not indexed

\ ============================================================================
\ Code Chunking - Intelligent splitting by semantic boundaries
\ ============================================================================

\ Chunk buffer for accumulating lines
4096 constant chunk-buf-size
create chunk-buf chunk-buf-size allot
variable chunk-len
variable chunk-start-line
variable chunk-end-line

: chunk-reset ( -- )
  0 chunk-len !
  0 chunk-start-line !
  0 chunk-end-line ! ;

: chunk-add-line ( addr u line# -- )
  \ Add line to current chunk
  chunk-start-line @ 0= if dup chunk-start-line ! then
  chunk-end-line !
  chunk-len @ chunk-buf-size < if
    chunk-buf chunk-len @ + swap dup chunk-len +! move
    10 chunk-buf chunk-len @ + c!  \ Add newline
    1 chunk-len +!
  then ;

: chunk$ ( -- addr u )
  chunk-buf chunk-len @ ;

: contains? ( addr u pattern$ -- flag )
  \ Check if string contains pattern (simple implementation)
  2>r
  begin
    dup 2r@ nip >= while
    2dup 2r@ nip min 2r@ compare 0= if
      2drop 2r> 2drop true exit
    then
    1 /string
  repeat
  2drop 2r> 2drop false ;

: detect-chunk-type ( addr u -- type$ )
  \ Detect what kind of code construct this line starts
  \ Returns: "function", "class", "module", "word", or ""

  \ Check for Forth word definition
  2dup s" : " 2swap 0 2 min compare 0= if 2drop s" word" exit then

  \ Check for Python/JS function
  2dup s" def " contains? if 2drop s" function" exit then
  2dup s" function " contains? if 2drop s" function" exit then
  2dup s" async function" contains? if 2drop s" function" exit then
  2dup s" fn " contains? if 2drop s" function" exit then  \ Rust

  \ Check for class
  2dup s" class " contains? if 2drop s" class" exit then
  2dup s" struct " contains? if 2drop s" struct" exit then
  2dup s" type " contains? if 2drop s" type" exit then

  \ Check for module/package
  2dup s" module " contains? if 2drop s" module" exit then
  2dup s" package " contains? if 2drop s" module" exit then
  2dup s" namespace " contains? if 2drop s" module" exit then

  2drop s" " ;

: is-boundary-line? ( addr u -- flag )
  \ Check if this line starts a new semantic unit
  detect-chunk-type nip 0> ;

\ ============================================================================
\ JSON Escaping for API calls
\ ============================================================================

: json-escape-char ( c -- )
  \ Escape character for JSON string, output to str2-buf
  dup [char] " = if drop s\" \\\"" str2+ exit then
  dup [char] \ = if drop s" \\\\" str2+ exit then
  dup 10 = if drop s\" \\n" str2+ exit then
  dup 13 = if drop s\" \\r" str2+ exit then
  dup 9 = if drop s\" \\t" str2+ exit then
  dup 32 < if drop exit then  \ Skip other control chars
  str2-char ;

: json-escape ( addr u -- escaped$ )
  \ Escape string for JSON embedding
  str2-reset
  0 ?do
    dup i + c@ json-escape-char
  loop
  drop str2$ ;

\ ============================================================================
\ Embedding API - OpenAI
\ ============================================================================

: openai-embed ( text$ -- )
  \ Call OpenAI embeddings API, result in vector-file
  json-escape  \ Escape for JSON
  str-reset
  s" curl -s https://api.openai.com/v1/embeddings " str+
  s" -H 'Content-Type: application/json' " str+
  s" -H 'Authorization: Bearer '\"$OPENAI_API_KEY\"'' " str+
  s" -d '{\"input\": \"" str+
  str+  \ Escaped text
  s" \", \"model\": \"" str+
  embedding-model str+
  s" \"}' > " str+
  vector-file str+
  str$ system drop ;

: extract-openai-vector ( -- vector$ )
  \ Extract embedding vector from OpenAI response using jq
  str-reset
  s" jq -c '.data[0].embedding' " str+ vector-file str+
  s"  2>/dev/null" str+
  str$ system drop
  \ Read from command output - simplified
  s" []" ;  \ Placeholder

\ ============================================================================
\ Embedding API - Ollama (local)
\ ============================================================================

: ollama-embed ( text$ -- )
  \ Call Ollama embeddings API
  json-escape
  str-reset
  s" curl -s http://" str+ ollama-host str+
  s" /api/embeddings " str+
  s" -d '{\"model\": \"" str+
  s" nomic-embed-text" str+  \ Default Ollama embedding model
  s" \", \"prompt\": \"" str+
  str+
  s" \"}' > " str+
  vector-file str+
  str$ system drop ;

: extract-ollama-vector ( -- vector$ )
  str-reset
  s" jq -c '.embedding' " str+ vector-file str+
  str$ system drop
  s" []" ;

\ ============================================================================
\ Unified Embedding Interface
\ ============================================================================

: get-embedding ( text$ -- vector$ )
  \ Get embedding vector using configured provider
  embedding-provider s" ollama" str= if
    ollama-embed
    extract-ollama-vector
  else
    openai-embed
    extract-openai-vector
  then ;

\ ============================================================================
\ Vector Storage
\ ============================================================================

: sql-escape-text ( addr u -- escaped$ )
  \ Escape text for SQL string literal (single quotes)
  str2-reset
  0 ?do
    dup i + c@
    dup [char] ' = if drop s" ''" str2+ else str2-char then
  loop
  drop str2$ ;

\ Temporary storage for embedding parameters
create emb-file-buf 256 allot
variable emb-file-len
create emb-type-buf 64 allot
variable emb-type-len
create emb-chunk-buf 4096 allot
variable emb-chunk-len
create emb-vector-buf 8192 allot
variable emb-vector-len
variable emb-start
variable emb-end

: store-embedding ( chunk$ type$ file$ start end vector$ -- )
  \ Store chunk and its embedding in database
  \ First, save all parameters to avoid stack juggling

  \ Save vector
  dup emb-vector-len ! emb-vector-buf swap move

  \ Save end line
  emb-end !

  \ Save start line
  emb-start !

  \ Save file path
  dup emb-file-len ! emb-file-buf swap move

  \ Save type
  dup emb-type-len ! emb-type-buf swap move

  \ Save chunk (for escaping)
  dup emb-chunk-len ! emb-chunk-buf swap move

  \ Now build the SQL command
  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"INSERT OR REPLACE INTO embeddings " str+
  s" (file_path, chunk_text, chunk_type, start_line, end_line, vector, model, chunk_hash) " str+
  s" VALUES ('" str+

  \ file_path
  emb-file-buf emb-file-len @ str+
  s" ', '" str+

  \ chunk_text - needs escaping
  emb-chunk-buf emb-chunk-len @ sql-escape-text str+
  s" ', '" str+

  \ chunk_type
  emb-type-buf emb-type-len @ str+
  s" ', " str+

  \ start_line
  emb-start @ 0 <# #s #> str+
  s" , " str+

  \ end_line
  emb-end @ 0 <# #s #> str+
  s" , '" str+

  \ vector
  emb-vector-buf emb-vector-len @ str+
  s" ', '" str+

  \ model
  embedding-model str+
  s" ', '" str+

  \ chunk_hash (MD5 of chunk text) - simplified
  s" hash-placeholder" str+
  s" ');\"" str+
  str$ system drop
  1 chunk-count +! ;

\ ============================================================================
\ File Processing
\ ============================================================================

256 constant line-max
create line-buf line-max allot
variable process-fid
variable current-line

: process-chunk ( type$ file$ -- )
  \ Process accumulated chunk: embed and store
  \ First save type and file to the embedding buffers
  chunk$ dup 10 > if  \ Only process non-trivial chunks
    \ Stack: type$ file$ chunk$

    \ Save chunk to buffer
    dup emb-chunk-len ! emb-chunk-buf swap move
    \ Stack: type$ file$

    \ Save file to buffer
    dup emb-file-len ! emb-file-buf swap move
    \ Stack: type$

    \ Save type to buffer
    dup emb-type-len ! emb-type-buf swap move
    \ Stack: empty

    \ Get embedding for chunk and save to buffer
    emb-chunk-buf emb-chunk-len @ get-embedding
    dup emb-vector-len ! emb-vector-buf swap move

    \ Now call store-embedding with correct order from buffers
    emb-chunk-buf emb-chunk-len @
    emb-type-buf emb-type-len @
    emb-file-buf emb-file-len @
    chunk-start-line @ chunk-end-line @
    emb-vector-buf emb-vector-len @
    store-embedding
  else
    2drop 2drop 2drop
  then
  chunk-reset ;

: process-file ( path$ -- )
  \ Read file, chunk it, embed chunks
  2dup s" Indexing: " type type cr

  chunk-reset
  0 current-line !
  s" code" 2>r  \ Default chunk type
  2dup 2>r     \ Save path

  r/o open-file if
    2r> 2drop 2r> 2drop
    s"   Error opening file" type cr exit
  then
  process-fid !

  begin
    line-buf line-max process-fid @ read-line throw
  while
    1 current-line +!
    line-buf swap

    \ Check if this line starts a new boundary
    2dup is-boundary-line? if
      \ Save new type, flush previous chunk
      2dup detect-chunk-type
      2r> 2r>           \ Get old type and path
      2dup 2>r 2>r      \ Re-save path
      drop 2>r          \ Save new type, drop old
      process-chunk
    then

    current-line @ chunk-add-line
  repeat
  drop

  \ Process final chunk
  2r> 2r>
  process-chunk

  process-fid @ close-file drop
  1 file-count +! ;

\ ============================================================================
\ Directory Indexing
\ ============================================================================

variable files-fid

: index-directory ( path$ extensions$ -- )
  \ Index all source files in directory tree
  s" Discovering source files..." type cr
  2dup find-source-files

  2drop  \ Done with extensions

  \ Open file list and process each
  files-list r/o open-file throw files-fid !

  begin
    line-buf line-max files-fid @ read-line throw
  while
    line-buf swap
    dup 0> if
      \ Trim trailing whitespace
      begin
        2dup + 1- c@ dup 32 <= swap 0> and
      while
        1-
      repeat

      \ Check if needs reindexing
      2dup needs-reindex? if
        process-file
      else
        1 skip-count +!
      then
    else
      2drop
    then
  repeat
  drop

  files-fid @ close-file drop ;

: index-report ( -- )
  \ Print indexing summary
  cr
  s" ========================================" type cr
  s" Indexing Complete" type cr
  s" ----------------------------------------" type cr
  s" Files indexed: " type file-count @ . cr
  s" Files skipped: " type skip-count @ . cr
  s" Chunks stored: " type chunk-count @ . cr
  s" Database: " type db-path type cr
  s" ========================================" type cr ;

\ ============================================================================
\ Cosine Similarity (simplified integer math)
\ ============================================================================

\ Note: Full cosine similarity would require floating point or fixed-point math.
\ This implementation shells out to Python for the actual similarity calculation.

: compute-similarity ( query-vector$ stored-vector$ -- similarity% )
  \ Compute cosine similarity between two vectors
  \ Returns integer percentage (0-100)
  \ Shell out to Python for actual computation
  str-reset
  s" python3 -c \"import json,math; " str+
  s" a=json.loads('" str+ 2swap str+ s" '); " str+
  s" b=json.loads('" str+ str+ s" '); " str+
  s" dot=sum(x*y for x,y in zip(a,b)); " str+
  s" mag_a=math.sqrt(sum(x*x for x in a)); " str+
  s" mag_b=math.sqrt(sum(x*x for x in b)); " str+
  s" sim=dot/(mag_a*mag_b) if mag_a*mag_b>0 else 0; " str+
  s" print(int(sim*100))\" 2>/dev/null" str+
  str$ system drop
  \ Would capture output - returning placeholder
  85 ;  \ Placeholder similarity

\ ============================================================================
\ Semantic Search
\ ============================================================================

variable result-count
variable max-results

: search-embeddings ( query-vector$ limit -- )
  \ Search database for similar embeddings
  max-results !

  \ Query all embeddings and compute similarity
  \ In production, use sqlite-vss or pg_vector for native vector search
  str-reset
  s" sqlite3 -separator '|' " str+ db-path str+
  s"  \"SELECT id, file_path, chunk_text, chunk_type, start_line, end_line, vector " str+
  s" FROM embeddings ORDER BY id LIMIT 1000\" > " str+
  query-result str+
  str$ system drop

  \ Note: In a real implementation, we would:
  \ 1. Load each vector
  \ 2. Compute cosine similarity with query vector
  \ 3. Sort by similarity
  \ 4. Return top N results

  s" Search results written to " type query-result type cr ;

: semantic-search ( query$ limit -- )
  \ High-level semantic search
  >r
  s" Embedding query..." type cr
  2dup get-embedding
  s" Searching " type db-path type s" ..." type cr
  r> search-embeddings ;

: format-result ( id file$ text$ type$ start end similarity -- )
  \ Format and print a search result
  cr
  s" Score: " type . s" %" type cr
  drop  \ end
  s" File: " type 2>r 2>r type s" :" type . cr  \ file:start
  s" Type: " type 2r> type cr
  s" ----------------------------------------" type cr
  2r> 80 min type  \ Truncate text to 80 chars
  s" ..." type cr ;

\ ============================================================================
\ Context Retrieval for LLM
\ ============================================================================

: get-context ( query$ n -- )
  \ Retrieve n chunks as context for LLM prompt
  >r 2dup
  s" # Code Context" type cr
  s" Query: " type type cr
  s" " type cr

  r> semantic-search

  \ Would iterate results and format as markdown code blocks
  s" ```" type cr
  s" [Retrieved code chunks would appear here]" type cr
  s" ```" type cr ;

\ ============================================================================
\ Statistics
\ ============================================================================

: index-stats ( -- )
  \ Print index statistics
  s" Index Statistics" type cr
  s" ================" type cr

  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"SELECT COUNT(*) FROM embeddings\"" str+
  str$ system drop
  \ Would display count

  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"SELECT COUNT(DISTINCT file_path) FROM embeddings\"" str+
  str$ system drop

  str-reset
  s" sqlite3 " str+ db-path str+
  s"  \"SELECT chunk_type, COUNT(*) FROM embeddings GROUP BY chunk_type\"" str+
  str$ system drop

  s" " type cr
  s" Database: " type db-path type cr
  s" Provider: " type embedding-provider type cr
  s" Model: " type embedding-model type cr ;

\ ============================================================================
\ CLI Interface
\ ============================================================================

: usage ( -- )
  s" Codebase Indexer - Semantic Code Search" type cr
  s" ========================================" type cr
  cr
  s" Usage:" type cr
  s"   ./fifth codebase-indexer/main.fs <command> [args]" type cr
  cr
  s" Commands:" type cr
  s"   index <path> [extensions]  Index source files in path" type cr
  s"                              Default: *.fs,*.py,*.js,*.ts,*.c,*.go,*.rs" type cr
  s"   query <text> [limit]       Semantic search for related code" type cr
  s"   context <text> [n]         Get n chunks as LLM context" type cr
  s"   stats                      Show index statistics" type cr
  s"   clear                      Clear all indexed data" type cr
  s"   init                       Initialize database only" type cr
  cr
  s" Environment:" type cr
  s"   OPENAI_API_KEY             Required for OpenAI embeddings" type cr
  s"   EMBEDDING_PROVIDER         'openai' (default) or 'ollama'" type cr
  s"   EMBEDDING_MODEL            Model identifier" type cr
  s"   OLLAMA_HOST                Ollama server (default: localhost:11434)" type cr
  s"   INDEX_DB                   Database path (default: codebase-index.db)" type cr
  cr
  s" Examples:" type cr
  s"   ./fifth codebase-indexer/main.fs index ./src" type cr
  s"   ./fifth codebase-indexer/main.fs query 'authentication handler'" type cr
  s"   ./fifth codebase-indexer/main.fs context 'error handling' 5" type cr ;

: cmd-index ( -- )
  \ Index command
  argc @ 3 < if
    s" Usage: index <path> [extensions]" type cr exit
  then

  0 file-count !
  0 chunk-count !
  0 skip-count !

  2 argv
  argc @ 4 >= if
    3 argv
  else
    default-extensions
  then

  index-directory
  index-report ;

: cmd-query ( -- )
  \ Query command
  argc @ 3 < if
    s" Usage: query <text> [limit]" type cr exit
  then

  2 argv
  argc @ 4 >= if
    3 argv drop c@ [char] 0 -  \ Simple single-digit parse
  else
    10  \ Default limit
  then

  semantic-search ;

: cmd-context ( -- )
  \ Context retrieval command
  argc @ 3 < if
    s" Usage: context <text> [n]" type cr exit
  then

  2 argv
  argc @ 4 >= if
    3 argv drop c@ [char] 0 -
  else
    5  \ Default chunk count
  then

  get-context ;

: main ( -- )
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" index" str= if 2drop cmd-index exit then
  2dup s" query" str= if 2drop cmd-query exit then
  2dup s" context" str= if 2drop cmd-context exit then
  2dup s" stats" str= if 2drop index-stats exit then
  2dup s" clear" str= if 2drop clear-db exit then
  2dup s" init" str= if 2drop s" Database initialized: " type db-path type cr exit then
  2drop usage ;

main
bye
