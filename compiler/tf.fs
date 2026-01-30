\ tf.fs - Fifth to C Compiler
\ Source-to-source translator. Zero C dependencies unless I/O used.

variable uses-io  0 uses-io !
variable in-def   0 in-def !
variable label-n  0 label-n !
create entry-name 64 allot  0 entry-name c!

\ === I/O Word Detection ===
: io-word? ( addr u -- flag )
  2dup s" ." compare 0= if 2drop -1 exit then
  2dup s" cr" compare 0= if 2drop -1 exit then
  2dup s" emit" compare 0= if 2drop -1 exit then
  2dup s" space" compare 0= if 2drop -1 exit then
  2dup s" spaces" compare 0= if 2drop -1 exit then
  2dup s" type" compare 0= if 2drop -1 exit then
  2dup s" .s" compare 0= if 2drop -1 exit then
  2dup s" .\"" compare 0= if 2drop -1 exit then
  2drop 0 ;

\ === Emit C for a word ===
: emit-word ( addr u -- )
  \ Check for I/O
  2dup io-word? if -1 uses-io ! then
  \ Primitives
  2dup s" +" compare 0= if 2drop s"   f_add();" type cr exit then
  2dup s" -" compare 0= if 2drop s"   f_sub();" type cr exit then
  2dup s" *" compare 0= if 2drop s"   f_mul();" type cr exit then
  2dup s" /" compare 0= if 2drop s"   f_div();" type cr exit then
  2dup s" mod" compare 0= if 2drop s"   f_mod();" type cr exit then
  2dup s" dup" compare 0= if 2drop s"   f_dup();" type cr exit then
  2dup s" drop" compare 0= if 2drop s"   f_drop();" type cr exit then
  2dup s" swap" compare 0= if 2drop s"   f_swap();" type cr exit then
  2dup s" over" compare 0= if 2drop s"   f_over();" type cr exit then
  2dup s" rot" compare 0= if 2drop s"   f_rot();" type cr exit then
  2dup s" nip" compare 0= if 2drop s"   f_nip();" type cr exit then
  2dup s" tuck" compare 0= if 2drop s"   f_tuck();" type cr exit then
  2dup s" 2dup" compare 0= if 2drop s"   f_2dup();" type cr exit then
  2dup s" 2drop" compare 0= if 2drop s"   f_2drop();" type cr exit then
  2dup s" negate" compare 0= if 2drop s"   f_neg();" type cr exit then
  2dup s" abs" compare 0= if 2drop s"   f_abs();" type cr exit then
  2dup s" 1+" compare 0= if 2drop s"   TOS++;" type cr exit then
  2dup s" 1-" compare 0= if 2drop s"   TOS--;" type cr exit then
  2dup s" 2*" compare 0= if 2drop s"   TOS<<=1;" type cr exit then
  2dup s" 2/" compare 0= if 2drop s"   TOS>>=1;" type cr exit then
  2dup s" and" compare 0= if 2drop s"   f_and();" type cr exit then
  2dup s" or" compare 0= if 2drop s"   f_or();" type cr exit then
  2dup s" xor" compare 0= if 2drop s"   f_xor();" type cr exit then
  2dup s" invert" compare 0= if 2drop s"   TOS=~TOS;" type cr exit then
  2dup s" =" compare 0= if 2drop s"   f_eq();" type cr exit then
  2dup s" <" compare 0= if 2drop s"   f_lt();" type cr exit then
  2dup s" >" compare 0= if 2drop s"   f_gt();" type cr exit then
  2dup s" 0=" compare 0= if 2drop s"   TOS=TOS?0:-1;" type cr exit then
  2dup s" 0<" compare 0= if 2drop s"   TOS=TOS<0?-1:0;" type cr exit then
  2dup s" 0>" compare 0= if 2drop s"   TOS=TOS>0?-1:0;" type cr exit then
  2dup s" @" compare 0= if 2drop s"   TOS=*(cell_t*)TOS;" type cr exit then
  2dup s" !" compare 0= if 2drop s"   *(cell_t*)TOS=NOS;sp+=2;" type cr exit then
  2dup s" c@" compare 0= if 2drop s"   TOS=*(char*)TOS;" type cr exit then
  2dup s" c!" compare 0= if 2drop s"   *(char*)TOS=NOS;sp+=2;" type cr exit then
  2dup s" >r" compare 0= if 2drop s"   *--rsp=*sp++;" type cr exit then
  2dup s" r>" compare 0= if 2drop s"   *--sp=*rsp++;" type cr exit then
  2dup s" r@" compare 0= if 2drop s"   *--sp=*rsp;" type cr exit then
  2dup s" ." compare 0= if 2drop s"   f_dot();" type cr exit then
  2dup s" cr" compare 0= if 2drop s"   f_cr();" type cr exit then
  2dup s" emit" compare 0= if 2drop s"   f_emit();" type cr exit then
  2dup s" space" compare 0= if 2drop s"   f_space();" type cr exit then
  \ User word - emit as function call
  s"   w_" type type s" ();" type cr ;

\ === Scan pass - detect I/O usage ===
: scan-file ( addr u -- )
  r/o open-file throw >r
  begin
    pad 256 r@ read-line throw
  while
    pad swap
    begin
      dup 0> while
      over c@ bl <= if 1 /string else
        2dup bl scan 2>r  \ find end of word
        2r@ nip - >r over r>  \ get word
        io-word? if -1 uses-io ! then
        2r>
      then
    repeat 2drop
  repeat drop
  r> close-file drop ;

\ === Runtime (minimal, no libc) ===
: emit-runtime ( -- )
  s" typedef long cell_t;" type cr
  s" static cell_t stk[256],*sp=stk+256;" type cr
  s" static cell_t rs[256],*rsp=rs+256;" type cr
  s" #define TOS sp[0]" type cr
  s" #define NOS sp[1]" type cr
  s" static void f_dup(void){cell_t x=TOS;*--sp=x;}" type cr
  s" static void f_drop(void){sp++;}" type cr
  s" static void f_swap(void){cell_t t=TOS;TOS=NOS;NOS=t;}" type cr
  s" static void f_over(void){*--sp=NOS;}" type cr
  s" static void f_rot(void){cell_t x=sp[2];sp[2]=sp[1];sp[1]=TOS;TOS=x;}" type cr
  s" static void f_nip(void){NOS=TOS;sp++;}" type cr
  s" static void f_tuck(void){cell_t t=TOS;TOS=NOS;NOS=t;*--sp=t;}" type cr
  s" static void f_2dup(void){*--sp=NOS;*--sp=NOS;}" type cr
  s" static void f_2drop(void){sp+=2;}" type cr
  s" static void f_neg(void){TOS=-TOS;}" type cr
  s" static void f_abs(void){if(TOS<0)TOS=-TOS;}" type cr
  s" static void f_add(void){NOS+=TOS;sp++;}" type cr
  s" static void f_sub(void){NOS-=TOS;sp++;}" type cr
  s" static void f_mul(void){NOS*=TOS;sp++;}" type cr
  s" static void f_div(void){NOS/=TOS;sp++;}" type cr
  s" static void f_mod(void){NOS%=TOS;sp++;}" type cr
  s" static void f_and(void){NOS&=TOS;sp++;}" type cr
  s" static void f_or(void){NOS|=TOS;sp++;}" type cr
  s" static void f_xor(void){NOS^=TOS;sp++;}" type cr
  s" static void f_eq(void){NOS=NOS==TOS?-1:0;sp++;}" type cr
  s" static void f_lt(void){NOS=NOS<TOS?-1:0;sp++;}" type cr
  s" static void f_gt(void){NOS=NOS>TOS?-1:0;sp++;}" type cr
  uses-io @ if
    \ Syscall-based I/O (Linux x86_64)
    s" static long sys_write(int fd,const void*buf,long n){long r;asm volatile(\"syscall\":\"=a\"(r):\"a\"(1),\"D\"(fd),\"S\"(buf),\"d\"(n):\"rcx\",\"r11\",\"memory\");return r;}" type cr
    s" static void f_emit(void){char c=*sp++;sys_write(1,&c,1);}" type cr
    s" static void f_cr(void){char c=10;sys_write(1,&c,1);}" type cr
    s" static void f_space(void){char c=32;sys_write(1,&c,1);}" type cr
    s" static void f_dot(void){char b[21];int i=20;cell_t n=*sp++;int neg=n<0;if(neg)n=-n;b[i--]=32;do{b[i--]='0'+n%10;n/=10;}while(n);if(neg)b[i--]='-';sys_write(1,b+i+1,20-i);}" type cr
  then
  cr ;

\ === Main compile pass ===
: compile-file ( addr u -- )
  r/o open-file throw >r
  begin
    pad 256 r@ read-line throw
  while
    pad swap
    begin
      dup 0> while
      over c@ bl <= if 1 /string else
        2dup bl scan 2>r
        2r@ nip - >r over r>
        \ Got word at (addr u)
        2dup s" :" compare 0= if
          2drop -1 in-def !
          \ Get name
          2r> 1 /string bl scan 1 /string
          2dup bl scan 2>r 2r@ nip - >r over r>
          \ Save as entry point
          2dup entry-name 1+ swap cmove
          dup entry-name c!
          s" static void w_" type type s" (void){" type cr
          2r>
        else 2dup s" ;" compare 0= if
          2drop 0 in-def !
          s" }" type cr
          2r>
        else 2dup s" if" compare 0= if
          2drop
          label-n @ 1+ dup label-n !
          s"   if(!*sp++){goto L" type 0 .r s" ;}" type cr
          2r>
        else 2dup s" then" compare 0= if
          2drop
          s" L" type label-n @ 0 .r s" :;" type cr
          2r>
        else 2dup s" else" compare 0= if
          2drop
          label-n @ 1+ label-n !
          s"   goto L" type label-n @ 0 .r s" ;" type cr
          s" L" type label-n @ 1- 0 .r s" :;" type cr
          2r>
        else
          \ Number or word
          2dup 0 -rot  \ accumulator
          begin dup 0> while
            over c@ dup [char] 0 >= over [char] 9 <= and if
              [char] 0 - swap 10 * + >r 1 /string r>
            else
              drop 2drop 0 -1  \ not a number
            then
          repeat
          if
            \ It's a number
            drop
            s"   *--sp=" type 0 .r s" ;" type cr
          else
            \ It's a word
            2drop emit-word
          then
          2r>
        then then then then then
      then
    repeat 2drop
  repeat drop
  r> close-file drop ;

\ === Usage ===
: usage ( -- )
  ." tf - Fifth to C" cr
  ." Usage: fifth compiler/tf.fs input.fs > out.c" cr
  ." Then:  cc out.c -o program" cr ;

\ === Main ===
argc 2 < if usage bye then

\ Get input filename
1 argv

\ Two passes: scan for I/O, then compile
2dup scan-file

\ Emit
emit-runtime
compile-file
\ Entry point - use _start for no libc
s" void _start(void){" type cr
entry-name c@ if
  s"   w_" type entry-name 1+ entry-name c@ type s" ();" type cr
then
s"   asm volatile(\"movq $60,%rax;xorl %edi,%edi;syscall\");" type cr
s" }" type cr

bye
