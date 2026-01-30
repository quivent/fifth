\ tf.fs - Fifth to x86_64 ELF Compiler
\ Compiles Fifth source directly to Linux ELF binary. No C, no cc.

\ === Memory layout ===
\ code-buf: generated machine code
\ Binary output written to file

create code-buf 65536 allot
variable code-pos  0 code-pos !
variable entry-pos 0 entry-pos !

\ === Byte emission ===
: emit-byte ( b -- )
  code-buf code-pos @ + c!
  1 code-pos +! ;

: emit-word ( w -- )
  dup emit-byte 8 rshift emit-byte ;

: emit-dword ( d -- )
  dup emit-word 16 rshift emit-word ;

: emit-qword ( q -- )
  dup emit-dword 32 rshift emit-dword ;

: code-here ( -- addr )
  code-buf code-pos @ + ;

: patch-dword ( val addr -- )
  dup 3 + swap do
    dup i c!
    8 rshift
  loop drop ;

\ === x86_64 instruction encoding ===
\ Stack pointer: r15 (data stack)
\ TOS cache: rax when beneficial
\ Memory stack at runtime

\ Push immediate to stack: sub r15,8; mov [r15],imm
: emit-push-imm ( n -- )
  $49 emit-byte $83 emit-byte $ef emit-byte $08 emit-byte  \ sub r15,8
  $49 emit-byte $c7 emit-byte $07 emit-byte                 \ mov qword [r15], imm32
  emit-dword ;

\ Push 64-bit immediate (for large numbers)
: emit-push-imm64 ( n -- )
  $49 emit-byte $83 emit-byte $ef emit-byte $08 emit-byte  \ sub r15,8
  $48 emit-byte $b8 emit-byte                               \ mov rax, imm64
  emit-qword
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15], rax

: emit-push ( n -- )
  dup $7fffffff > over $-80000000 < or if
    emit-push-imm64
  else
    emit-push-imm
  then ;

\ dup: mov rax,[r15]; sub r15,8; mov [r15],rax
: emit-dup ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $ef emit-byte $08 emit-byte  \ sub r15,8
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ drop: add r15,8
: emit-drop ( -- )
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte ; \ add r15,8

\ swap: mov rax,[r15]; mov rcx,[r15+8]; mov [r15],rcx; mov [r15+8],rax
: emit-swap ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $8b emit-byte $4f emit-byte $08 emit-byte  \ mov rcx,[r15+8]
  $49 emit-byte $89 emit-byte $0f emit-byte                 \ mov [r15],rcx
  $49 emit-byte $89 emit-byte $47 emit-byte $08 emit-byte ; \ mov [r15+8],rax

\ over: mov rax,[r15+8]; sub r15,8; mov [r15],rax
: emit-over ( -- )
  $49 emit-byte $8b emit-byte $47 emit-byte $08 emit-byte  \ mov rax,[r15+8]
  $49 emit-byte $83 emit-byte $ef emit-byte $08 emit-byte  \ sub r15,8
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ +: mov rax,[r15]; add r15,8; add [r15],rax
: emit-add ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $49 emit-byte $01 emit-byte $07 emit-byte ;              \ add [r15],rax

\ -: mov rax,[r15]; add r15,8; sub [r15],rax
: emit-sub ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $49 emit-byte $29 emit-byte $07 emit-byte ;              \ sub [r15],rax

\ *: mov rax,[r15]; add r15,8; imul rax,[r15]; mov [r15],rax
: emit-mul ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $49 emit-byte $0f emit-byte $af emit-byte $07 emit-byte  \ imul rax,[r15]
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ /: mov rcx,[r15]; mov rax,[r15+8]; add r15,8; cqo; idiv rcx; mov [r15],rax
: emit-div ( -- )
  $49 emit-byte $8b emit-byte $0f emit-byte                 \ mov rcx,[r15]
  $49 emit-byte $8b emit-byte $47 emit-byte $08 emit-byte  \ mov rax,[r15+8]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $48 emit-byte $99 emit-byte                               \ cqo
  $48 emit-byte $f7 emit-byte $f9 emit-byte                 \ idiv rcx
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ mod: same as / but store rdx
: emit-mod ( -- )
  $49 emit-byte $8b emit-byte $0f emit-byte                 \ mov rcx,[r15]
  $49 emit-byte $8b emit-byte $47 emit-byte $08 emit-byte  \ mov rax,[r15+8]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $48 emit-byte $99 emit-byte                               \ cqo
  $48 emit-byte $f7 emit-byte $f9 emit-byte                 \ idiv rcx
  $49 emit-byte $89 emit-byte $17 emit-byte ;              \ mov [r15],rdx

\ negate: neg qword [r15]
: emit-negate ( -- )
  $49 emit-byte $f7 emit-byte $1f emit-byte ;              \ neg qword [r15]

\ and: mov rax,[r15]; add r15,8; and [r15],rax
: emit-and ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte
  $49 emit-byte $21 emit-byte $07 emit-byte ;

\ or: mov rax,[r15]; add r15,8; or [r15],rax
: emit-or ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte
  $49 emit-byte $09 emit-byte $07 emit-byte ;

\ xor: mov rax,[r15]; add r15,8; xor [r15],rax
: emit-xor ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte
  $49 emit-byte $31 emit-byte $07 emit-byte ;

\ invert: not qword [r15]
: emit-invert ( -- )
  $49 emit-byte $f7 emit-byte $17 emit-byte ;              \ not qword [r15]

\ =: compare, set -1 or 0
: emit-eq ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $49 emit-byte $3b emit-byte $07 emit-byte                 \ cmp rax,[r15]
  $0f emit-byte $94 emit-byte $c0 emit-byte                 \ sete al
  $48 emit-byte $0f emit-byte $b6 emit-byte $c0 emit-byte  \ movzx rax,al
  $48 emit-byte $f7 emit-byte $d8 emit-byte                 \ neg rax
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ <: signed less than
: emit-lt ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $49 emit-byte $39 emit-byte $07 emit-byte                 \ cmp [r15],rax
  $0f emit-byte $9c emit-byte $c0 emit-byte                 \ setl al
  $48 emit-byte $0f emit-byte $b6 emit-byte $c0 emit-byte  \ movzx rax,al
  $48 emit-byte $f7 emit-byte $d8 emit-byte                 \ neg rax
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ >: signed greater than
: emit-gt ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte
  $49 emit-byte $39 emit-byte $07 emit-byte                 \ cmp [r15],rax
  $0f emit-byte $9f emit-byte $c0 emit-byte                 \ setg al
  $48 emit-byte $0f emit-byte $b6 emit-byte $c0 emit-byte
  $48 emit-byte $f7 emit-byte $d8 emit-byte
  $49 emit-byte $89 emit-byte $07 emit-byte ;

\ 0=: test [r15], set flag
: emit-0eq ( -- )
  $49 emit-byte $83 emit-byte $3f emit-byte $00 emit-byte  \ cmp qword [r15],0
  $0f emit-byte $94 emit-byte $c0 emit-byte                 \ sete al
  $48 emit-byte $0f emit-byte $b6 emit-byte $c0 emit-byte  \ movzx rax,al
  $48 emit-byte $f7 emit-byte $d8 emit-byte                 \ neg rax
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ @: mov rax,[r15]; mov rax,[rax]; mov [r15],rax
: emit-fetch ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $48 emit-byte $8b emit-byte $00 emit-byte                 \ mov rax,[rax]
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ !: mov rax,[r15]; mov rcx,[r15+8]; mov [rax],rcx; add r15,16
: emit-store ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $8b emit-byte $4f emit-byte $08 emit-byte  \ mov rcx,[r15+8]
  $48 emit-byte $89 emit-byte $08 emit-byte                 \ mov [rax],rcx
  $49 emit-byte $83 emit-byte $c7 emit-byte $10 emit-byte ; \ add r15,16

\ c@: mov rax,[r15]; movzx rax,byte [rax]; mov [r15],rax
: emit-cfetch ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $48 emit-byte $0f emit-byte $b6 emit-byte $00 emit-byte  \ movzx rax,byte [rax]
  $49 emit-byte $89 emit-byte $07 emit-byte ;              \ mov [r15],rax

\ c!: mov rax,[r15]; mov cl,[r15+8]; mov [rax],cl; add r15,16
: emit-cstore ( -- )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $8a emit-byte $4f emit-byte $08 emit-byte  \ mov cl,[r15+8]
  $88 emit-byte $08 emit-byte                               \ mov [rax],cl
  $49 emit-byte $83 emit-byte $c7 emit-byte $10 emit-byte ; \ add r15,16

\ emit (putchar): mov rax,1; mov rdi,1; mov rsi,r15; mov rdx,1; syscall; add r15,8
: emit-emit ( -- )
  $b8 emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte  \ mov eax,1
  $bf emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte  \ mov edi,1
  $4c emit-byte $89 emit-byte $fe emit-byte                               \ mov rsi,r15
  $ba emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte  \ mov edx,1
  $0f emit-byte $05 emit-byte                                             \ syscall
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte ;              \ add r15,8

\ cr: push 10, emit
: emit-cr ( -- )
  10 emit-push
  emit-emit ;

\ . (print number): complex - call helper or inline
\ For now, simple version that prints single digit (placeholder)
: emit-dot ( -- )
  \ TODO: full number printing
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $48 emit-byte $83 emit-byte $c0 emit-byte $30 emit-byte  \ add rax,'0'
  $50 emit-byte                                             \ push rax
  $b8 emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $bf emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $48 emit-byte $89 emit-byte $e6 emit-byte                 \ mov rsi,rsp
  $ba emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $0f emit-byte $05 emit-byte
  $58 emit-byte                                             \ pop rax
  \ space
  $6a emit-byte $20 emit-byte                               \ push ' '
  $b8 emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $bf emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $48 emit-byte $89 emit-byte $e6 emit-byte
  $ba emit-byte $01 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte
  $0f emit-byte $05 emit-byte
  $58 emit-byte ;

\ Call (for word references): call rel32
: emit-call ( addr -- )
  $e8 emit-byte
  code-pos @ 4 + -  \ relative offset
  emit-dword ;

\ Return: ret
: emit-ret ( -- )
  $c3 emit-byte ;

\ Unconditional jump: jmp rel32
: emit-jmp ( -- fixup-addr )
  $e9 emit-byte
  code-here
  0 emit-dword ;

: patch-jmp ( fixup-addr -- )
  code-pos @ over 4 + - swap patch-dword ;

\ Conditional jump (if): pop, test, jz rel32
: emit-0branch ( -- fixup-addr )
  $49 emit-byte $8b emit-byte $07 emit-byte                 \ mov rax,[r15]
  $49 emit-byte $83 emit-byte $c7 emit-byte $08 emit-byte  \ add r15,8
  $48 emit-byte $85 emit-byte $c0 emit-byte                 \ test rax,rax
  $0f emit-byte $84 emit-byte                               \ jz rel32
  code-here
  0 emit-dword ;

\ === ELF Generation ===
create elf-buf 65536 allot
variable elf-pos

: e! ( b -- ) elf-buf elf-pos @ + c!  1 elf-pos +! ;
: e2! ( w -- ) dup e! 8 rshift e! ;
: e4! ( d -- ) dup e2! 16 rshift e2! ;
: e8! ( q -- ) dup e4! 32 rshift e4! ;

: emit-elf-header ( entry code-size -- )
  0 elf-pos !
  \ ELF magic
  $7f e! [char] E e! [char] L e! [char] F e!
  \ Class (64-bit), endian (little), version, OS/ABI, padding
  2 e! 1 e! 1 e! 0 e!  0 e8!
  \ Type (executable), machine (x86_64)
  2 e2!  $3e e2!
  \ Version
  1 e4!
  \ Entry point (virtual address)
  $401000 + e8!
  \ Program header offset (immediately after ELF header = 64)
  64 e8!
  \ Section header offset (none)
  0 e8!
  \ Flags
  0 e4!
  \ ELF header size
  64 e2!
  \ Program header entry size
  56 e2!
  \ Program header count
  2 e2!
  \ Section header entry size
  0 e2!
  \ Section header count
  0 e2!
  \ Section name string table index
  0 e2!

  \ Program header 1: loadable segment for code
  \ p_type = PT_LOAD
  1 e4!
  \ p_flags = PF_R | PF_X
  5 e4!
  \ p_offset
  0 e8!
  \ p_vaddr
  $400000 e8!
  \ p_paddr
  $400000 e8!
  \ p_filesz (header + code)
  176 ( over + ) e8!
  drop \ code-size
  \ p_memsz
  176 e8!
  \ p_align
  $1000 e8!

  \ Program header 2: loadable segment for stack/data
  1 e4!  6 e4!  \ PT_LOAD, PF_R | PF_W
  0 e8!  $600000 e8!  $600000 e8!
  0 e8!  $10000 e8!   \ 64KB for stack
  $1000 e8! ;

\ === Word Dictionary ===
\ Simple linear list: name-len, name, code-offset

create words-buf 4096 allot
variable words-pos  0 words-pos !

: add-word ( addr u code-offset -- )
  >r
  words-buf words-pos @ + >r
  dup r@ c!  1 r> + >r       \ store length
  r@ swap cmove              \ store name
  dup r> + >r
  r> r> swap !               \ store offset at aligned pos (simplified)
  words-pos @ + 1+ 8 + words-pos ! ;

: find-word ( addr u -- code-offset | -1 )
  words-buf words-pos @ bounds ?do
    i c@ over = if
      i 1+ over 2 pick compare 0= if
        2drop
        i i c@ + 1+ @
        unloop exit
      then
    then
    i c@ 1+ 8 +
  +loop
  2drop -1 ;

\ === Control Flow Stack ===
create cf-stack 256 cells allot
variable cf-sp  0 cf-sp !

: cf-push ( x -- ) cf-stack cf-sp @ cells + !  1 cf-sp +! ;
: cf-pop ( -- x )  -1 cf-sp +!  cf-stack cf-sp @ cells + @ ;

\ === Compiler ===
variable compiling  0 compiling !
variable last-word  0 last-word !

: compile-word ( addr u -- )
  2dup s" +" compare 0= if 2drop emit-add exit then
  2dup s" -" compare 0= if 2drop emit-sub exit then
  2dup s" *" compare 0= if 2drop emit-mul exit then
  2dup s" /" compare 0= if 2drop emit-div exit then
  2dup s" mod" compare 0= if 2drop emit-mod exit then
  2dup s" dup" compare 0= if 2drop emit-dup exit then
  2dup s" drop" compare 0= if 2drop emit-drop exit then
  2dup s" swap" compare 0= if 2drop emit-swap exit then
  2dup s" over" compare 0= if 2drop emit-over exit then
  2dup s" negate" compare 0= if 2drop emit-negate exit then
  2dup s" and" compare 0= if 2drop emit-and exit then
  2dup s" or" compare 0= if 2drop emit-or exit then
  2dup s" xor" compare 0= if 2drop emit-xor exit then
  2dup s" invert" compare 0= if 2drop emit-invert exit then
  2dup s" =" compare 0= if 2drop emit-eq exit then
  2dup s" <" compare 0= if 2drop emit-lt exit then
  2dup s" >" compare 0= if 2drop emit-gt exit then
  2dup s" 0=" compare 0= if 2drop emit-0eq exit then
  2dup s" @" compare 0= if 2drop emit-fetch exit then
  2dup s" !" compare 0= if 2drop emit-store exit then
  2dup s" c@" compare 0= if 2drop emit-cfetch exit then
  2dup s" c!" compare 0= if 2drop emit-cstore exit then
  2dup s" emit" compare 0= if 2drop emit-emit exit then
  2dup s" cr" compare 0= if 2drop emit-cr exit then
  2dup s" ." compare 0= if 2drop emit-dot exit then
  \ Check if it's a known word
  2dup find-word dup -1 <> if
    -rot 2drop
    $401000 + emit-call exit
  then
  drop
  \ Unknown word - error
  ." Unknown word: " type cr ;

: compile-number ( addr u -- )
  0 0 2swap >number 2drop drop
  emit-push ;

: number? ( addr u -- flag )
  over c@ dup [char] - = swap [char] 0 [char] 9 1+ within or
  swap 1 > and ;

: process-token ( addr u -- )
  2dup s" :" compare 0= if
    2drop
    1 compiling !
    \ Next token is the word name - will be handled by main loop
    exit
  then
  2dup s" ;" compare 0= if
    2drop
    emit-ret
    0 compiling !
    exit
  then
  2dup s" if" compare 0= if
    2drop
    emit-0branch cf-push
    exit
  then
  2dup s" else" compare 0= if
    2drop
    emit-jmp
    cf-pop patch-jmp
    cf-push
    exit
  then
  2dup s" then" compare 0= if
    2drop
    cf-pop patch-jmp
    exit
  then
  2dup number? if
    compile-number
  else
    compile-word
  then ;

\ === Startup/Exit Code ===
: emit-startup ( -- )
  \ Set up r15 as stack pointer
  \ mov r15, 0x610000 (top of data segment)
  $49 emit-byte $bf emit-byte
  $00 emit-byte $00 emit-byte $61 emit-byte $00 emit-byte
  $00 emit-byte $00 emit-byte $00 emit-byte $00 emit-byte ;

: emit-exit ( -- )
  \ mov rax, 60; xor rdi,rdi; syscall
  $b8 emit-byte 60 emit-dword
  $48 emit-byte $31 emit-byte $ff emit-byte
  $0f emit-byte $05 emit-byte ;

\ === Main Compiler ===
: compile-file ( addr u -- )
  r/o open-file throw >r
  begin
    pad 256 r@ read-line throw
  while
    pad swap
    \ Parse tokens
    begin
      dup 0> while
      over c@ bl <= if 1 /string else
        2dup bl scan
        2>r 2r@ nip - >r over r>
        \ Got token (addr u)
        compiling @ 1 = last-word @ 0= and if
          \ This is the word name
          2dup code-pos @ add-word
          code-pos @ last-word !
        else
          process-token
        then
        2r>
      then
    repeat 2drop
  repeat drop
  r> close-file drop ;

: write-binary ( filename-addr filename-u -- )
  w/o create-file throw >r
  \ Write ELF header with code
  entry-pos @ code-pos @
  emit-elf-header
  elf-buf 176 r@ write-file throw
  \ Write code
  code-buf code-pos @ r@ write-file throw
  r> close-file throw ;

\ === Entry Point ===
: main ( -- )
  argc 2 < if
    ." tf - Fifth to x86_64 ELF compiler" cr
    ." Usage: fifth tf.fs input.fs" cr
    ." Output: a.out" cr
    bye
  then

  \ Reset
  0 code-pos !
  0 words-pos !
  0 cf-sp !
  0 compiling !
  0 last-word !

  \ Emit startup code
  emit-startup
  code-pos @ entry-pos !

  \ Compile input file
  1 argv compile-file

  \ Emit exit
  emit-exit

  \ Write output
  s" a.out" write-binary

  \ Make executable
  s" chmod +x a.out" system drop

  ." Compiled to a.out" cr ;

main
bye
