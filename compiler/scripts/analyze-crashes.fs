\ analyze_crashes.fs - Analyze and minimize crash artifacts from fuzzing
\ Usage: fifth analyze_crashes.fs [crash_directory]

: header ( -- )
  ." ========================================" cr
  ." Fast Forth Crash Analysis Tool" cr
  ." ========================================" cr cr ;

: check-crashes-dir ( -- )
  \ Check if crashes directory exists and has files
  s" CRASHES_DIR=\"${1:-tests/fuzz/artifacts}\"; if [ ! -d \"$CRASHES_DIR\" ]; then echo \"Error: Crash directory not found: $CRASHES_DIR\"; exit 1; fi; CRASHES=$(find \"$CRASHES_DIR\" -type f -name 'crash-*' -o -name '*crash*' 2>/dev/null); CRASH_COUNT=$(echo \"$CRASHES\" | grep -c . || echo 0); if [ \"$CRASH_COUNT\" -eq 0 ]; then echo '✅ No crashes found!'; exit 0; fi; echo \"Found $CRASH_COUNT crash artifacts\"; echo ''" system ;

: analyze-crashes ( -- )
  s" CRASHES_DIR=\"${1:-tests/fuzz/artifacts}\"; FUZZ_DIR='tests/fuzz'; CRASHES=$(find \"$CRASHES_DIR\" -type f -name 'crash-*' -o -name '*crash*' 2>/dev/null); for crash in $CRASHES; do echo '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'; echo \"Crash: $(basename \"$crash\")\"; echo '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'; FUZZER='unknown'; case \"$crash\" in *parser*) FUZZER='fuzz_parser';; *compiler*) FUZZER='fuzz_compiler';; *ssa*) FUZZER='fuzz_ssa';; *optimizer*) FUZZER='fuzz_optimizer';; *codegen*) FUZZER='fuzz_codegen';; esac; echo \"Fuzzer: $FUZZER\"; echo \"Size: $(wc -c < \"$crash\") bytes\"; echo \"Location: $crash\"; echo ''; if file \"$crash\" | grep -q text; then echo 'Content:'; echo '----------------------------------------'; head -20 \"$crash\" | cat -v; echo '----------------------------------------'; echo ''; else echo 'Binary crash artifact (not displaying)'; echo ''; fi; done" system ;

: show-summary ( -- )
  ." ========================================" cr
  ." Crash Summary" cr
  ." ========================================" cr cr
  s" CRASHES_DIR=\"${1:-tests/fuzz/artifacts}\"; CRASHES=$(find \"$CRASHES_DIR\" -type f -name 'crash-*' -o -name '*crash*' 2>/dev/null); echo 'Crashes by fuzzer:'; for fuzzer in fuzz_parser fuzz_compiler fuzz_ssa fuzz_optimizer fuzz_codegen; do count=$(echo \"$CRASHES\" | grep -c \"$fuzzer\" || echo 0); if [ \"$count\" -gt 0 ]; then echo \"  $fuzzer: $count\"; fi; done" system ;

: show-recommendations ( -- )
  cr ." Recommendations:" cr cr
  ." 1. Reproduce crashes:" cr
  ."    cd tests/fuzz" cr
  ."    cargo +nightly fuzz run <fuzzer_name> <crash_file>" cr cr
  ." 2. Debug with stack trace:" cr
  ."    RUST_BACKTRACE=1 cargo +nightly fuzz run <fuzzer_name> <crash_file>" cr cr
  ." 3. Run in debugger:" cr
  ."    cargo +nightly fuzz run --debug <fuzzer_name> <crash_file>" cr
  ."    lldb target/debug/<fuzzer_name>" cr cr
  ." 4. Add regression test:" cr
  ."    - Copy minimized case to tests/regression/" cr
  ."    - Add test case to prevent re-occurrence" cr cr
  ." 5. View coverage:" cr
  ."    cargo +nightly fuzz coverage <fuzzer_name>" cr cr ;

: main ( -- )
  header
  check-crashes-dir
  analyze-crashes
  show-summary
  show-recommendations ;

main
bye
