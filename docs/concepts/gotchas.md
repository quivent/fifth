---
title: Gotchas
parent: Concepts
nav_order: 1
---

# Gotchas

Things that will break you.

## 1. Whitespace is Everything

```forth
\ WRONG - one undefined word
</div>nl

\ RIGHT - two words
</div> nl
```

## 2. s" Doesn't Escape

```forth
s" hello\nworld"    \ Literal backslash-n
s\" hello\nworld"   \ Actual newline
```

## 3. Strings Are Transient

```forth
\ WRONG
: get-name s" Alice" ;
get-name type    \ May crash

\ Use strings immediately
```

## 4. Never Use s+

```forth
\ WRONG - crashes
s" Hello " s" World" s+

\ RIGHT - buffer pattern
str-reset s" Hello " str+ s" World" str+ str$
```

## 5. Stack Imbalance = Crash

```forth
\ "Invalid memory address" = stack error
\ Debug with .s
: debug .s drop .s ;
```

## 6. Include vs Require

```forth
\ WRONG - loads twice
include lib.fs
include lib.fs

\ RIGHT
require lib.fs
require lib.fs    \ Ignored
```

## 7. Strings Are Two Items

```forth
\ addr u on stack
s" hello" 2drop    \ Drop both
s" hello" drop     \ WRONG - leaves addr
```
