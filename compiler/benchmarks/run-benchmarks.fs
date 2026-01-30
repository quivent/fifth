\ run-benchmarks.fs - Benchmark Runner
\ Runs C baseline, GForth, and Fifth benchmarks

: header ( -- )
  ." ============================================================" cr
  ." Fifth Benchmark Runner" cr
  ." ============================================================" cr cr ;

: platform-info ( -- )
  ." Platform Information:" cr
  s" echo \"  OS: $(uname -s) $(uname -m)\"" system
  s" gcc --version 2>/dev/null | head -1 | sed 's/^/  GCC: /'" system
  s" gforth --version 2>/dev/null | head -1 | sed 's/^/  GForth: /' || echo '  GForth: Not installed'" system
  cr ;

: run-c-benchmarks ( -- )
  ." ============================================================" cr
  ." C BASELINE BENCHMARKS (gcc -O2)" cr
  ." ============================================================" cr
  s" cd benchmarks/c_baseline && make -s 2>/dev/null" system
  s" cd benchmarks/c_baseline && ./sieve 8190 100 2>/dev/null | grep -E 'Average|Validation'" system
  s" cd benchmarks/c_baseline && ./fibonacci 35 40 2>/dev/null | grep -E 'Average|Validation'" system
  s" cd benchmarks/c_baseline && ./matrix 100 10 2>/dev/null | grep -E 'Average'" system
  s" cd benchmarks/c_baseline && ./bubble_sort 1000 10 2>/dev/null | grep -E 'Average'" system
  cr ;

: run-gforth-benchmarks ( -- )
  ." ============================================================" cr
  ." GFORTH BENCHMARKS" cr
  ." ============================================================" cr
  s" if command -v gforth >/dev/null 2>&1; then echo 'Running GForth benchmarks...'; for f in benchmarks/forth/*.fth; do echo \"  $f\"; time gforth \"$f\" -e bye 2>&1 | grep real; done; else echo 'GForth not installed'; fi" system
  cr ;

: run-fifth-benchmarks ( -- )
  ." ============================================================" cr
  ." FIFTH BENCHMARKS" cr
  ." ============================================================" cr
  s" if [ -x engine/fifth ]; then echo 'Running Fifth benchmarks...'; for f in benchmarks/forth/*.fth; do echo \"  $f\"; time ./engine/fifth \"$f\" 2>&1 | grep real; done; else echo 'Fifth not built - run: cd engine && make'; fi" system
  cr ;

: compare-results ( -- )
  ." ============================================================" cr
  ." COMPARISON" cr
  ." ============================================================" cr
  ." Target: 85-110% of C performance" cr
  ." " cr
  ." Benchmark targets (from PERFORMANCE.md):" cr
  ."   Sieve (8190):     0.004 ms (C baseline)" cr
  ."   Fibonacci (35):   1.968 ms (C baseline)" cr
  ."   Matrix (100x100): 0.465 ms (C baseline)" cr
  ."   Bubble (1000):    0.266 ms (C baseline)" cr
  cr ;

: save-results ( -- )
  s" echo '{\"timestamp\":\"'$(date -Iseconds)'\",\"platform\":\"'$(uname -sm)'\"}' > benchmarks/results.json" system
  ." Results saved to benchmarks/results.json" cr ;

: run-all ( -- )
  header
  platform-info
  run-c-benchmarks
  run-gforth-benchmarks
  run-fifth-benchmarks
  compare-results
  save-results ;

: main ( -- )
  run-all ;

main
bye
