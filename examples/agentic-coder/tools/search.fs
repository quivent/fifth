\ fifth/examples/agentic-coder/tools/search.fs
\ Code search tools for agentic coder

\ --- Grep Search ---

: grep-search ( pattern-addr pattern-u path-addr path-u -- )
  \ Search for pattern in files
  str-reset
  s" grep -rn '" str+
  2swap str+
  s" ' " str+
  str+
  s"  2>/dev/null | head -20" str+
  str$ system drop ;

: tool-grep ( pattern-addr pattern-u path-addr path-u -- json-addr json-u )
  \ Grep and return results as JSON
  s" {\"status\": \"success\", \"matches\": []}"
  \ TODO: Parse grep output
  ;

\ --- Ripgrep (faster) ---

: rg-search ( pattern-addr pattern-u path-addr path-u -- )
  str-reset
  s" rg -n '" str+
  2swap str+
  s" ' " str+
  str+
  s"  2>/dev/null | head -20" str+
  str$ system drop ;

\ --- Glob/Find ---

: find-files ( pattern-addr pattern-u path-addr path-u -- )
  \ Find files matching pattern
  str-reset
  s" find " str+
  str+
  s"  -name '" str+
  2swap str+
  s" ' 2>/dev/null | head -20" str+
  str$ system drop ;

: tool-glob ( pattern-addr pattern-u path-addr path-u -- json-addr json-u )
  s" {\"status\": \"success\", \"files\": []}"
  \ TODO: Parse find output
  ;

\ --- AST-based Search (via tree-sitter or ctags) ---

: find-function ( name-addr name-u path-addr path-u -- )
  \ Find function definition
  str-reset
  s" grep -rn 'function " str+
  2swap str+
  s" \\|def " str+
  \ Need to repeat name
  s" \\|: " str+
  s" ' " str+
  str+
  str$ system drop ;

: find-class ( name-addr name-u path-addr path-u -- )
  \ Find class definition
  str-reset
  s" grep -rn 'class " str+
  2swap str+
  s" ' " str+
  str+
  str$ system drop ;

: tool-find-definition ( symbol-addr symbol-u path-addr path-u -- json-addr json-u )
  s" {\"status\": \"success\", \"definitions\": []}"
  \ TODO: Parse and structure results
  ;

\ --- Ctags Integration ---

: generate-tags ( path-addr path-u -- )
  \ Generate ctags for codebase
  str-reset
  s" ctags -R " str+
  str+
  str$ system drop ;

: lookup-tag ( symbol-addr symbol-u -- )
  \ Look up symbol in tags file
  str-reset
  s" grep '^" str+
  str+
  s" ' tags | head -5" str+
  str$ system drop ;

\ --- Semantic Search (placeholder) ---

: semantic-search ( query-addr query-u path-addr path-u -- )
  \ Would use embeddings for semantic similarity
  \ Requires: embed all files, store vectors, query with cosine similarity
  s" [Semantic search not implemented - would need embedding DB]" type cr
  2drop 2drop ;

\ --- Multi-Search ---

: search-codebase ( query-addr query-u -- )
  \ Combined search: grep + find + definitions
  s" ==> Searching codebase for: " type 2dup type cr
  cr

  s" --- Text matches ---" type cr
  2dup s" ." grep-search
  cr

  s" --- Files matching ---" type cr
  2dup s" *" 2swap s" ." find-files
  cr

  s" --- Possible definitions ---" type cr
  s" ." find-function

  2drop ;

\ --- Tool Interface ---

: tool-search-dispatch ( type-addr type-u query-addr query-u path-addr path-u -- json-addr json-u )
  \ Dispatch to appropriate search type
  2>r 2>r  \ save query and path
  2dup s" grep" compare 0= if 2drop 2r> 2r> tool-grep exit then
  2dup s" glob" compare 0= if 2drop 2r> 2r> tool-glob exit then
  2dup s" definition" compare 0= if 2drop 2r> 2r> tool-find-definition exit then
  2drop 2r> 2r>
  2drop 2drop
  s" {\"status\": \"error\", \"error\": \"Unknown search type\"}" ;
