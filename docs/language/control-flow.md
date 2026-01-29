---
title: Control Flow
parent: Language
nav_order: 3
---

# Control Flow

## IF...THEN

```forth
: check ( n -- )
  0> if ." positive" then ;
```

## IF...ELSE...THEN

```forth
: sign ( n -- )
  dup 0> if drop ." +" else 0< if ." -" else ." 0" then then ;
```

## BEGIN...UNTIL

```forth
: countdown ( n -- )
  begin dup . 1- dup 0= until drop ;

5 countdown    \ 5 4 3 2 1
```

## DO...LOOP

```forth
: stars ( n -- )
  0 do 42 emit loop ;

5 stars    \ *****
```

## Loop Variables

| Word | Description |
|------|-------------|
| `i` | Current index |
| `j` | Outer loop index |
| `leave` | Exit loop early |
