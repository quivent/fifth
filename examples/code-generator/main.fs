\ fifth/examples/code-generator/main.fs
\ Code generator from schema definitions

require ~/.fifth/lib/core.fs

\ Configuration
: output-dir ( -- addr u ) s" output/" ;

variable gen-fid

: gen>file ( filename-addr filename-u -- )
  w/o create-file throw gen-fid ! ;

: gen-emit ( addr u -- )
  gen-fid @ write-file throw ;

: gen-nl ( -- )
  s\" \n" gen-emit ;

: gen-line ( addr u -- )
  gen-emit gen-nl ;

: gen-close ( -- )
  gen-fid @ close-file throw ;

\ --- Schema Parsing (via jq) ---

: jq-query ( json-addr json-u query-addr query-u -- result-addr result-u )
  \ Run jq query on JSON string
  \ TODO: Implement with temp files and shell
  2drop 2drop s" []" ;

: extract-entities ( json-addr json-u -- )
  \ Extract entity definitions from schema
  s" .entities[]" jq-query
  2drop ;  \ TODO: Parse result

\ --- Model Generation ---

: gen-model-header ( name-addr name-u -- )
  s" // Generated model: " gen-emit
  gen-line
  s" // Do not edit manually" gen-line
  gen-nl ;

: gen-field ( name-addr name-u type-addr type-u -- )
  s"   " gen-emit
  2swap gen-emit  \ field name
  s" : " gen-emit
  gen-emit        \ type
  s" ;" gen-line ;

: gen-model ( name-addr name-u -- )
  str-reset
  output-dir str+
  2dup str+
  s" .ts" str+
  str$ gen>file

  2dup gen-model-header

  s" export interface " gen-emit
  gen-emit
  s"  {" gen-line

  \ TODO: Generate fields from schema
  s" id" s" number" gen-field
  s" createdAt" s" Date" gen-field
  s" updatedAt" s" Date" gen-field

  s" }" gen-line

  gen-close ;

\ --- Route Generation ---

: gen-route ( name-addr name-u -- )
  str-reset
  output-dir str+
  2dup str+
  s" Route.ts" str+
  str$ gen>file

  s" // Generated route for " gen-emit 2dup gen-line
  s" import { Router } from 'express';" gen-line
  gen-nl

  s" const router = Router();" gen-line
  gen-nl

  s" // GET /" gen-emit 2dup gen-line
  s" router.get('/', async (req, res) => {" gen-line
  s"   // TODO: List " gen-emit gen-line
  s"   res.json([]);" gen-line
  s" });" gen-line
  gen-nl

  s" // GET /:id" gen-line
  s" router.get('/:id', async (req, res) => {" gen-line
  s"   // TODO: Get by id" gen-line
  s"   res.json({});" gen-line
  s" });" gen-line
  gen-nl

  s" // POST /" gen-line
  s" router.post('/', async (req, res) => {" gen-line
  s"   // TODO: Create" gen-line
  s"   res.status(201).json({});" gen-line
  s" });" gen-line
  gen-nl

  s" export default router;" gen-line

  gen-close ;

\ --- Test Generation ---

: gen-test ( name-addr name-u -- )
  str-reset
  output-dir str+
  2dup str+
  s" .test.ts" str+
  str$ gen>file

  s" // Generated tests for " gen-emit 2dup gen-line
  s" import { describe, it, expect } from 'vitest';" gen-line
  gen-nl

  s" describe('" gen-emit 2dup gen-emit s" ', () => {" gen-line
  s"   it('should create', () => {" gen-line
  s"     // TODO: Implement test" gen-line
  s"     expect(true).toBe(true);" gen-line
  s"   });" gen-line
  gen-nl
  s"   it('should read', () => {" gen-line
  s"     // TODO: Implement test" gen-line
  s"     expect(true).toBe(true);" gen-line
  s"   });" gen-line
  s" });" gen-line

  gen-close ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: generate-all ( name-addr name-u -- )
  s" Generating code for: " type 2dup type cr
  2dup gen-model
  s"   Created model" type cr
  2dup gen-route
  s"   Created route" type cr
  2dup gen-test
  s"   Created tests" type cr
  2drop
  s" Done!" type cr ;

: usage ( -- )
  s" Code Generator" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth code-generator/main.fs <entity-name>" type cr
  s" " type cr
  s" Example:" type cr
  s"   ./fifth code-generator/main.fs User" type cr ;

: main ( -- )
  ensure-output
  argc @ 2 < if
    usage exit
  then
  1 argv generate-all ;

main
bye
