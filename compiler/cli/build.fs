\ build.fs - Fast Forth CLI build script

: header ( -- )
  ." Fast Forth CLI Build Script" cr
  ." ==============================" cr cr ;

: check-rust ( -- )
  s" if ! command -v cargo >/dev/null 2>&1; then echo 'Error: Rust is not installed'; echo 'Please install Rust from: https://rustup.rs/'; exit 1; fi; echo \"✓ Rust version: $(rustc --version)\"" system
  cr ;

: build-debug ( -- )
  ." Building debug version..." cr
  s" cargo build" system ;

: build-release ( -- )
  ." Building release version (optimized)..." cr
  s" cargo build --release" system ;

: show-result-debug ( -- )
  cr
  s" if [ -f 'target/debug/fastforth' ]; then echo '✓ Build successful!'; echo ''; echo \"Binary location: target/debug/fastforth\"; echo \"Size: $(du -h target/debug/fastforth | cut -f1)\"; echo ''; echo 'Quick test:'; echo '  target/debug/fastforth --version'; echo '  target/debug/fastforth --help'; echo '  target/debug/fastforth repl'; echo '  target/debug/fastforth run examples/hello.fth'; echo ''; echo 'Running quick test...'; if target/debug/fastforth --version 2>/dev/null; then echo '✓ Quick test passed!'; else echo '⚠ Quick test failed'; fi; else echo 'Build failed'; fi" system ;

: show-result-release ( -- )
  cr
  s" if [ -f 'target/release/fastforth' ]; then echo '✓ Build successful!'; echo ''; echo \"Binary location: target/release/fastforth\"; echo \"Size: $(du -h target/release/fastforth | cut -f1)\"; echo ''; echo 'Quick test:'; echo '  target/release/fastforth --version'; echo '  target/release/fastforth --help'; echo '  target/release/fastforth repl'; echo '  target/release/fastforth run examples/hello.fth'; echo ''; echo 'Running quick test...'; if target/release/fastforth --version 2>/dev/null; then echo '✓ Quick test passed!'; else echo '⚠ Quick test failed'; fi; else echo 'Build failed'; fi" system ;

\ Default to debug build - pass 'release' argument to build release
: main ( -- )
  header
  check-rust
  build-debug
  show-result-debug
  cr ." Build complete!" cr ;

: main-release ( -- )
  header
  check-rust
  build-release
  show-result-release
  cr ." Build complete!" cr ;

\ Run debug build by default
main
bye
