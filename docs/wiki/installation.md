---
layout: default
title: Installation
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

## Manual Install

```bash
git clone https://github.com/quivent/fifth.git
cd fifth
cd engine && make && cd ..
sudo cp engine/fifth /usr/local/bin/
mkdir -p ~/.fifth/lib ~/.fifth/packages
cp -r lib/* ~/.fifth/lib/
```

## Verify

```bash
fifth -e "2 3 + . cr"
# Output: 5
```

## What Gets Installed

```
/usr/local/bin/fifth      # 57 KB interpreter

~/.fifth/                  # Package directory
├── lib/                   # Core libraries
│   ├── str.fs             # String buffers
│   ├── html.fs            # HTML generation
│   ├── sql.fs             # SQLite interface
│   ├── template.fs        # Templates
│   ├── ui.fs              # UI components
│   ├── pkg.fs             # Package system
│   └── core.fs            # Loads all
└── packages/              # Your packages
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `FIFTH_HOME` | `~/.fifth` | Package directory |

[Back to Wiki](../)
