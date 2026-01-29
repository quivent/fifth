---
title: Home
layout: home
nav_order: 1
---

<div class="hero" style="text-align: center; padding: 4rem 0;">
  <p style="display: inline-flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; background: #0f0f0f; border: 1px solid #262626; border-radius: 100px; font-size: 0.8125rem; color: #a3a3a3; margin-bottom: 2rem;">
    <span style="width: 6px; height: 6px; background: #6366f1; border-radius: 50%;"></span>
    A Forth for the Agentic Era
  </p>
</div>

# Fifth

> *"I think the industry is fundamentally unable to appreciate simplicity."*
> — Chuck Moore, creator of Forth

Fifth is a self-contained Forth ecosystem designed for AI-assisted development. One binary, zero dependencies, instant startup.

---

<div style="display: flex; justify-content: center; gap: 4rem; padding: 2rem 0; border-top: 1px solid #262626; border-bottom: 1px solid #262626; margin: 2rem 0;">
  <div style="text-align: center;">
    <div style="font-size: 2rem; font-weight: 600; font-family: 'JetBrains Mono', monospace; letter-spacing: -0.02em;">57KB</div>
    <div style="font-size: 0.75rem; color: #737373; text-transform: uppercase; letter-spacing: 0.05em;">Binary Size</div>
  </div>
  <div style="text-align: center;">
    <div style="font-size: 2rem; font-weight: 600; font-family: 'JetBrains Mono', monospace; letter-spacing: -0.02em;">75</div>
    <div style="font-size: 0.75rem; color: #737373; text-transform: uppercase; letter-spacing: 0.05em;">Core Words</div>
  </div>
  <div style="text-align: center;">
    <div style="font-size: 2rem; font-weight: 600; font-family: 'JetBrains Mono', monospace; letter-spacing: -0.02em;">0</div>
    <div style="font-size: 0.75rem; color: #737373; text-transform: uppercase; letter-spacing: 0.05em;">Dependencies</div>
  </div>
</div>

---

## Quick Install

```bash
brew tap quivent/fifth
brew install fifth
```

Or [build from source](getting-started/installation).

---

## Why Fifth?

| Challenge for LLMs | Traditional Languages | Fifth |
|-------------------|----------------------|-------|
| Implicit state | Variables everywhere | One explicit stack |
| Large API | Thousands of methods | 75 core words |
| Complex control | Callbacks, promises | Linear execution |
| Hidden effects | Mutations, getters | Stack effects documented |

Stack effects are contracts. Either the stack is correct and the code works, or it's wrong and you get an immediate crash. No hidden state. No surprises.

[Learn more about Fifth for LLMs →](concepts/agentic)

---

## Hello World

```forth
: hello  ( -- )
  ." Hello, Fifth!" cr ;

hello
```

Run it:

```bash
fifth -e ': hello ." Hello, Fifth!" cr ; hello'
```

---

## Native I/O — No Shell, No Fork

Fifth talks directly to the OS. No subprocess spawning.

```forth
\ Other languages: fork → exec → /usr/bin/open → LaunchServices → browser
s" open /tmp/page.html" system        \ 182ms

\ Fifth: C → LaunchServices → browser
s" /tmp/page.html" open-path          \  56ms — 3.2x faster
```

`open-path` calls macOS `LSOpenCFURLRef` from C. Same API that `/usr/bin/open` calls, minus 126ms of process overhead. A 57KB binary with native OS integration.

---

## What's Included

- **Interpreter** — Fast C-based Forth engine, 2ms startup
- **Native I/O** — Direct OS calls, no subprocess overhead
- **Package System** — `use lib:` and `use pkg:` for libraries
- **HTML Generation** — Type-safe templates with auto-escaping
- **SQLite Interface** — Query databases via shell-out pattern
- **String Buffers** — Safe string manipulation without allocation

[Get Started →](getting-started/quickstart)
