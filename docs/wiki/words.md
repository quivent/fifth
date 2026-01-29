---
layout: default
title: Words and Definitions
---

# Words and Definitions

Everything in Forth is a **word**. Words are like functions, but simpler.

## Defining Words

```forth
: square ( n -- n² ) dup * ;
```

- `:` starts a definition
- `square` is the name
- `( n -- n² )` is the stack comment (documentation)
- `dup *` is the body
- `;` ends the definition

## Calling Words

Just type the name:

```forth
5 square .    \ 25
```

## Composition

Words compose by stacking:

```forth
: double ( n -- n*2 ) 2 * ;
: quadruple ( n -- n*4 ) double double ;

5 quadruple .    \ 20
```

## Naming Conventions

| Pattern | Meaning | Example |
|---------|---------|---------|
| `<tag>` | Open something | `<div>` |
| `</tag>` | Close something | `</div>` |
| `tag.` | Convenience (open+close) | `p.` |
| `word?` | Returns a flag | `empty?` |
| `word!` | Stores/modifies | `count!` |
| `word@` | Fetches | `count@` |

## Variables and Constants

```forth
variable counter       \ Create variable
0 counter !            \ Store 0
counter @              \ Fetch value
counter @ 1+ counter ! \ Increment

constant PI 3141       \ Create constant (scaled integer)
PI .                   \ 3141
```

## Local Variables (if needed)

```forth
: example { a b -- sum }
  a b + ;
```

But prefer stack manipulation — it's the Forth way.

## Immediate Words

Execute at compile time:

```forth
: [char] ( "c" -- ) char postpone literal ; immediate
```

## Recursion

```forth
: factorial ( n -- n! )
  dup 1 > if
    dup 1- recurse *
  then ;

5 factorial .    \ 120
```

[Back to Wiki](../)
