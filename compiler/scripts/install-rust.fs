\ install-rust.fs - Install Rust for Fast Forth development
\ This enables full optimizations (85-110% of C performance)

: header ( -- )
  ." ════════════════════════════════════════════════════════════" cr
  ."   Fast Forth - Rust Installation" cr
  ." ════════════════════════════════════════════════════════════" cr cr
  ." This will install the Rust toolchain to enable:" cr
  ."   ✓ 85-110% of C performance (vs 30-50% minimal compiler)" cr
  ."   ✓ Hindley-Milner type inference" cr
  ."   ✓ Advanced LLVM optimizations" cr
  ."   ✓ Ability to modify Fast Forth compiler" cr cr
  ." Download size: ~1.5 GB" cr
  ." Install time: 5-25 minutes" cr cr ;

: check-rust ( -- )
  s" if command -v cargo >/dev/null 2>&1; then echo \"Rust is already installed: $(rustc --version)\"; echo ''; echo 'To reinstall, run: rustup self uninstall && rerun this script'; exit 0; fi" system ;

: install-rust ( -- )
  ." Installing Rust..." cr
  ." ════════════════════════════════════════════════════════════" cr
  s" curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y" system
  s" . \"$HOME/.cargo/env\" 2>/dev/null || true" system
  cr
  ." ════════════════════════════════════════════════════════════" cr
  ."   Rust installation complete!" cr
  ." ════════════════════════════════════════════════════════════" cr cr
  s" rustc --version 2>/dev/null || echo 'Run: source $HOME/.cargo/env'" system
  s" cargo --version 2>/dev/null || true" system
  cr ;

: build-fastforth ( -- )
  s" if [ -f 'Cargo.toml' ]; then echo 'Building Fast Forth with full optimizations...'; echo 'This will take 2-5 minutes...'; echo ''; cargo build --release && echo '' && echo '════════════════════════════════════════════════════════════' && echo '  Build complete!' && echo '════════════════════════════════════════════════════════════' && echo '' && echo \"Binary location: target/release/fastforth\" && echo \"Binary size: $(du -h target/release/fastforth 2>/dev/null | cut -f1)\" && echo '' && echo 'Install globally:' && echo '  cargo install --path .' && echo '' && echo 'Test it:' && echo '  ./target/release/fastforth --version' && echo '  ./target/release/fastforth repl'; else echo 'Not in Fast Forth directory. To build Fast Forth:'; echo '  cd /path/to/fast-forth'; echo '  cargo build --release'; fi" system ;

: show-performance ( -- )
  cr
  ." ════════════════════════════════════════════════════════════" cr
  ."   Performance Comparison" cr
  ." ════════════════════════════════════════════════════════════" cr cr
  ."   Minimal compiler:  30-50% of C  (no dependencies)" cr
  ."   Rust+LLVM:         85-110% of C (just installed!)" cr cr
  ."   Speedup: 2-3x faster code execution!" cr cr ;

: main ( -- )
  header
  check-rust
  install-rust
  build-fastforth
  show-performance ;

main
bye
