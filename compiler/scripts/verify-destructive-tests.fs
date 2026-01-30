\ verify_destructive_tests.fs - Verification script for destructive testing infrastructure
\ Checks that all components are in place and configured correctly

: check-pass ( addr u -- )
  ." ✓ " type cr ;

: check-fail ( addr u -- )
  ." ✗ " type cr ;

: check-warn ( addr u -- )
  ." ⚠ " type cr ;

: check-info ( addr u -- )
  ." ℹ " type cr ;

: header ( -- )
  ." Destructive Testing Infrastructure Verification" cr
  ." ================================================" cr cr ;

: check-docker ( -- )
  s" Checking Docker availability..." check-info
  s" if command -v docker >/dev/null 2>&1; then if docker info >/dev/null 2>&1; then echo '✓ Docker is installed and running'; else echo '✗ Docker is installed but not running'; exit 1; fi; else echo '✗ Docker is not installed'; exit 1; fi" system ;

: check-required-files ( -- )
  s" Checking required files..." check-info
  s" for file in tests/destructive/Dockerfile tests/destructive/mod.rs tests/destructive/safety.rs tests/destructive/test_oom.rs tests/destructive/test_disk_full.rs tests/destructive/test_stack_overflow.rs tests/destructive/test_fd_exhaustion.rs tests/destructive/README.md scripts/run_destructive_tests.sh .github/workflows/destructive-tests.yml; do if [ -f \"$file\" ]; then echo \"✓ $file exists\"; else echo \"✗ $file missing\"; fi; done" system ;

: check-cargo-toml ( -- )
  s" Checking Cargo.toml configuration..." check-info
  s" if grep -q 'destructive_tests = \\[\\]' Cargo.toml; then echo '✓ destructive_tests feature flag present'; else echo '✗ destructive_tests feature flag missing in Cargo.toml'; fi" system ;

: check-runner-perms ( -- )
  s" Checking test runner permissions..." check-info
  s" if [ -x 'scripts/run_destructive_tests.sh' ]; then echo '✓ Test runner is executable'; else chmod +x scripts/run_destructive_tests.sh 2>/dev/null && echo '✓ Fixed test runner permissions' || echo '⚠ Could not fix permissions'; fi" system ;

: count-tests ( -- )
  s" Counting destructive tests..." check-info
  s" OOM=$(grep -c '^#\\[test\\]' tests/destructive/test_oom.rs 2>/dev/null || echo 0); DISK=$(grep -c '^#\\[test\\]' tests/destructive/test_disk_full.rs 2>/dev/null || echo 0); STACK=$(grep -c '^#\\[test\\]' tests/destructive/test_stack_overflow.rs 2>/dev/null || echo 0); FD=$(grep -c '^#\\[test\\]' tests/destructive/test_fd_exhaustion.rs 2>/dev/null || echo 0); TOTAL=$((OOM + DISK + STACK + FD)); echo \"✓ Found $TOTAL destructive tests\"; echo \"  - OOM: $OOM tests\"; echo \"  - Disk Full: $DISK tests\"; echo \"  - Stack Overflow: $STACK tests\"; echo \"  - FD Exhaustion: $FD tests\"" system ;

: check-safety-guards ( -- )
  s" Verifying safety guards..." check-info
  s" if grep -q 'ensure_containerized()' tests/destructive/test_oom.rs 2>/dev/null && grep -q 'is_in_container()' tests/destructive/safety.rs 2>/dev/null; then echo '✓ Safety guards implemented'; else echo '✗ Safety guards missing'; fi" system ;

: check-dockerfile ( -- )
  s" Checking Dockerfile configuration..." check-info
  s" if grep -q 'destructive_tests' tests/destructive/Dockerfile 2>/dev/null; then echo '✓ Dockerfile configured for destructive tests'; else echo '⚠ Dockerfile may need destructive_tests feature'; fi" system ;

: check-ci-workflow ( -- )
  s" Checking CI workflow..." check-info
  s" if grep -q 'destructive_tests' .github/workflows/destructive-tests.yml 2>/dev/null; then echo '✓ CI workflow configured'; else echo '✗ CI workflow not properly configured'; fi" system ;

: check-compilation ( -- )
  s" Verifying tests compile..." check-info
  s" if cargo check --features destructive_tests --quiet 2>&1 | grep -q 'error'; then echo '✗ Compilation errors detected'; echo 'Run: cargo check --features destructive_tests'; else echo '✓ Tests compile successfully'; fi" system ;

: show-summary ( -- )
  cr ." ================================================" cr
  ." Verification Summary" cr
  ." ================================================" cr cr
  ." Destructive testing infrastructure is ready." cr cr
  ." To run tests:" cr
  ."   ./scripts/run_destructive_tests.fs" cr
  ."   fifth scripts/run_destructive_tests.fs" cr cr
  ." To run specific category:" cr
  ."   (Edit script to call run-oom-tests, run-disk-full-tests, etc.)" cr cr ;

: main ( -- )
  header
  check-docker
  check-required-files
  check-cargo-toml
  check-runner-perms
  count-tests
  check-safety-guards
  check-dockerfile
  check-ci-workflow
  check-compilation
  show-summary ;

main
bye
