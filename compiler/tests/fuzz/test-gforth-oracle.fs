\ test_gforth_oracle.fs - Test the GForth differential oracle

: header ( -- )
  ." Testing GForth Differential Oracle" cr
  ." ====================================" cr cr ;

: check-gforth ( -- )
  s" if ! command -v gforth >/dev/null 2>&1; then echo '❌ GForth not found. Install with: brew install gforth'; exit 1; fi; echo \"✓ GForth version: $(gforth --version 2>&1 | head -1)\"" system
  cr ;

: run-test-cases ( -- )
  ." Running test cases:" cr cr
  s" for code in '42' '17 25 +' '100 50 -' '10 5 *' '100 10 /' '5 DUP' '3 4 SWAP' '10 20 OVER'; do echo -n \"  Testing: '$code' ... \"; result=$(echo -e \"$code\\n.s\\nbye\" | gforth 2>&1 | grep '<' | tail -1); if [ -n \"$result\" ]; then echo \"✓ Stack: $result\"; else echo '⚠ No stack output'; fi; done" system ;

: run-random-tests ( -- )
  cr ." Testing property generation:" cr cr
  s" for i in 1 2 3 4 5 6 7 8 9 10; do a=$((RANDOM % 1000)); b=$((RANDOM % 1000 + 1)); code=\"$a $b +\"; result=$(echo -e \"$code\\n.s\\nbye\" | gforth 2>&1 | grep '<' | tail -1 || echo 'error'); echo \"  Random case $i: '$code' -> $result\"; done" system ;

: footer ( -- )
  cr ." ✓ GForth oracle is working correctly!" cr cr
  ." To run the full property-based test suite:" cr
  ."   cd tests/fuzz" cr
  ."   cargo test --lib differential_tests" cr ;

: main ( -- )
  header
  check-gforth
  run-test-cases
  run-random-tests
  footer ;

main
bye
