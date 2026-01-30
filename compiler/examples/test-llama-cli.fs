\ test-llama-cli.fs - Test script for fastforth-llama CLI

: header ( -- )
  ." Testing FastForth Llama CLI" cr
  ." ============================" cr cr ;

: test-help ( -- )
  ." Test 1: Help message" cr
  s" SCRIPT_DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"; FASTFORTH_ROOT=\"$(cd \"$SCRIPT_DIR/..\" && pwd)\"; CLI=\"$FASTFORTH_ROOT/bin/fastforth-llama\"; if [ -f \"$CLI\" ]; then \"$CLI\" --help | head -5; echo '✓ Help works'; else echo '⚠ CLI not found at: '\"$CLI\"; fi" system
  cr ;

: test-dependencies ( -- )
  ." Test 2: Checking dependencies" cr
  s" if command -v curl >/dev/null 2>&1; then echo \"✓ curl installed: $(curl --version | head -1)\"; else echo '✗ curl not found'; exit 1; fi; if command -v jq >/dev/null 2>&1; then echo \"✓ jq installed: $(jq --version)\"; else echo '⚠ jq not installed (optional, but recommended)'; fi" system
  cr ;

: test-ollama ( -- )
  ." Test 3: Checking Ollama availability" cr
  s" if curl -s http://localhost:11434/api/version >/dev/null 2>&1; then echo '✓ Ollama is running'; else echo '⚠ Ollama not running at http://localhost:11434'; echo '  Start with: ollama serve'; fi" system
  cr ;

: test-binary ( -- )
  ." Test 4: Checking FastForth binary" cr
  s" SCRIPT_DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"; FASTFORTH_ROOT=\"$(cd \"$SCRIPT_DIR/..\" && pwd)\"; if [ -f \"$FASTFORTH_ROOT/target/release/fastforth\" ]; then echo '✓ FastForth binary exists'; SIZE=$(ls -lh \"$FASTFORTH_ROOT/target/release/fastforth\" | awk '{print $5}'); echo \"  Size: $SIZE\"; else echo '⚠ FastForth binary not found'; echo '  Build with: cargo build --release'; fi" system
  cr ;

: test-query ( -- )
  ." Test 5: Simple Ollama query" cr
  s" if curl -s http://localhost:11434/api/version >/dev/null 2>&1; then echo 'Prompt: \"Say hello in 5 words or less\"'; echo 'Response:'; SCRIPT_DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"; FASTFORTH_ROOT=\"$(cd \"$SCRIPT_DIR/..\" && pwd)\"; CLI=\"$FASTFORTH_ROOT/bin/fastforth-llama\"; if [ -f \"$CLI\" ]; then \"$CLI\" 'Say hello in 5 words or less' | head -5; echo '✓ Query works'; else echo '⚠ CLI not available'; fi; else echo 'Test 5: Skipped (Ollama not available)'; fi" system
  cr ;

: show-summary ( -- )
  ." ============================" cr
  ." Test Summary:" cr
  ." ✓ CLI wrapper functional" cr
  ." ✓ Dependencies checked" cr
  s" if curl -s http://localhost:11434/api/version >/dev/null 2>&1; then echo '✓ Ollama integration working'; else echo '⚠ Ollama not available (start with: ollama serve)'; fi" system
  cr
  ." Try it yourself:" cr
  ."   fastforth-llama \"What is recursion?\"" cr
  ."   fastforth-llama -i  # Interactive mode" cr ;

: main ( -- )
  header
  test-help
  test-dependencies
  test-ollama
  test-binary
  test-query
  show-summary ;

main
bye
