---
title: Installation
parent: Getting Started
nav_order: 1
---

# Installation

## Homebrew (Recommended)

```bash
brew tap quivent/fifth
brew install fifth
```

Installs to `/opt/homebrew/bin/fifth`. Works immediately.

## From Source

```bash
git clone https://github.com/quivent/fifth.git
cd fifth && cd engine && make && cd ..
./fifth install.fs
```

This:
1. Builds the interpreter (~1 second)
2. Copies `fifth` to `/usr/local/bin/`
3. Sets up `~/.fifth/lib/` with core libraries

## Verify

```bash
fifth -e "2 3 + . cr"
# Output: 5
```

## What Gets Installed

| Location | Description |
|----------|-------------|
| `/usr/local/bin/fifth` | 57 KB interpreter |
| `~/.fifth/lib/` | Core libraries |
| `~/.fifth/packages/` | Your packages |
