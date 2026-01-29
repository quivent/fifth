---
layout: default
title: html.fs - HTML Generation
---

# html.fs — HTML Generation

Type-safe HTML with automatic escaping.

```forth
require ~/.fifth/lib/pkg.fs
use lib:html.fs
```

## Basic Tags

```forth
<div>
  s" Hello, World!" text
</div>
```

Output: `<div>Hello, World!</div>`

## Escaping

| Word | Description |
|------|-------------|
| `text` | HTML-escape and output (safe for user data) |
| `raw` | Output without escaping (for trusted HTML) |

```forth
s" <script>alert('xss')</script>" text
\ Output: &lt;script&gt;alert('xss')&lt;/script&gt;
```

**Rule: Never use `raw` for user data.**

## Convenience Words

```forth
s" Hello" h1.        \ <h1>Hello</h1>
s" World" p.         \ <p>World</p>
s" Click" button.    \ <button>Click</button>
```

## Attributes

```forth
<div> s" class" s" container" attr>
  s" Content" text
</div>
\ <div class="container">Content</div>
```

## Document Structure

```forth
s" /tmp/page.html" w/o create-file throw html>file

s" Page Title" html-head
  <style>
    s" body { font-family: sans-serif; }" raw
  </style>
html-body
  s" Hello" h1.
html-end

html-fid @ close-file throw
```

## Available Tags

All standard HTML5 tags:

```
<html> <head> <body> <div> <span> <p> <a>
<h1> <h2> <h3> <h4> <h5> <h6>
<ul> <ol> <li> <table> <tr> <th> <td>
<form> <input> <button> <select> <option>
<header> <footer> <nav> <main> <section> <article>
<strong> <em> <code> <pre> <blockquote>
```

Each has `<tag>` opener and `</tag>` closer.

[Back to Wiki](../)
