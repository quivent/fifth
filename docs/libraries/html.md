---
title: html.fs
parent: Libraries
nav_order: 2
---

# html.fs — HTML Generation

Type-safe HTML with automatic escaping.

## Usage

```forth
use lib:html.fs

<div>
  s" Hello" text
</div>
```

## Escaping

| Word | Description |
|------|-------------|
| `text` | Escape and output (safe) |
| `raw` | Output as-is (trusted only) |

```forth
s" <script>alert('xss')</script>" text
\ Output: &lt;script&gt;...
```

**Never use `raw` for user data.**

## Convenience Words

```forth
s" Title" h1.     \ <h1>Title</h1>
s" Text" p.       \ <p>Text</p>
```

## Document Structure

```forth
s" /tmp/page.html" w/o create-file throw html>file
s" Title" html-head html-body
  s" Hello" h1.
html-end
html-fid @ close-file throw
```
