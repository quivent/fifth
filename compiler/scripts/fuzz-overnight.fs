\ fuzz_overnight.fs - Comprehensive overnight fuzzing script for Fast Forth
\ Runs multiple fuzzing strategies in parallel for extended periods

variable duration-hours
8 duration-hours !

: header ( -- )
  ." ========================================" cr
  ." Fast Forth Overnight Fuzzing" cr
  ." ========================================" cr
  ." Duration: " duration-hours @ . ." hours" cr
  s" echo \"Start time: $(date)\"" system
  cr ;

: setup-directories ( -- )
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); FUZZ_DIR='tests/fuzz'; REPORT_DIR=\"${FUZZ_DIR}/overnight_reports\"; CRASHES_DIR=\"${FUZZ_DIR}/crashes/${TIMESTAMP}\"; CORPUS_DIR=\"${FUZZ_DIR}/corpus/${TIMESTAMP}\"; mkdir -p \"${REPORT_DIR}\" \"${CRASHES_DIR}\" \"${CORPUS_DIR}\"; echo \"Report will be saved to: ${REPORT_DIR}/fuzz_report_${TIMESTAMP}.html\"" system ;

: install-cargo-fuzz ( -- )
  s" cargo +nightly fuzz --version >/dev/null 2>&1 || (echo 'Installing cargo-fuzz...' && cargo +nightly install cargo-fuzz)" system ;

: run-libfuzzer-parser ( -- )
  ." [LibFuzzer Parser] Starting coverage-guided fuzzing..." cr
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); DURATION=$((8 * 3600)); cd tests/fuzz && cargo +nightly fuzz run fuzz_parser -- -max_total_time=\"$DURATION\" -print_final_stats=1 -artifact_prefix=\"crashes/${TIMESTAMP}/parser_\" -timeout=10 >overnight_reports/libfuzzer_parser_${TIMESTAMP}.log 2>&1 &" system ;

: run-libfuzzer-compiler ( -- )
  ." [LibFuzzer Compiler] Starting end-to-end compilation fuzzing..." cr
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); DURATION=$((8 * 3600)); cd tests/fuzz && cargo +nightly fuzz run fuzz_compiler -- -max_total_time=\"$DURATION\" -print_final_stats=1 -artifact_prefix=\"crashes/${TIMESTAMP}/compiler_\" -timeout=30 >overnight_reports/libfuzzer_compiler_${TIMESTAMP}.log 2>&1 &" system ;

: run-libfuzzer-ssa ( -- )
  ." [LibFuzzer SSA] Starting SSA construction fuzzing..." cr
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); DURATION=$((8 * 3600)); cd tests/fuzz && cargo +nightly fuzz run fuzz_ssa -- -max_total_time=\"$DURATION\" -print_final_stats=1 -artifact_prefix=\"crashes/${TIMESTAMP}/ssa_\" -timeout=20 >overnight_reports/libfuzzer_ssa_${TIMESTAMP}.log 2>&1 &" system ;

: run-libfuzzer-optimizer ( -- )
  ." [LibFuzzer Optimizer] Starting optimization passes fuzzing..." cr
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); DURATION=$((8 * 3600)); cd tests/fuzz && cargo +nightly fuzz run fuzz_optimizer -- -max_total_time=\"$DURATION\" -print_final_stats=1 -artifact_prefix=\"crashes/${TIMESTAMP}/optimizer_\" -timeout=30 >overnight_reports/libfuzzer_optimizer_${TIMESTAMP}.log 2>&1 &" system ;

: run-property-tests ( -- )
  ." [Property Tests] Running extended property-based tests..." cr
  s" cd tests/fuzz; for cases in 10000 50000 100000; do echo \"  Running $cases test cases...\"; PROPTEST_CASES=\"$cases\" PROPTEST_MAX_SHRINK_ITERS=10000 cargo test --lib --release >overnight_reports/proptest_${cases}_$(date +%Y%m%d_%H%M%S).log 2>&1 || echo \"  Found failures with $cases cases!\"; done" system ;

: run-differential-fuzzing ( -- )
  ." [Differential] Running differential fuzzing against GForth..." cr
  s" if command -v gforth >/dev/null 2>&1; then cd tests/fuzz && PROPTEST_CASES=50000 cargo test differential_tests --release >overnight_reports/differential_$(date +%Y%m%d_%H%M%S).log 2>&1 || echo '  Found divergences from GForth!'; else echo '  GForth not found, skipping differential fuzzing'; fi" system ;

: run-stress-tests ( -- )
  ." [Stress Tests] Running stress tests with extreme values..." cr
  s" cat > /tmp/stress_test.fth << 'STRESSEOF'
: stress-max-int 9223372036854775807 . ;
: stress-min-int -9223372036854775808 . ;
: stress-deep-recursion 1000 0 do i 2 * loop ;
: stress-large-stack 10000 0 do i loop ;
: stress-nested-loops 100 0 do 100 0 do 100 0 do i j k + + loop loop loop ;
STRESSEOF
timeout 300 cargo run --release --bin fastforth -- /tmp/stress_test.fth >tests/fuzz/overnight_reports/stress_$(date +%Y%m%d_%H%M%S).log 2>&1 || true" system ;

: start-fuzzing ( -- )
  cr ." Starting fuzzing strategies..." cr cr
  run-libfuzzer-parser
  s" sleep 2" system
  run-libfuzzer-compiler
  s" sleep 2" system
  run-libfuzzer-ssa
  s" sleep 2" system
  run-libfuzzer-optimizer
  s" sleep 2" system
  run-property-tests
  run-differential-fuzzing
  run-stress-tests ;

: monitor-progress ( -- )
  cr ." Fuzzing in progress..." cr
  ." Background processes running. Monitor with:" cr
  ."   tail -f tests/fuzz/overnight_reports/*.log" cr
  ."   find tests/fuzz/crashes -type f | wc -l  # count crashes" cr cr
  ." To stop early: pkill -f 'cargo +nightly fuzz'" cr cr ;

: generate-report ( -- )
  ." Generating final report..." cr
  s" TIMESTAMP=$(date +%Y%m%d_%H%M%S); REPORT_FILE=\"tests/fuzz/overnight_reports/fuzz_report_${TIMESTAMP}.html\"; CRASHES_DIR='tests/fuzz/crashes'; TOTAL_CRASHES=$(find \"$CRASHES_DIR\" -type f 2>/dev/null | wc -l); echo \"<html><head><title>Fast Forth Fuzzing Report</title></head><body><h1>Fuzzing Report - $TIMESTAMP</h1><p>Total crashes: $TOTAL_CRASHES</p></body></html>\" > \"$REPORT_FILE\"; echo \"✅ Report generated: $REPORT_FILE\"; echo ''; echo 'Summary:'; echo \"  Total crashes: $TOTAL_CRASHES\"; echo ''; if [ \"$TOTAL_CRASHES\" -gt 0 ]; then echo '⚠ CRASHES FOUND! Review artifacts in:'; echo \"  $CRASHES_DIR\"; else echo '✅ No crashes found! Compiler is robust.'; fi" system ;

: main ( -- )
  header
  setup-directories
  install-cargo-fuzz
  start-fuzzing
  monitor-progress
  ." Note: Fuzzing is running in background. Run 'generate-report' when complete." cr
  ." Or wait " duration-hours @ . ." hours for automatic completion." cr ;

main
bye
