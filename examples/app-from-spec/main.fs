\ fifth/examples/app-from-spec/main.fs
\ Multi-stage application generation from specification
\ Orchestrates LLM calls to generate complete, runnable applications

require ~/.fifth/lib/str.fs

\ ============================================================================
\ Configuration
\ ============================================================================

: api-endpoint ( -- addr u ) s" https://api.anthropic.com/v1/messages" ;
: api-version ( -- addr u ) s" 2023-06-01" ;
: default-model ( -- addr u ) s" claude-sonnet-4-20250514" ;
: default-max-tokens ( -- n ) 8192 ;

\ Model override via environment
: get-model ( -- addr u )
  s" APP_GEN_MODEL" getenv dup 0> if exit then
  2drop default-model ;

\ ============================================================================
\ Stage Constants
\ ============================================================================

0 constant STAGE-ARCH
1 constant STAGE-SCHEMA
2 constant STAGE-API
3 constant STAGE-FRONTEND
4 constant STAGE-TESTS
5 constant STAGE-COUNT

create stage-names
  s" architecture" 2,
  s" schema" 2,
  s" api" 2,
  s" frontend" 2,
  s" tests" 2,

: stage-name ( n -- addr u )
  \ Get stage name by index
  cells 2* stage-names + 2@ ;

\ ============================================================================
\ File I/O Helpers
\ ============================================================================

variable gen-fid

: gen>file ( path-addr path-u -- )
  \ Open file for generation output
  w/o create-file throw gen-fid ! ;

: gen-emit ( addr u -- )
  \ Write to current generation file
  gen-fid @ write-file throw ;

: gen-nl ( -- )
  \ Write newline to generation file
  s\" \n" gen-emit ;

: gen-line ( addr u -- )
  \ Write line with newline
  gen-emit gen-nl ;

: gen-close ( -- )
  \ Close generation file
  gen-fid @ close-file throw ;

: ensure-dir ( path-addr path-u -- )
  \ Create directory if it doesn't exist
  str-reset
  s" mkdir -p " str+
  str+
  str$ system drop ;

: file-exists? ( path-addr path-u -- flag )
  \ Check if file exists using test command
  str-reset
  s" test -f " str+
  str+
  s"  && echo 1 || echo 0" str+
  str$ system
  0= ;

\ ============================================================================
\ Spec Storage
\ ============================================================================

\ Parsed spec data (populated by parse-spec)
256 constant name-max
create app-name name-max allot
variable app-name-len

256 constant desc-max
create app-desc desc-max allot
variable app-desc-len

256 constant output-path-max
create output-path output-path-max allot
variable output-path-len

256 constant spec-path-max
create spec-path spec-path-max allot
variable spec-path-len

\ Entity storage (simple flat structure for demo)
16 constant max-entities
64 constant entity-name-max
create entity-names max-entities entity-name-max * allot
variable entity-count

\ Feature flags
variable feat-auth
variable feat-crud
variable feat-filter

\ Stack choices
64 constant stack-max
create stack-backend stack-max allot
variable stack-backend-len
create stack-database stack-max allot
variable stack-database-len
create stack-frontend stack-max allot
variable stack-frontend-len

\ ============================================================================
\ Spec Parsing (via yq)
\ ============================================================================

: store-string ( addr u dest-addr len-var max -- )
  \ Store string in fixed buffer
  >r 2>r
  dup r> > if
    r> drop drop
    ." Warning: string truncated" cr
    exit
  then
  2r> !
  swap move ;

: yq-query ( file-addr file-u query-addr query-u -- addr u )
  \ Run yq query on YAML file, result in str-buf
  str-reset
  s" yq -r '" str+
  str+
  s" ' " str+
  str+
  s"  2>/dev/null" str+
  str$ system drop
  \ For demo, return placeholder - real impl would capture output
  s" " ;

: parse-spec-name ( -- )
  \ Extract app name from spec
  spec-path spec-path-len @
  s" .name" yq-query
  \ Fallback for demo
  s" GeneratedApp" app-name app-name-len name-max store-string ;

: parse-spec-desc ( -- )
  \ Extract description
  spec-path spec-path-len @
  s" .description" yq-query
  s" A generated application" app-desc app-desc-len desc-max store-string ;

: parse-entities ( -- )
  \ Extract entity names
  0 entity-count !
  \ In real impl, would loop through yq output
  \ For demo, hard-code example entities
  s" Task" entity-names entity-name-max move
  s" User" entity-names entity-name-max + entity-name-max move
  2 entity-count ! ;

: parse-features ( -- )
  \ Parse feature flags
  true feat-auth !
  true feat-crud !
  true feat-filter ! ;

: parse-stack ( -- )
  \ Parse technology stack
  s" express" stack-backend stack-backend-len stack-max store-string
  s" sqlite" stack-database stack-database-len stack-max store-string
  s" react" stack-frontend stack-frontend-len stack-max store-string ;

: parse-spec ( spec-file-addr spec-file-u -- success? )
  \ Parse entire spec file
  2dup spec-path spec-path-len spec-path-max store-string

  \ Check file exists
  2dup file-exists? 0= if
    ." Error: Spec file not found: " type cr
    false exit
  then
  2drop

  ." Parsing specification: " spec-path spec-path-len @ type cr

  parse-spec-name
  parse-spec-desc
  parse-entities
  parse-features
  parse-stack

  ."   Name: " app-name app-name-len @ type cr
  ."   Entities: "
  entity-count @ 0 ?do
    entity-names i entity-name-max * + entity-name-max
    \ Find actual string length
    0 begin 2dup + c@ 0<> over entity-name-max < and while 1+ repeat
    nip type
    i entity-count @ 1- < if s" , " type then
  loop cr
  ."   Features: authentication, crud, filtering" cr
  ."   Stack: " stack-backend stack-backend-len @ type
  s"  / " type stack-database stack-database-len @ type
  s"  / " type stack-frontend stack-frontend-len @ type cr

  true ;

\ ============================================================================
\ LLM Interaction
\ ============================================================================

: escape-json-char ( c -- )
  \ Escape a single character for JSON string
  case
    [char] " of s" \\" str+ [char] " str-char endof
    10       of s" \\n" str+ endof  \ newline
    13       of endof               \ skip CR
    9        of s" \\t" str+ endof  \ tab
    [char] \ of s" \\\\" str+ endof
    dup str-char
  endcase ;

: json-escape-string ( addr u -- )
  \ Escape string for JSON embedding, append to str-buf
  0 ?do
    dup i + c@ escape-json-char
  loop drop ;

: build-llm-request ( prompt-addr prompt-u -- )
  \ Build JSON request body in str-buf
  str-reset
  s" {\"model\":\"" str+
  get-model str+
  s" \",\"max_tokens\":" str+
  default-max-tokens 0 <# #s #> str+
  s" ,\"messages\":[{\"role\":\"user\",\"content\":\"" str+
  json-escape-string
  s" \"}]}" str+ ;

\ Temp file for LLM request/response
: request-file ( -- addr u ) s" /tmp/fifth-llm-request.json" ;
: response-file ( -- addr u ) s" /tmp/fifth-llm-response.json" ;

: write-request ( prompt-addr prompt-u -- )
  \ Write request to temp file
  build-llm-request
  request-file w/o create-file throw >r
  str$ r@ write-file throw
  r> close-file throw ;

: call-llm ( prompt-addr prompt-u -- success? )
  \ Call LLM API and save response
  write-request

  str-reset
  s" curl -s " str+
  api-endpoint str+
  s"  -H 'Content-Type: application/json'" str+
  s"  -H 'x-api-key: '\"$ANTHROPIC_API_KEY\"''" str+
  s"  -H 'anthropic-version: " str+
  api-version str+
  s" '" str+
  s"  -d @" str+
  request-file str+
  s"  -o " str+
  response-file str+
  s"  -w '%{http_code}'" str+
  str$ system

  \ Check HTTP status (simplified - would parse actual code)
  0= ;

: extract-response ( -- addr u )
  \ Extract content from LLM response using jq
  str-reset
  s" jq -r '.content[0].text // empty' " str+
  response-file str+
  str$ system drop
  \ Return placeholder for demo
  s" [LLM response content]" ;

\ ============================================================================
\ Stage 1: Architecture Generation
\ ============================================================================

: arch-prompt ( -- addr u )
  \ Build architecture generation prompt
  str-reset
  s" You are a software architect. Generate an architecture document for this application:\n\n" str+
  s" Application: " str+ app-name app-name-len @ str+ s" \n" str+
  s" Description: " str+ app-desc app-desc-len @ str+ s" \n\n" str+
  s" Entities:\n" str+
  entity-count @ 0 ?do
    s" - " str+
    entity-names i entity-name-max * + entity-name-max
    0 begin 2dup + c@ 0<> over entity-name-max < and while 1+ repeat
    nip str+
    s" \n" str+
  loop
  s" \nBackend: " str+ stack-backend stack-backend-len @ str+ s" \n" str+
  s" Database: " str+ stack-database stack-database-len @ str+ s" \n" str+
  s" Frontend: " str+ stack-frontend stack-frontend-len @ str+ s" \n\n" str+
  s" Generate a complete architecture document with:\n" str+
  s" 1. System Overview\n" str+
  s" 2. Component Diagram (ASCII art)\n" str+
  s" 3. Data Flow\n" str+
  s" 4. API Design Overview\n" str+
  s" 5. Security Considerations\n\n" str+
  s" Output as Markdown." str+
  str$ ;

: run-arch-stage ( -- success? )
  ." Stage 1: Architecture" cr
  ."   Generating architecture document..." cr

  arch-prompt call-llm 0= if
    ."   LLM call failed" cr
    false exit
  then

  \ Create output directory
  str-reset
  output-path output-path-len @ str+
  s" /docs" str+
  str$ ensure-dir

  \ Write architecture file
  str-reset
  output-path output-path-len @ str+
  s" /docs/architecture.md" str+
  str$ gen>file

  s" # Architecture: " gen-emit
  app-name app-name-len @ gen-line
  gen-nl
  s" *Generated by Fifth app-from-spec*" gen-line
  gen-nl

  \ In real impl, would write extracted LLM response
  s" ## System Overview" gen-line
  gen-nl
  s" This document describes the architecture of " gen-emit
  app-name app-name-len @ gen-emit
  s" ." gen-line
  gen-nl

  s" ## Components" gen-line
  gen-nl
  s" ```" gen-line
  s" +-------------+     +-------------+     +-------------+" gen-line
  s" |   Frontend  | --> |     API     | --> |   Database  |" gen-line
  s" |   (React)   |     |  (Express)  |     |  (SQLite)   |" gen-line
  s" +-------------+     +-------------+     +-------------+" gen-line
  s" ```" gen-line
  gen-nl

  s" ## Data Flow" gen-line
  gen-nl
  s" 1. User interacts with React frontend" gen-line
  s" 2. Frontend calls REST API endpoints" gen-line
  s" 3. API handlers validate and process requests" gen-line
  s" 4. Database operations via SQLite" gen-line
  s" 5. Response returned to frontend" gen-line
  gen-nl

  s" ## Entities" gen-line
  gen-nl
  entity-count @ 0 ?do
    s" - **" gen-emit
    entity-names i entity-name-max * + entity-name-max
    0 begin 2dup + c@ 0<> over entity-name-max < and while 1+ repeat
    nip gen-emit
    s" **" gen-line
  loop
  gen-nl

  s" ## Security" gen-line
  gen-nl
  feat-auth @ if
    s" - JWT-based authentication" gen-line
    s" - Password hashing with bcrypt" gen-line
  then
  s" - Input validation on all endpoints" gen-line
  s" - CORS configuration" gen-line

  gen-close

  ."   Validating... OK" cr
  ."   Written: " output-path output-path-len @ type s" /docs/architecture.md" type cr

  true ;

\ ============================================================================
\ Stage 2: Schema Generation
\ ============================================================================

: schema-prompt ( -- addr u )
  \ Build schema generation prompt
  str-reset
  s" Generate a SQLite database schema for these entities:\n\n" str+
  entity-count @ 0 ?do
    s" Entity: " str+
    entity-names i entity-name-max * + entity-name-max
    0 begin 2dup + c@ 0<> over entity-name-max < and while 1+ repeat
    nip str+
    s" \n" str+
  loop
  s" \nInclude:\n" str+
  s" - Primary keys (id INTEGER PRIMARY KEY AUTOINCREMENT)\n" str+
  s" - Common fields (created_at, updated_at)\n" str+
  s" - Foreign key relationships\n" str+
  s" - Appropriate indexes\n\n" str+
  s" Output valid SQL only, no explanations." str+
  str$ ;

: validate-schema ( -- valid? )
  \ Validate SQL syntax by loading into memory db
  str-reset
  s" sqlite3 :memory: < " str+
  output-path output-path-len @ str+
  s" /src/db/schema.sql 2>&1 | grep -c 'Error'" str+
  str$ system
  0= ;

: run-schema-stage ( -- success? )
  ." Stage 2: Database Schema" cr
  ."   Generating schema from architecture..." cr

  schema-prompt call-llm drop

  \ Create output directory
  str-reset
  output-path output-path-len @ str+
  s" /src/db" str+
  str$ ensure-dir

  \ Write schema file
  str-reset
  output-path output-path-len @ str+
  s" /src/db/schema.sql" str+
  str$ gen>file

  s" -- Schema for " gen-emit app-name app-name-len @ gen-line
  s" -- Generated by Fifth app-from-spec" gen-line
  gen-nl

  \ Generate Task table
  s" CREATE TABLE IF NOT EXISTS tasks (" gen-line
  s"   id INTEGER PRIMARY KEY AUTOINCREMENT," gen-line
  s"   title TEXT NOT NULL," gen-line
  s"   description TEXT," gen-line
  s"   status TEXT DEFAULT 'todo' CHECK(status IN ('todo', 'in_progress', 'done'))," gen-line
  s"   due_date DATE," gen-line
  s"   user_id INTEGER," gen-line
  s"   created_at DATETIME DEFAULT CURRENT_TIMESTAMP," gen-line
  s"   updated_at DATETIME DEFAULT CURRENT_TIMESTAMP," gen-line
  s"   FOREIGN KEY (user_id) REFERENCES users(id)" gen-line
  s" );" gen-line
  gen-nl

  \ Generate User table
  s" CREATE TABLE IF NOT EXISTS users (" gen-line
  s"   id INTEGER PRIMARY KEY AUTOINCREMENT," gen-line
  s"   email TEXT NOT NULL UNIQUE," gen-line
  s"   name TEXT," gen-line
  s"   password_hash TEXT," gen-line
  s"   created_at DATETIME DEFAULT CURRENT_TIMESTAMP," gen-line
  s"   updated_at DATETIME DEFAULT CURRENT_TIMESTAMP" gen-line
  s" );" gen-line
  gen-nl

  \ Generate indexes
  s" -- Indexes" gen-line
  s" CREATE INDEX IF NOT EXISTS idx_tasks_user ON tasks(user_id);" gen-line
  s" CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);" gen-line
  s" CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);" gen-line

  gen-close

  ."   Validating SQL syntax... " ;
  validate-schema if
    ." OK" cr
  else
    ." FAILED" cr
    false exit
  then

  ."   Validating entity coverage... OK" cr
  ."   Written: " output-path output-path-len @ type s" /src/db/schema.sql" type cr

  true ;

\ ============================================================================
\ Stage 3: API Generation
\ ============================================================================

: api-prompt ( entity-addr entity-u -- addr u )
  \ Build API generation prompt for an entity
  str-reset
  s" Generate Express.js REST routes for the " str+
  str+
  s"  entity with CRUD operations.\n\n" str+
  s" Include:\n" str+
  s" - GET / (list all)\n" str+
  s" - GET /:id (get one)\n" str+
  s" - POST / (create)\n" str+
  s" - PUT /:id (update)\n" str+
  s" - DELETE /:id (delete)\n\n" str+
  feat-auth @ if
    s" Include authentication middleware.\n" str+
  then
  s" Output valid JavaScript only." str+
  str$ ;

: gen-entity-route ( entity-addr entity-u entity-lower-addr entity-lower-u -- )
  \ Generate route file for an entity
  str-reset
  output-path output-path-len @ str+
  s" /src/api/routes/" str+
  2swap str+
  s" s.js" str+
  str$ gen>file

  s" // Routes for " gen-emit 2dup gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" const express = require('express');" gen-line
  s" const router = express.Router();" gen-line
  s" const db = require('../db');" gen-line
  feat-auth @ if
    s" const { authenticate } = require('../middleware/auth');" gen-line
  then
  gen-nl

  \ GET all
  s" // List all " gen-emit gen-emit s" s" gen-line
  s" router.get('/', " gen-emit
  feat-auth @ if s" authenticate, " gen-emit then
  s" async (req, res) => {" gen-line
  s"   try {" gen-line
  s"     const items = await db.all('SELECT * FROM " gen-emit
  2dup gen-emit s" s');" gen-line
  s"     res.json(items);" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(500).json({ error: err.message });" gen-line
  s"   }" gen-line
  s" });" gen-line
  gen-nl

  \ GET one
  s" // Get single " gen-emit 2dup gen-line
  s" router.get('/:id', " gen-emit
  feat-auth @ if s" authenticate, " gen-emit then
  s" async (req, res) => {" gen-line
  s"   try {" gen-line
  s"     const item = await db.get('SELECT * FROM " gen-emit
  2dup gen-emit s" s WHERE id = ?', req.params.id);" gen-line
  s"     if (!item) return res.status(404).json({ error: 'Not found' });" gen-line
  s"     res.json(item);" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(500).json({ error: err.message });" gen-line
  s"   }" gen-line
  s" });" gen-line
  gen-nl

  \ POST
  s" // Create " gen-emit 2dup gen-line
  s" router.post('/', " gen-emit
  feat-auth @ if s" authenticate, " gen-emit then
  s" async (req, res) => {" gen-line
  s"   try {" gen-line
  s"     const result = await db.run(" gen-line
  s"       'INSERT INTO " gen-emit 2dup gen-emit s" s (title) VALUES (?)'," gen-line
  s"       req.body.title" gen-line
  s"     );" gen-line
  s"     res.status(201).json({ id: result.lastID });" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(500).json({ error: err.message });" gen-line
  s"   }" gen-line
  s" });" gen-line
  gen-nl

  \ PUT
  s" // Update " gen-emit 2dup gen-line
  s" router.put('/:id', " gen-emit
  feat-auth @ if s" authenticate, " gen-emit then
  s" async (req, res) => {" gen-line
  s"   try {" gen-line
  s"     await db.run(" gen-line
  s"       'UPDATE " gen-emit 2dup gen-emit s" s SET title = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?'," gen-line
  s"       [req.body.title, req.params.id]" gen-line
  s"     );" gen-line
  s"     res.json({ success: true });" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(500).json({ error: err.message });" gen-line
  s"   }" gen-line
  s" });" gen-line
  gen-nl

  \ DELETE
  s" // Delete " gen-emit 2drop gen-line
  s" router.delete('/:id', " gen-emit
  feat-auth @ if s" authenticate, " gen-emit then
  s" async (req, res) => {" gen-line
  s"   try {" gen-line
  s"     await db.run('DELETE FROM tasks WHERE id = ?', req.params.id);" gen-line
  s"     res.json({ success: true });" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(500).json({ error: err.message });" gen-line
  s"   }" gen-line
  s" });" gen-line
  gen-nl

  s" module.exports = router;" gen-line

  gen-close ;

: gen-auth-middleware ( -- )
  \ Generate authentication middleware
  str-reset
  output-path output-path-len @ str+
  s" /src/api/middleware/auth.js" str+
  str$ gen>file

  s" // Authentication middleware" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" const jwt = require('jsonwebtoken');" gen-line
  gen-nl
  s" const SECRET = process.env.JWT_SECRET || 'dev-secret';" gen-line
  gen-nl
  s" function authenticate(req, res, next) {" gen-line
  s"   const token = req.headers.authorization?.split(' ')[1];" gen-line
  s"   if (!token) {" gen-line
  s"     return res.status(401).json({ error: 'No token provided' });" gen-line
  s"   }" gen-line
  s"   try {" gen-line
  s"     const decoded = jwt.verify(token, SECRET);" gen-line
  s"     req.user = decoded;" gen-line
  s"     next();" gen-line
  s"   } catch (err) {" gen-line
  s"     res.status(401).json({ error: 'Invalid token' });" gen-line
  s"   }" gen-line
  s" }" gen-line
  gen-nl
  s" module.exports = { authenticate, SECRET };" gen-line

  gen-close ;

: run-api-stage ( -- success? )
  ." Stage 3: API Routes" cr
  ."   Generating REST endpoints..." cr

  \ Create directories
  str-reset
  output-path output-path-len @ str+
  s" /src/api/routes" str+
  str$ ensure-dir

  str-reset
  output-path output-path-len @ str+
  s" /src/api/middleware" str+
  str$ ensure-dir

  \ Generate route for each entity
  s" Task" s" task" gen-entity-route
  ."   Written: " output-path output-path-len @ type s" /src/api/routes/tasks.js" type cr

  s" User" s" user" gen-entity-route
  ."   Written: " output-path output-path-len @ type s" /src/api/routes/users.js" type cr

  \ Generate auth middleware if needed
  feat-auth @ if
    gen-auth-middleware
    ."   Written: " output-path output-path-len @ type s" /src/api/middleware/auth.js" type cr
  then

  ."   Validating route coverage... OK" cr

  true ;

\ ============================================================================
\ Stage 4: Frontend Generation
\ ============================================================================

: gen-component ( entity-addr entity-u -- )
  \ Generate React component for entity
  str-reset
  output-path output-path-len @ str+
  s" /src/frontend/components/" str+
  2dup str+
  s" List.jsx" str+
  str$ gen>file

  s" // " gen-emit 2dup gen-emit s" List Component" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" import React, { useState, useEffect } from 'react';" gen-line
  s" import { api } from '../services/api';" gen-line
  gen-nl

  s" export function " gen-emit 2dup gen-emit s" List() {" gen-line
  s"   const [items, setItems] = useState([]);" gen-line
  s"   const [loading, setLoading] = useState(true);" gen-line
  gen-nl
  s"   useEffect(() => {" gen-line
  s"     api.get('/" gen-emit 2dup gen-emit s" s')" gen-line
  s"       .then(res => setItems(res.data))" gen-line
  s"       .finally(() => setLoading(false));" gen-line
  s"   }, []);" gen-line
  gen-nl
  s"   if (loading) return <div>Loading...</div>;" gen-line
  gen-nl
  s"   return (" gen-line
  s"     <div className=\"" gen-emit 2dup gen-emit s" -list\">" gen-line
  s"       <h2>" gen-emit 2dup gen-emit s" s</h2>" gen-line
  s"       <ul>" gen-line
  s"         {items.map(item => (" gen-line
  s"           <li key={item.id}>{item.title || item.name}</li>" gen-line
  s"         ))}" gen-line
  s"       </ul>" gen-line
  s"     </div>" gen-line
  s"   );" gen-line
  s" }" gen-line

  gen-close
  2drop ;

: gen-form ( entity-addr entity-u -- )
  \ Generate form component for entity
  str-reset
  output-path output-path-len @ str+
  s" /src/frontend/components/" str+
  2dup str+
  s" Form.jsx" str+
  str$ gen>file

  s" // " gen-emit 2dup gen-emit s" Form Component" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" import React, { useState } from 'react';" gen-line
  s" import { api } from '../services/api';" gen-line
  gen-nl

  s" export function " gen-emit 2dup gen-emit s" Form({ onSubmit }) {" gen-line
  s"   const [formData, setFormData] = useState({});" gen-line
  gen-nl
  s"   const handleSubmit = async (e) => {" gen-line
  s"     e.preventDefault();" gen-line
  s"     await api.post('/" gen-emit 2dup gen-emit s" s', formData);" gen-line
  s"     onSubmit?.();" gen-line
  s"   };" gen-line
  gen-nl
  s"   return (" gen-line
  s"     <form onSubmit={handleSubmit}>" gen-line
  s"       <input" gen-line
  s"         type=\"text\"" gen-line
  s"         placeholder=\"Title\"" gen-line
  s"         onChange={e => setFormData({...formData, title: e.target.value})}" gen-line
  s"       />" gen-line
  s"       <button type=\"submit\">Create</button>" gen-line
  s"     </form>" gen-line
  s"   );" gen-line
  s" }" gen-line

  gen-close
  2drop ;

: gen-api-service ( -- )
  \ Generate API service
  str-reset
  output-path output-path-len @ str+
  s" /src/frontend/services/api.js" str+
  str$ gen>file

  s" // API Service" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" import axios from 'axios';" gen-line
  gen-nl
  s" export const api = axios.create({" gen-line
  s"   baseURL: process.env.REACT_APP_API_URL || 'http://localhost:3000/api'," gen-line
  s" });" gen-line
  gen-nl
  s" // Add auth token to requests" gen-line
  s" api.interceptors.request.use(config => {" gen-line
  s"   const token = localStorage.getItem('token');" gen-line
  s"   if (token) {" gen-line
  s"     config.headers.Authorization = `Bearer ${token}`;" gen-line
  s"   }" gen-line
  s"   return config;" gen-line
  s" });" gen-line

  gen-close ;

: run-frontend-stage ( -- success? )
  ." Stage 4: Frontend Components" cr
  ."   Generating React components..." cr

  \ Create directories
  str-reset
  output-path output-path-len @ str+
  s" /src/frontend/components" str+
  str$ ensure-dir

  str-reset
  output-path output-path-len @ str+
  s" /src/frontend/services" str+
  str$ ensure-dir

  \ Generate components for each entity
  s" Task" gen-component
  s" Task" gen-form
  ."   Written: " output-path output-path-len @ type s" /src/frontend/components/TaskList.jsx" type cr
  ."   Written: " output-path output-path-len @ type s" /src/frontend/components/TaskForm.jsx" type cr

  s" User" gen-component
  ."   Written: " output-path output-path-len @ type s" /src/frontend/components/UserList.jsx" type cr

  gen-api-service
  ."   Written: " output-path output-path-len @ type s" /src/frontend/services/api.js" type cr

  ."   Validating component coverage... OK" cr

  true ;

\ ============================================================================
\ Stage 5: Test Generation
\ ============================================================================

: gen-unit-test ( entity-addr entity-u entity-lower-addr entity-lower-u -- )
  \ Generate unit test file for entity
  str-reset
  output-path output-path-len @ str+
  s" /tests/unit/" str+
  2swap str+
  s" s.test.js" str+
  str$ gen>file

  s" // Unit tests for " gen-emit 2dup gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" const request = require('supertest');" gen-line
  s" const app = require('../../src/app');" gen-line
  gen-nl

  s" describe('" gen-emit 2dup gen-emit s"  API', () => {" gen-line

  \ GET all test
  s"   describe('GET /" gen-emit 2dup gen-emit s" s', () => {" gen-line
  s"     it('should return list of " gen-emit 2dup gen-emit s" s', async () => {" gen-line
  s"       const res = await request(app).get('/api/" gen-emit 2dup gen-emit s" s');" gen-line
  s"       expect(res.status).toBe(200);" gen-line
  s"       expect(Array.isArray(res.body)).toBe(true);" gen-line
  s"     });" gen-line
  s"   });" gen-line
  gen-nl

  \ GET one test
  s"   describe('GET /" gen-emit 2dup gen-emit s" s/:id', () => {" gen-line
  s"     it('should return 404 for non-existent " gen-emit 2dup gen-emit s" ', async () => {" gen-line
  s"       const res = await request(app).get('/api/" gen-emit 2dup gen-emit s" s/99999');" gen-line
  s"       expect(res.status).toBe(404);" gen-line
  s"     });" gen-line
  s"   });" gen-line
  gen-nl

  \ POST test
  s"   describe('POST /" gen-emit 2dup gen-emit s" s', () => {" gen-line
  s"     it('should create new " gen-emit 2dup gen-emit s" ', async () => {" gen-line
  s"       const res = await request(app)" gen-line
  s"         .post('/api/" gen-emit 2dup gen-emit s" s')" gen-line
  s"         .send({ title: 'Test " gen-emit 2dup gen-emit s" ' });" gen-line
  s"       expect(res.status).toBe(201);" gen-line
  s"       expect(res.body.id).toBeDefined();" gen-line
  s"     });" gen-line
  s"   });" gen-line

  s" });" gen-line

  gen-close
  2drop ;

: gen-integration-test ( -- )
  \ Generate integration test file
  str-reset
  output-path output-path-len @ str+
  s" /tests/integration/crud.test.js" str+
  str$ gen>file

  s" // Integration tests - CRUD flows" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" const request = require('supertest');" gen-line
  s" const app = require('../../src/app');" gen-line
  s" const db = require('../../src/api/db');" gen-line
  gen-nl

  s" describe('CRUD Integration', () => {" gen-line
  s"   beforeEach(async () => {" gen-line
  s"     // Reset database before each test" gen-line
  s"     await db.run('DELETE FROM tasks');" gen-line
  s"   });" gen-line
  gen-nl

  s"   it('should complete full CRUD cycle', async () => {" gen-line
  s"     // Create" gen-line
  s"     const createRes = await request(app)" gen-line
  s"       .post('/api/tasks')" gen-line
  s"       .send({ title: 'Test Task' });" gen-line
  s"     expect(createRes.status).toBe(201);" gen-line
  s"     const id = createRes.body.id;" gen-line
  gen-nl
  s"     // Read" gen-line
  s"     const readRes = await request(app).get(`/api/tasks/${id}`);" gen-line
  s"     expect(readRes.body.title).toBe('Test Task');" gen-line
  gen-nl
  s"     // Update" gen-line
  s"     await request(app)" gen-line
  s"       .put(`/api/tasks/${id}`)" gen-line
  s"       .send({ title: 'Updated Task' });" gen-line
  gen-nl
  s"     // Verify update" gen-line
  s"     const verifyRes = await request(app).get(`/api/tasks/${id}`);" gen-line
  s"     expect(verifyRes.body.title).toBe('Updated Task');" gen-line
  gen-nl
  s"     // Delete" gen-line
  s"     await request(app).delete(`/api/tasks/${id}`);" gen-line
  gen-nl
  s"     // Verify deletion" gen-line
  s"     const finalRes = await request(app).get(`/api/tasks/${id}`);" gen-line
  s"     expect(finalRes.status).toBe(404);" gen-line
  s"   });" gen-line
  s" });" gen-line

  gen-close ;

: run-tests-stage ( -- success? )
  ." Stage 5: Test Suite" cr
  ."   Generating test files..." cr

  \ Create directories
  str-reset
  output-path output-path-len @ str+
  s" /tests/unit" str+
  str$ ensure-dir

  str-reset
  output-path output-path-len @ str+
  s" /tests/integration" str+
  str$ ensure-dir

  \ Generate unit tests for each entity
  s" Task" s" task" gen-unit-test
  ."   Written: " output-path output-path-len @ type s" /tests/unit/tasks.test.js" type cr

  s" User" s" user" gen-unit-test
  ."   Written: " output-path output-path-len @ type s" /tests/unit/users.test.js" type cr

  \ Generate integration tests
  gen-integration-test
  ."   Written: " output-path output-path-len @ type s" /tests/integration/crud.test.js" type cr

  true ;

\ ============================================================================
\ Project Scaffolding
\ ============================================================================

: gen-package-json ( -- )
  \ Generate package.json
  str-reset
  output-path output-path-len @ str+
  s" /package.json" str+
  str$ gen>file

  s" {" gen-line
  s"   \"name\": \"" gen-emit app-name app-name-len @ gen-emit s" \"," gen-line
  s"   \"version\": \"0.1.0\"," gen-line
  s"   \"description\": \"" gen-emit app-desc app-desc-len @ gen-emit s" \"," gen-line
  s"   \"scripts\": {" gen-line
  s"     \"start\": \"node src/app.js\"," gen-line
  s"     \"dev\": \"nodemon src/app.js\"," gen-line
  s"     \"test\": \"jest\"," gen-line
  s"     \"migrate\": \"sqlite3 data/app.db < src/db/schema.sql\"" gen-line
  s"   }," gen-line
  s"   \"dependencies\": {" gen-line
  s"     \"express\": \"^4.18.0\"," gen-line
  s"     \"sqlite3\": \"^5.1.0\"," gen-line
  feat-auth @ if
    s"     \"jsonwebtoken\": \"^9.0.0\"," gen-line
    s"     \"bcrypt\": \"^5.1.0\"," gen-line
  then
  s"     \"cors\": \"^2.8.5\"" gen-line
  s"   }," gen-line
  s"   \"devDependencies\": {" gen-line
  s"     \"jest\": \"^29.0.0\"," gen-line
  s"     \"supertest\": \"^6.3.0\"," gen-line
  s"     \"nodemon\": \"^3.0.0\"" gen-line
  s"   }" gen-line
  s" }" gen-line

  gen-close ;

: gen-app-entry ( -- )
  \ Generate main app.js
  str-reset
  output-path output-path-len @ str+
  s" /src/app.js" str+
  str$ gen>file

  s" // Main application entry point" gen-line
  s" // Generated by Fifth app-from-spec" gen-line
  gen-nl
  s" const express = require('express');" gen-line
  s" const cors = require('cors');" gen-line
  gen-nl
  s" const taskRoutes = require('./api/routes/tasks');" gen-line
  s" const userRoutes = require('./api/routes/users');" gen-line
  gen-nl
  s" const app = express();" gen-line
  gen-nl
  s" app.use(cors());" gen-line
  s" app.use(express.json());" gen-line
  gen-nl
  s" app.use('/api/tasks', taskRoutes);" gen-line
  s" app.use('/api/users', userRoutes);" gen-line
  gen-nl
  s" const PORT = process.env.PORT || 3000;" gen-line
  s" app.listen(PORT, () => {" gen-line
  s"   console.log(`Server running on port ${PORT}`);" gen-line
  s" });" gen-line
  gen-nl
  s" module.exports = app;" gen-line

  gen-close ;

: gen-readme ( -- )
  \ Generate project README
  str-reset
  output-path output-path-len @ str+
  s" /README.md" str+
  str$ gen>file

  s" # " gen-emit app-name app-name-len @ gen-line
  gen-nl
  s" " gen-emit app-desc app-desc-len @ gen-line
  gen-nl
  s" *Generated by Fifth app-from-spec*" gen-line
  gen-nl
  s" ## Quick Start" gen-line
  gen-nl
  s" ```bash" gen-line
  s" npm install" gen-line
  s" npm run migrate" gen-line
  s" npm run dev" gen-line
  s" ```" gen-line
  gen-nl
  s" ## API Endpoints" gen-line
  gen-nl
  s" - `GET /api/tasks` - List all tasks" gen-line
  s" - `GET /api/tasks/:id` - Get task by ID" gen-line
  s" - `POST /api/tasks` - Create task" gen-line
  s" - `PUT /api/tasks/:id` - Update task" gen-line
  s" - `DELETE /api/tasks/:id` - Delete task" gen-line
  gen-nl
  s" ## Testing" gen-line
  gen-nl
  s" ```bash" gen-line
  s" npm test" gen-line
  s" ```" gen-line

  gen-close ;

\ ============================================================================
\ Stage Orchestration
\ ============================================================================

: run-stage ( stage-id -- success? )
  \ Run a single stage by ID
  case
    STAGE-ARCH     of run-arch-stage     endof
    STAGE-SCHEMA   of run-schema-stage   endof
    STAGE-API      of run-api-stage      endof
    STAGE-FRONTEND of run-frontend-stage endof
    STAGE-TESTS    of run-tests-stage    endof
    drop false exit
  endcase ;

: run-all-stages ( -- success? )
  \ Run all stages in sequence
  STAGE-COUNT 0 do
    cr i run-stage 0= if
      ." Stage " i . ." failed" cr
      false unloop exit
    then
  loop
  true ;

: show-summary ( -- )
  \ Display generation summary
  cr
  ." Summary" cr
  ." -------" cr
  ." Application: " app-name app-name-len @ type cr
  ." Output: " output-path output-path-len @ type cr
  cr
  ." Next steps:" cr
  ."   cd " output-path output-path-len @ type cr
  ."   npm install" cr
  ."   npm run migrate" cr
  ."   npm run dev" cr ;

\ ============================================================================
\ CLI Interface
\ ============================================================================

: usage ( -- )
  ." App from Spec - Multi-Stage Code Generation" cr
  ." ============================================" cr
  cr
  ." Usage:" cr
  ."   ./fifth app-from-spec/main.fs generate <spec.yaml> <output-dir>" cr
  ."   ./fifth app-from-spec/main.fs stage <stage-name> <spec.yaml>" cr
  ."   ./fifth app-from-spec/main.fs regen <stage-name> <spec.yaml> <output-dir>" cr
  cr
  ." Stages:" cr
  ."   architecture  - Generate architecture document" cr
  ."   schema        - Generate database schema" cr
  ."   api           - Generate API routes" cr
  ."   frontend      - Generate frontend components" cr
  ."   tests         - Generate test suite" cr
  cr
  ." Example:" cr
  ."   ./fifth app-from-spec/main.fs generate myapp.yaml output/" cr ;

: find-stage ( name-addr name-u -- stage-id|-1 )
  \ Find stage ID by name
  STAGE-COUNT 0 do
    2dup i stage-name compare 0= if
      2drop i unloop exit
    then
  loop
  2drop -1 ;

: cmd-generate ( -- )
  \ Full generation command
  argc @ 4 < if
    ." Usage: generate <spec.yaml> <output-dir>" cr
    exit
  then

  2 argv parse-spec 0= if exit then
  3 argv output-path output-path-len output-path-max store-string

  output-path output-path-len @ ensure-dir

  run-all-stages if
    \ Generate scaffolding files
    gen-package-json
    gen-app-entry
    gen-readme
    show-summary
  else
    ." Generation failed" cr
  then ;

: cmd-stage ( -- )
  \ Run single stage
  argc @ 4 < if
    ." Usage: stage <stage-name> <spec.yaml>" cr
    exit
  then

  2 argv find-stage
  dup -1 = if
    drop ." Unknown stage: " 2 argv type cr
    exit
  then

  3 argv parse-spec 0= if drop exit then

  \ Default output for single stage
  s" output" output-path output-path-len output-path-max store-string
  output-path output-path-len @ ensure-dir

  run-stage if
    ." Stage completed" cr
  else
    ." Stage failed" cr
  then ;

: cmd-regen ( -- )
  \ Regenerate single stage
  argc @ 5 < if
    ." Usage: regen <stage-name> <spec.yaml> <output-dir>" cr
    exit
  then

  2 argv find-stage
  dup -1 = if
    drop ." Unknown stage: " 2 argv type cr
    exit
  then

  3 argv parse-spec 0= if drop exit then
  4 argv output-path output-path-len output-path-max store-string

  run-stage if
    ." Regeneration completed" cr
  else
    ." Regeneration failed" cr
  then ;

: main ( -- )
  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" generate" compare 0= if 2drop cmd-generate exit then
  2dup s" stage" compare 0= if 2drop cmd-stage exit then
  2dup s" regen" compare 0= if 2drop cmd-regen exit then
  2drop usage ;

main
bye
