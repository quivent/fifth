\ quick_fuzz.fs - Quick fuzzing for rapid iteration during development
\ Runs for 5 minutes across all targets

variable fuzz-duration
300 fuzz-duration !  \ 5 minutes

: header ( -- )
  ." Quick Fuzzing (5 minutes per target)" cr cr ;

: install-cargo-fuzz ( -- )
  s" cargo +nightly fuzz --version >/dev/null 2>&1 || (echo 'Installing cargo-fuzz...' && cargo +nightly install cargo-fuzz)" system ;

: fuzz-target ( addr u -- )
  \ Fuzz a single target
  ." Fuzzing " 2dup type ." ..." cr
  str-reset
  s" cd tests/fuzz && cargo +nightly fuzz run " str+
  str+  \ target name
  s"  -- -max_total_time=" str+
  fuzz-duration @ s>d <# #s #> str+
  s"  -print_final_stats=1" str+
  str$ system ;

: fuzz-all-targets ( -- )
  s" fuzz_parser" fuzz-target
  s" fuzz_compiler" fuzz-target
  s" fuzz_ssa" fuzz-target
  s" fuzz_optimizer" fuzz-target
  s" fuzz_codegen" fuzz-target ;

: check-crashes ( -- )
  cr ." Quick fuzz complete! Check artifacts/ for any crashes." cr
  s" if [ -d 'tests/fuzz/artifacts' ] && [ \"$(find tests/fuzz/artifacts -type f 2>/dev/null | wc -l)\" -gt 0 ]; then echo ''; echo 'Crashes found! Running analysis...'; cd tests/fuzz && ../../scripts/analyze_crashes.fs artifacts 2>/dev/null || ../../scripts/analyze_crashes.sh artifacts 2>/dev/null || echo 'Run: ./scripts/analyze_crashes.fs artifacts'; fi" system ;

: main ( -- )
  header
  s" cd tests/fuzz" system
  install-cargo-fuzz
  fuzz-all-targets
  check-crashes ;

main
bye
