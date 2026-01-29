---
layout: default
title: Quick Start
---

# Quick Start

## Hello World

```bash
fifth -e ': hello ." Hello, World!" cr ; hello'
```

## Interactive REPL

```bash
fifth
2 3 + .          \ prints 5
: square dup * ;
5 square .       \ prints 25
bye
```

## Run a File

```bash
fifth examples/project-dashboard.fs
```

[Back to Wiki](../)
