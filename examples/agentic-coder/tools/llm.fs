\ fifth/examples/agentic-coder/tools/llm.fs
\ LLM API interaction for agentic coder

\ --- Configuration ---

: anthropic-api ( -- addr u ) s" https://api.anthropic.com/v1/messages" ;
: openai-api ( -- addr u ) s" https://api.openai.com/v1/chat/completions" ;

: default-model ( -- addr u ) s" claude-3-sonnet-20240229" ;
: default-max-tokens ( -- n ) 4096 ;

\ Response buffer
8192 constant llm-buf-size
create llm-response-buf llm-buf-size allot
variable llm-response-len

\ --- JSON Helpers ---

: json-escape ( addr u -- escaped-addr escaped-u )
  \ Escape string for JSON embedding
  \ TODO: Handle quotes, newlines, backslashes
  ;

: extract-json-field ( json-addr json-u field-addr field-u -- value-addr value-u )
  \ Extract field value from JSON (simple implementation)
  \ For proper parsing, shell out to jq
  2drop 2drop s" " ;

\ --- Anthropic Claude API ---

: build-claude-request ( prompt-addr prompt-u -- request-addr request-u )
  str-reset
  s" {" str+
  s" \"model\": \"" str+ default-model str+ s" \"," str+
  s" \"max_tokens\": " str+ default-max-tokens 0 <# #s #> str+ s" ," str+
  s" \"messages\": [{" str+
  s" \"role\": \"user\"," str+
  s" \"content\": \"" str+
  \ TODO: Properly escape prompt
  str+
  s" \"}]}" str+
  str$ ;

: call-claude ( prompt-addr prompt-u -- response-addr response-u success? )
  build-claude-request

  str-reset
  s" curl -s " str+ anthropic-api str+
  s"  -H 'Content-Type: application/json'" str+
  s"  -H 'x-api-key: '\"$ANTHROPIC_API_KEY\"''" str+
  s"  -H 'anthropic-version: 2023-06-01'" str+
  s"  -d '" str+
  str+  \ request body
  s" '" str+

  \ Execute
  str$ system drop

  \ TODO: Capture and parse response
  s" [Claude response would appear here]" true ;

\ --- OpenAI API ---

: build-openai-request ( prompt-addr prompt-u -- request-addr request-u )
  str-reset
  s" {" str+
  s" \"model\": \"gpt-4\"," str+
  s" \"messages\": [{" str+
  s" \"role\": \"user\"," str+
  s" \"content\": \"" str+
  str+
  s" \"}]}" str+
  str$ ;

: call-openai ( prompt-addr prompt-u -- response-addr response-u success? )
  build-openai-request

  str-reset
  s" curl -s " str+ openai-api str+
  s"  -H 'Content-Type: application/json'" str+
  s"  -H 'Authorization: Bearer '\"$OPENAI_API_KEY\"''" str+
  s"  -d '" str+
  str+
  s" '" str+

  str$ system drop

  s" [OpenAI response would appear here]" true ;

\ --- Tool Use Protocol ---

: build-tool-use-prompt ( task-addr task-u tools-addr tools-u -- prompt-addr prompt-u )
  \ Build prompt with tool definitions
  str-reset
  s" You have access to the following tools:\n\n" str+
  str+  \ tools definitions
  s" \n\nTask: " str+
  2swap str+
  s" \n\nUse tools by responding with JSON: {\"tool\": \"name\", \"args\": {...}}" str+
  s" \nWhen done, respond with: {\"done\": true, \"result\": \"...\"}" str+
  str$ ;

: parse-tool-call ( response-addr response-u -- tool-addr tool-u args-addr args-u valid? )
  \ Parse tool call from LLM response
  \ Look for {"tool": "...", "args": {...}}
  \ TODO: Implement with jq
  2drop s" " s" " false ;

: tool-use-loop ( task-addr task-u max-iterations -- result-addr result-u )
  \ Execute tool use loop until done or max iterations
  >r
  r> 0 ?do
    \ Call LLM with current context
    \ Parse response for tool call
    \ Execute tool if requested
    \ Check for done signal
  loop
  s" [Tool loop completed]" ;

\ --- Embeddings (for semantic search) ---

: get-embedding ( text-addr text-u -- )
  \ Get embedding vector for text
  \ Would use OpenAI embeddings API
  str-reset
  s" curl -s https://api.openai.com/v1/embeddings" str+
  s"  -H 'Content-Type: application/json'" str+
  s"  -H 'Authorization: Bearer '\"$OPENAI_API_KEY\"''" str+
  s"  -d '{\"input\": \"" str+
  str+
  s" \", \"model\": \"text-embedding-ada-002\"}'" str+
  str$ system drop ;

\ --- Streaming (placeholder) ---

: stream-response ( prompt-addr prompt-u xt -- )
  \ Stream LLM response, calling xt for each chunk
  \ Fifth doesn't support streaming well, so this would
  \ need to poll or use a temp file approach
  drop 2drop
  s" [Streaming not fully implemented]" type cr ;

\ --- High-level Interface ---

: llm-query ( prompt-addr prompt-u -- response-addr response-u )
  \ Query LLM and return response text
  call-claude if
    \ Parse content from response JSON
    s" .content[0].text" extract-json-field
  else
    s" [Error calling LLM]"
  then ;

: llm-with-tools ( task-addr task-u -- result-addr result-u )
  \ Execute task with tool use
  s" read: Read a file\nwrite: Write a file\nshell: Run command\nsearch: Search code"
  build-tool-use-prompt
  5 tool-use-loop ;
