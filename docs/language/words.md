---
title: Words
parent: Language
nav_order: 2
---

# Words and Definitions

Everything in Forth is a word.

## Defining

```forth
: square ( n -- n² ) dup * ;
```

- `:` starts definition
- `square` is the name
- `( n -- n² )` is the stack comment
- `dup *` is the body
- `;` ends definition

## Composition

```forth
: double ( n -- n*2 ) 2 * ;
: quadruple ( n -- n*4 ) double double ;

5 quadruple .    \ 20
```

## Naming Conventions

| Pattern | Meaning | Example |
|---------|---------|---------|
| `<tag>` | Open | `<div>` |
| `</tag>` | Close | `</div>` |
| `word.` | Convenience | `p.` |
| `word?` | Returns flag | `empty?` |
| `word!` | Stores | `count!` |
| `word@` | Fetches | `count@` |
