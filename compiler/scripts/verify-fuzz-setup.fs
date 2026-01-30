\ verify_fuzz_setup.fs - Verify fuzzing infrastructure is correctly set up

variable errors
variable warnings
0 errors !
0 warnings !

: check-ok ( addr u -- )
  ." ✓ " type cr ;

: check-fail ( addr u -- )
  ." ✗ " type cr
  1 errors +! ;

: check-warn ( addr u -- )
  ." ⚠ " type cr
  1 warnings +! ;

: header ( -- )
  ." ========================================" cr
  ." Fast Forth Fuzzing Setup Verification" cr
  ." ========================================" cr cr ;

: check-rust ( -- )
  ." Checking Rust installation..." cr
  s" if command -v rustc >/dev/null 2>&1; then echo \"✓ Rust installed: $(rustc --version)\"; else echo '✗ Rust not installed'; fi" system ;

: check-nightly ( -- )
  s" if command -v cargo >/dev/null 2>&1 && cargo +nightly --version >/dev/null 2>&1; then echo \"✓ Nightly toolchain: $(cargo +nightly --version)\"; else echo '✗ Nightly toolchain not installed (run: rustup install nightly)'; fi" system ;

: check-cargo-fuzz ( -- )
  s" if cargo +nightly fuzz --version >/dev/null 2>&1; then echo \"✓ cargo-fuzz installed: $(cargo +nightly fuzz --version 2>&1 | head -1)\"; else echo '⚠ cargo-fuzz not installed (run: cargo +nightly install cargo-fuzz)'; fi" system ;

: check-gforth ( -- )
  cr ." Checking optional dependencies..." cr
  s" if command -v gforth >/dev/null 2>&1; then echo \"✓ GForth installed: $(gforth --version 2>&1 | head -1) (differential testing available)\"; else echo '⚠ GForth not installed (differential testing disabled)'; echo '  Install with: brew install gforth (macOS) or apt install gforth (Linux)'; fi" system ;

: check-directories ( -- )
  cr ." Checking directory structure..." cr
  s" for dir in tests/fuzz tests/fuzz/fuzz_targets tests/fuzz/src scripts; do if [ -d \"$dir\" ]; then echo \"✓ Directory exists: $dir\"; else echo \"✗ Directory missing: $dir\"; fi; done" system ;

: check-fuzz-targets ( -- )
  cr ." Checking fuzz targets..." cr
  s" for target in tests/fuzz/fuzz_targets/fuzz_parser.rs tests/fuzz/fuzz_targets/fuzz_compiler.rs tests/fuzz/fuzz_targets/fuzz_ssa.rs tests/fuzz/fuzz_targets/fuzz_optimizer.rs tests/fuzz/fuzz_targets/fuzz_codegen.rs; do if [ -f \"$target\" ]; then echo \"✓ Fuzz target: $(basename \"$target\")\"; else echo \"✗ Missing fuzz target: $target\"; fi; done" system ;

: check-scripts ( -- )
  cr ." Checking fuzzing scripts..." cr
  s" for script in scripts/fuzz_overnight.sh scripts/quick_fuzz.sh scripts/analyze_crashes.sh scripts/fuzz_overnight.fs scripts/quick_fuzz.fs scripts/analyze_crashes.fs; do if [ -f \"$script\" ]; then if [ -x \"$script\" ] || echo \"$script\" | grep -q '\\.fs$'; then echo \"✓ Script: $(basename \"$script\")\"; else echo \"⚠ Script: $(basename \"$script\") (not executable)\"; fi; fi; done" system ;

: check-cargo-toml ( -- )
  cr ." Checking Cargo configuration..." cr
  s" if [ -f 'tests/fuzz/Cargo.toml' ]; then echo '✓ Fuzz Cargo.toml exists'; if grep -q 'fuzz_parser' tests/fuzz/Cargo.toml; then echo '✓ Parser target registered in Cargo.toml'; else echo '✗ Parser target not registered in Cargo.toml'; fi; if grep -q 'fuzz_compiler' tests/fuzz/Cargo.toml; then echo '✓ Compiler target registered in Cargo.toml'; else echo '✗ Compiler target not registered in Cargo.toml'; fi; else echo '✗ tests/fuzz/Cargo.toml missing'; fi" system ;

: test-build ( -- )
  cr ." Testing fuzz build..." cr
  s" cd tests/fuzz && cargo +nightly build --bins >/tmp/fuzz_build.log 2>&1 && echo '✓ Fuzz targets build successfully' || (echo '✗ Fuzz targets failed to build (see /tmp/fuzz_build.log)' && tail -20 /tmp/fuzz_build.log)" system ;

: test-property-build ( -- )
  cr ." Testing property tests build..." cr
  s" cd tests/fuzz && cargo test --lib --no-run >/tmp/proptest_build.log 2>&1 && echo '✓ Property tests build successfully' || (echo '✗ Property tests failed to build (see /tmp/proptest_build.log)' && tail -20 /tmp/proptest_build.log)" system ;

: show-summary ( -- )
  cr ." ========================================" cr
  ." Summary" cr
  ." ========================================" cr cr
  ." Quick start:" cr
  ."   ./scripts/quick_fuzz.fs              # 5-minute quick fuzz" cr
  ."   fifth scripts/quick_fuzz.fs" cr
  ."   ./scripts/fuzz_overnight.fs          # 8-hour overnight fuzz" cr cr
  ." Documentation:" cr
  ."   tests/fuzz/README.md                 # Fuzzing guide" cr
  ."   fuzz/README.md                       # Comprehensive documentation" cr cr ;

: main ( -- )
  header
  check-rust
  check-nightly
  check-cargo-fuzz
  check-gforth
  check-directories
  check-fuzz-targets
  check-scripts
  check-cargo-toml
  test-build
  test-property-build
  show-summary ;

main
bye
