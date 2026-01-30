\ run_property_tests.fs - Quick test runner for property-based fuzzing

: header ( -- )
  ." Fast Forth Property-Based Fuzzing Test Runner" cr
  ." ==============================================" cr cr ;

: check-directory ( -- )
  s" if [ ! -f 'Cargo.toml' ]; then echo 'Error: Must run from tests/fuzz directory'; exit 1; fi" system ;

: run-quick ( -- )
  ." Running quick validation (corpus tests only)" cr cr
  s" cargo test corpus_tests --lib" system ;

: run-standard ( -- )
  ." Running standard property tests (1000 cases per property)" cr cr
  s" PROPTEST_CASES=${PROPTEST_CASES:-1000} cargo test --lib -- --test-threads=1" system ;

: run-deep ( -- )
  ." Running deep exploration (10000 cases per property)" cr
  ." This may take 10-15 minutes..." cr cr
  s" PROPTEST_CASES=10000 cargo test --lib -- --test-threads=1" system ;

: run-differential ( -- )
  s" if ! command -v gforth >/dev/null 2>&1; then echo 'GForth not found. Install with: brew install gforth'; exit 1; fi" system
  ." Running differential tests against GForth" cr cr
  s" PROPTEST_CASES=${PROPTEST_CASES:-1000} cargo test differential_tests --lib" system ;

: run-oracle ( -- )
  ." Testing GForth differential oracle" cr cr
  s" ./test_gforth_oracle.fs 2>/dev/null || ./test_gforth_oracle.sh" system ;

: show-stats ( -- )
  ." Test Statistics:" cr
  ." ================" cr cr
  ." Property Test Suites:" cr
  s" grep -E 'fn prop_|fn diff_' src/property_tests.rs 2>/dev/null | wc -l | xargs echo '  Test functions:'" system
  cr ." Corpus Cases:" cr
  s" grep -E '^\\s+\"' src/property_tests.rs 2>/dev/null | wc -l | xargs echo '  Edge cases:'" system
  cr ." Default test cases per run:" cr
  ."   Property tests: ~6000" cr
  ."   Corpus tests: 40+" cr
  ."   Differential tests: 200" cr cr
  ." Expected runtime (standard mode):" cr
  ."   Property tests: 2-5 minutes" cr
  ."   Corpus tests: < 1 second" cr
  ."   Differential tests: 1-2 minutes" cr ;

: show-usage ( -- )
  ." Usage: fifth run_property_tests.fs [mode]" cr cr
  ." Modes:" cr
  ."   quick        - Run corpus tests only (< 1 second)" cr
  ."   standard     - Run all property tests with 1000 cases (default)" cr
  ."   deep         - Run all property tests with 10000 cases (~15 min)" cr
  ."   differential - Run differential tests against GForth" cr
  ."   oracle       - Test GForth oracle functionality" cr
  ."   stats        - Show test statistics" cr cr
  ." Environment variables:" cr
  ."   PROPTEST_CASES - Number of test cases per property (default: 1000)" cr cr
  ." Examples:" cr
  ."   fifth run_property_tests.fs quick" cr
  ."   fifth run_property_tests.fs standard" cr
  ."   PROPTEST_CASES=5000 fifth run_property_tests.fs standard" cr
  ."   fifth run_property_tests.fs deep" cr ;

: footer ( -- )
  cr ." ✓ Tests complete!" cr ;

\ Default: run quick tests
: main ( -- )
  header
  check-directory
  run-quick
  footer ;

main
bye
