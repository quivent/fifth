# Forth Reference

A consolidated reference for Forth concepts and Fifth's implementation.

---

## 1. Machine Model

Forth has six concepts. Everything else derives from these.

### Two Stacks

**Data stack**: Where all computation happens. Arguments in, results out. No named parameters.

**Return stack**: Holds subroutine return addresses. Temporary storage via `>R`/`R>`, but must balance within each word.

### Cell Size

One cell = one machine word (8 bytes on 64-bit, 4 on 32-bit). Everything on the stack is one cell. No type tags.

### Dictionary

Linked list of word entries. New definitions added at end, search from newest. Redefining shadows but doesn't replace.

### Linear Memory

`HERE` points to next free byte. `ALLOT` advances it. No `free`. Memory grows forward only.

### Input Stream

Whitespace-delimited tokens. Every token is either a dictionary word or a number.

### State Flag

`STATE` = 0: interpreting (execute immediately). `STATE` != 0: compiling (append to definition).

---

## 2. Execution Cycle

```
LOOP:
  Read next token
  IF found in dictionary:
    IF interpreting OR IMMEDIATE: execute
    ELSE: compile reference
  ELSE IF number:
    IF interpreting: push
    ELSE: compile literal
  ELSE: error
```

---

## 3. Core Operations

### Stack Manipulation

| Word | Effect | Description |
|------|--------|-------------|
| `DUP` | `( x -- x x )` | Duplicate |
| `DROP` | `( x -- )` | Discard |
| `SWAP` | `( x y -- y x )` | Exchange |
| `OVER` | `( x y -- x y x )` | Copy second |
| `ROT` | `( x y z -- y z x )` | Rotate |
| `-ROT` | `( x y z -- z x y )` | Reverse rotate |
| `NIP` | `( x y -- y )` | Drop second |
| `2DUP` | `( x y -- x y x y )` | Duplicate pair |
| `2DROP` | `( x y -- )` | Discard pair |
| `2SWAP` | `( a b c d -- c d a b )` | Exchange pairs |
| `>R` | `( x -- ) R:( -- x )` | To return stack |
| `R>` | `( -- x ) R:( x -- )` | From return stack |
| `2>R` | `( x y -- ) R:( -- x y )` | Pair to return stack |
| `2R>` | `( -- x y ) R:( x y -- )` | Pair from return stack |

### Arithmetic

| Word | Effect | Description |
|------|--------|-------------|
| `+` | `( n1 n2 -- n3 )` | Add |
| `-` | `( n1 n2 -- n3 )` | Subtract |
| `*` | `( n1 n2 -- n3 )` | Multiply |
| `/` | `( n1 n2 -- n3 )` | Divide |
| `MOD` | `( n1 n2 -- rem )` | Remainder |
| `/MOD` | `( n1 n2 -- rem quot )` | Both |
| `NEGATE` | `( n -- -n )` | Negate |
| `ABS` | `( n -- |n| )` | Absolute |
| `MIN` `MAX` | `( n1 n2 -- n )` | Smaller/larger |

### Comparison

All return `TRUE` (-1) or `FALSE` (0).

| Word | Effect | Description |
|------|--------|-------------|
| `=` | `( n1 n2 -- flag )` | Equal |
| `<>` | `( n1 n2 -- flag )` | Not equal |
| `<` `>` | `( n1 n2 -- flag )` | Less/greater (signed) |
| `0=` | `( n -- flag )` | Zero? |
| `0<` `0>` | `( n -- flag )` | Negative/positive? |

### Memory

| Word | Effect | Description |
|------|--------|-------------|
| `@` | `( addr -- x )` | Fetch cell |
| `!` | `( x addr -- )` | Store cell |
| `C@` | `( addr -- c )` | Fetch byte |
| `C!` | `( c addr -- )` | Store byte |
| `+!` | `( n addr -- )` | Add to cell |
| `HERE` | `( -- addr )` | Next free byte |
| `ALLOT` | `( n -- )` | Reserve n bytes |
| `CELLS` | `( n -- bytes )` | Cells to bytes |
| `,` | `( x -- )` | Compile cell at HERE |
| `MOVE` | `( src dst n -- )` | Copy bytes |

### Defining Words

| Word | Effect | Description |
|------|--------|-------------|
| `:` `;` | | Begin/end definition |
| `VARIABLE` | `( "name" -- )` | One-cell variable |
| `CONSTANT` | `( x "name" -- )` | Constant value |
| `CREATE` | `( "name" -- )` | Make entry returning address |
| `DOES>` | | Attach runtime behavior |
| `IMMEDIATE` | | Mark word as compile-time |

### Control Flow

| Structure | Usage |
|-----------|-------|
| `IF ... THEN` | `flag IF ... THEN` |
| `IF ... ELSE ... THEN` | `flag IF ... ELSE ... THEN` |
| `BEGIN ... UNTIL` | `BEGIN ... flag UNTIL` |
| `BEGIN ... WHILE ... REPEAT` | `BEGIN ... flag WHILE ... REPEAT` |
| `?DO ... LOOP` | `limit start ?DO ... LOOP` |
| `CASE ... OF ... ENDOF ... ENDCASE` | Multi-way branch |

### Strings

| Word | Effect | Description |
|------|--------|-------------|
| `S"` | `( -- addr u )` | String literal (transient) |
| `S\"` | `( -- addr u )` | With escapes |
| `."` | | Print literal |
| `TYPE` | `( addr u -- )` | Print string |
| `CR` | | Newline |
| `EMIT` | `( c -- )` | Print char |
| `/STRING` | `( addr u n -- addr' u' )` | Advance by n |

### File I/O

| Word | Effect | Description |
|------|--------|-------------|
| `OPEN-FILE` | `( addr u mode -- fid ior )` | Open |
| `CREATE-FILE` | `( addr u mode -- fid ior )` | Create/truncate |
| `CLOSE-FILE` | `( fid -- ior )` | Close |
| `WRITE-FILE` | `( addr u fid -- ior )` | Write |
| `READ-LINE` | `( addr u fid -- u2 flag ior )` | Read line |
| `R/O` `W/O` | `( -- mode )` | Read/write only |
| `THROW` | `( n -- )` | Throw if nonzero |

---

## 4. Fifth Interpreter Words

The Fifth C interpreter provides these commonly-used words beyond ANS Core:

| Word | Purpose | Fifth Usage |
|------|---------|-------------|
| `S\"` | Escaped strings (`\"`, `\n`, `\t`) | HTML onclick attributes |
| `EMIT-FILE` | `( c fid -- ior )` Write char to file | Newlines in HTML output |
| `STDOUT` | `( -- fid )` Standard output fd | Debug output |
| `SLURP-FILE` | `( addr u -- addr2 u2 )` Read entire file | Template capture |
| `REQUIRE` | Load file once | All library imports |
| `GETENV` | `( addr u -- addr' u' )` Get env var | Package paths |
| `PARSE-NAME` | `( -- addr u )` Parse next word | Package system |
| `SYSTEM` | `( addr u -- )` Shell command | SQLite, open |

These are implemented in the Fifth interpreter (`engine/`).

---

## 5. Fifth Word Categories

### By Category

| Category | ANS Core | ANS Ext | Fifth-specific | Total |
|----------|----------|---------|----------------|-------|
| Stack | 10 | 4 | 0 | 14 |
| Arithmetic | 7 | 3 | 0 | 10 |
| Memory | 10 | 0 | 0 | 10 |
| Control | 10 | 4 | 0 | 14 |
| Strings | 5 | 0 | 2 | 7 |
| File I/O | 8 | 0 | 3 | 11 |
| Defining | 6 | 0 | 0 | 6 |
| System | 0 | 0 | 3 | 3 |
| **Total** | **~56** | **~11** | **8** | **~75** |

### Interpreter Implementation

Fifth's C interpreter implements ANS Core words plus commonly-used extensions. The `SYSTEM` word (for shell-out to sqlite3) is essential for database access.

---

## 6. Practical Reference

### Building Fifth

```bash
cd ~/fifth/engine
make
```

### Running

```bash
./fifth program.fs          # Execute file
./fifth -e "2 3 + . bye"    # One-liner
./fifth                      # Interactive REPL
```

### Debugging

| Tool | Purpose |
|------|---------|
| `.S` | Show stack without consuming |
| `SEE word` | Decompile word |
| `WORDS` | List all words |

### Common Errors

| Message | Cause |
|---------|-------|
| `Undefined word` | Typo or missing space (`</div>nl` vs `</div> nl`) |
| `Stack underflow` | Consumed value not present |
| `Invalid memory address` | Stack imbalance, bad pointer |
| `word is redefined` | Used `INCLUDE` not `REQUIRE` |

### S" vs S\"

Use `S"` by default. Use `S\"` only when you need:
- Embedded quotes: `S\" he said \"hi\""`
- Escape sequences: `S\" line1\nline2"`

---

## 7. CREATE/DOES> Pattern

The defining-word mechanism for creating words that create words:

```forth
\ How CONSTANT works:
: CONSTANT  ( x "name" -- )
    CREATE ,       \ Create entry, store x
    DOES> @        \ Runtime: fetch stored value
;

42 CONSTANT answer
answer .           \ 42

\ How ARRAY works:
: ARRAY  ( n "name" -- )
    CREATE CELLS ALLOT
    DOES> SWAP CELLS +
;

10 ARRAY data
42 3 data !        \ Store at index 3
3 data @ .         \ 42
```

---

## 8. Fifth Buffer Pattern

Fifth avoids dynamic allocation. Use static buffers:

```forth
1024 CONSTANT buf-max
CREATE buf buf-max ALLOT
VARIABLE buf-len

: buf-reset  0 buf-len ! ;
: buf+ ( addr u -- )
    buf buf-len @ + SWAP DUP buf-len +! MOVE ;
: buf$ ( -- addr u )
    buf buf-len @ ;
```

Two buffers prevent conflicts:
- Primary (`str-buf`): General string building
- Secondary (`str2-buf`): Used by `html-escape`

---

*Fifth Forth Language Reference*
*~300 lines*
