---
layout: default
title: Control Flow
---

# Control Flow

## Conditionals

### IF...THEN

```forth
: check ( n -- )
  0> if
    ." positive"
  then ;

5 check    \ positive
-3 check   \ (nothing)
```

### IF...ELSE...THEN

```forth
: sign ( n -- )
  dup 0> if
    drop ." positive"
  else
    0< if ." negative" else ." zero" then
  then ;
```

## Loops

### BEGIN...UNTIL

Repeat until condition is true:

```forth
: countdown ( n -- )
  begin
    dup . 1-
    dup 0=
  until drop ;

5 countdown    \ 5 4 3 2 1
```

### BEGIN...WHILE...REPEAT

Repeat while condition is true:

```forth
: countdown ( n -- )
  begin
    dup 0>
  while
    dup . 1-
  repeat drop ;
```

### DO...LOOP

Counted loop:

```forth
: stars ( n -- )
  0 do
    42 emit    \ '*' = ASCII 42
  loop ;

5 stars    \ *****
```

### DO...+LOOP

Custom increment:

```forth
: evens ( -- )
  10 0 do
    i .
  2 +loop ;

evens    \ 0 2 4 6 8
```

### Loop Variables

| Word | Description |
|------|-------------|
| `i` | Current index |
| `j` | Outer loop index |
| `leave` | Exit loop early |
| `unloop` | Clean up before `exit` |

## CASE

```forth
: day ( n -- )
  case
    1 of ." Monday" endof
    2 of ." Tuesday" endof
    3 of ." Wednesday" endof
    ." Unknown"
  endcase ;
```

[Back to Wiki](../)
