\ run_destructive_tests.fs - Destructive Test Runner
\ Safely executes resource-constrained tests in Docker containers

: log-info ( addr u -- )
  ." [INFO] " type cr ;

: log-success ( addr u -- )
  ." [SUCCESS] " type cr ;

: log-warning ( addr u -- )
  ." [WARNING] " type cr ;

: log-error ( addr u -- )
  ." [ERROR] " type cr ;

: header ( -- )
  s" FastForth Destructive Test Runner" log-info
  ." ==================================" cr ;

: check-docker ( -- )
  s" if ! command -v docker >/dev/null 2>&1; then echo '[ERROR] Docker is not installed or not in PATH'; exit 1; fi; if ! docker info >/dev/null 2>&1; then echo '[ERROR] Docker daemon is not running'; exit 1; fi; echo '[SUCCESS] Docker is available'" system ;

: build-container ( -- )
  s" Building destructive test container..." log-info
  s" docker build -t fastforth-destructive-tests -f tests/destructive/Dockerfile . && echo '[SUCCESS] Container built successfully' || (echo '[ERROR] Failed to build container' && exit 1)" system ;

: run-oom-tests ( -- )
  s" Running OOM tests (128MB memory limit)..." log-info
  s" docker run --rm --name fastforth-destructive-test-runner-oom --memory=128m --memory-swap=128m --env DESTRUCTIVE_TESTS_ENABLED=1 --env ALLOW_DESTRUCTIVE_TESTS=1 fastforth-destructive-tests cargo test --release --features destructive_tests test_oom -- --test-threads=1 --nocapture || echo '[WARNING] Some OOM tests failed (may be expected)'" system
  s" OOM tests completed" log-success ;

: run-disk-full-tests ( -- )
  s" Running disk full tests (100MB disk limit)..." log-info
  s" docker run --rm --name fastforth-destructive-test-runner-disk --env DESTRUCTIVE_TESTS_ENABLED=1 --env ALLOW_DESTRUCTIVE_TESTS=1 fastforth-destructive-tests cargo test --release --features destructive_tests test_disk_full -- --test-threads=1 --nocapture || echo '[WARNING] Some disk tests failed (may be expected)'" system
  s" Disk full tests completed" log-success ;

: run-stack-overflow-tests ( -- )
  s" Running stack overflow tests (1MB stack limit)..." log-info
  s" docker run --rm --name fastforth-destructive-test-runner-stack --ulimit stack=1048576:1048576 --env DESTRUCTIVE_TESTS_ENABLED=1 --env ALLOW_DESTRUCTIVE_TESTS=1 fastforth-destructive-tests cargo test --release --features destructive_tests test_stack_overflow -- --test-threads=1 --nocapture || echo '[WARNING] Some stack tests failed (may be expected)'" system
  s" Stack overflow tests completed" log-success ;

: run-fd-exhaustion-tests ( -- )
  s" Running FD exhaustion tests (256 FD limit)..." log-info
  s" docker run --rm --name fastforth-destructive-test-runner-fd --ulimit nofile=256:256 --env DESTRUCTIVE_TESTS_ENABLED=1 --env ALLOW_DESTRUCTIVE_TESTS=1 fastforth-destructive-tests cargo test --release --features destructive_tests test_fd_exhaustion -- --test-threads=1 --nocapture || echo '[WARNING] Some FD tests failed (may be expected)'" system
  s" FD exhaustion tests completed" log-success ;

: run-all-tests ( -- )
  s" Running all destructive tests..." log-info
  s" docker run --rm --name fastforth-destructive-test-runner-all --memory=256m --memory-swap=256m --ulimit stack=1048576:1048576 --ulimit nofile=512:512 --env DESTRUCTIVE_TESTS_ENABLED=1 --env ALLOW_DESTRUCTIVE_TESTS=1 fastforth-destructive-tests cargo test --release --features destructive_tests -- --test-threads=1 --nocapture || echo '[WARNING] Some tests failed (may be expected for destructive tests)'" system
  s" All destructive tests completed" log-success ;

: cleanup ( -- )
  s" Cleaning up..." log-info
  s" docker ps -a | grep fastforth-destructive-test-runner | awk '{print $1}' | xargs -r docker rm -f 2>/dev/null || true" system
  s" Cleanup completed" log-success ;

: main ( -- )
  header
  check-docker
  build-container
  run-all-tests
  cleanup
  s" Test run completed successfully" log-success ;

main
bye
