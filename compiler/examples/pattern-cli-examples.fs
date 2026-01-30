\ pattern_cli_examples.fs - Pattern CLI Usage Examples

: header ( -- )
  ." === Fast Forth Pattern CLI Examples ===" cr cr ;

: example1 ( -- )
  ." Example 1: Initialize pattern database" cr
  ." $ fastforth patterns init --db=patterns.db --seed" cr cr ;

: example2 ( -- )
  ." Example 2: List all patterns" cr
  ." $ fastforth patterns list" cr cr ;

: example3 ( -- )
  ." Example 3: List recursive patterns" cr
  ." $ fastforth patterns list --category=recursive" cr cr ;

: example4 ( -- )
  ." Example 4: Query O(1) patterns in JSON format" cr
  ." $ fastforth patterns query --perf='O(1)' --format=json" cr cr ;

: example5 ( -- )
  ." Example 5: Show pattern details" cr
  ." $ fastforth patterns show DUP_TRANSFORM_001" cr cr ;

: example6 ( -- )
  ." Example 6: Search for factorial patterns" cr
  ." $ fastforth patterns search factorial" cr cr ;

: example7 ( -- )
  ." Example 7: Query patterns by tags" cr
  ." $ fastforth patterns query --tags='recursion,optimized'" cr cr ;

: example8 ( -- )
  ." Example 8: Export patterns to JSON" cr
  ." $ fastforth patterns export --output=patterns.json" cr cr ;

: example9 ( -- )
  ." Example 9: Show pattern library statistics" cr
  ." $ fastforth patterns stats" cr cr ;

: example10 ( -- )
  ." Example 10: List first 5 patterns" cr
  ." $ fastforth patterns list --limit=5" cr cr ;

: example11 ( -- )
  ." Example 11: Advanced query - recursive patterns with O(n) complexity" cr
  ." $ fastforth patterns query --category=recursive --perf='O(n)' --format=json" cr cr ;

: example12 ( -- )
  ." Example 12: Import patterns from JSON" cr
  ." $ fastforth patterns import --input=custom_patterns.json" cr cr ;

: main ( -- )
  header
  example1 example2 example3 example4 example5 example6
  example7 example8 example9 example10 example11 example12 ;

main
bye
