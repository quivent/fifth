# TinyCC for Fifth

TinyCC (tcc) is an **optional** dependency for zero-toolchain C compilation.

## Why TCC?

The Fifth compiler can emit C code (`fifthc --emit-c`). You can compile this with:
- **clang** (macOS default) — already installed
- **gcc** — `brew install gcc`
- **tcc** — smallest, fastest compile, but optional

## Installing TCC

### macOS (Homebrew)

TCC isn't in Homebrew by default. Options:

1. **Use clang instead** (recommended for macOS):
   ```bash
   fifthc compile program.fs --emit-c -o program.c
   clang -O2 program.c -o program
   ```

2. **Build from git** (has macOS fixes):
   ```bash
   git clone https://repo.or.cz/tinycc.git
   cd tinycc
   ./configure --prefix=/usr/local
   make && sudo make install
   ```

### Linux

```bash
# Debian/Ubuntu
apt install tcc

# Fedora
dnf install tcc

# Arch
pacman -S tcc
```

## Performance

| Compiler | Compile Time | Runtime | Size |
|----------|--------------|---------|------|
| clang -O2 | 10-20ms | 60-70% of C | varies |
| gcc -O2 | 15-30ms | 65-75% of C | varies |
| tcc | 2-5ms | 40-50% of C | smallest |

TCC compiles ~5x faster but produces slower code. Good for rapid iteration.

## Using with Fifth

```bash
# With clang (default on macOS)
fifthc compile program.fs --emit-c
clang -O2 program.c -o program

# With tcc (if installed)
fifthc compile program.fs --emit-c
tcc program.c -o program
```
