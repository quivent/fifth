/* prims.c - Fifth Primitives
 *
 * All C-level primitives: stack, arithmetic, memory, comparison,
 * logic, compiler words, control flow, strings, and numeric output.
 */

#include "fifth.h"
#include <ctype.h>

/* ============================================================
 * Stack Operations
 * ============================================================ */

static void p_dup(vm_t *vm)   { cell_t a = tos(vm); push(vm, a); }
static void p_drop(vm_t *vm)  { pop(vm); }
static void p_swap(vm_t *vm)  { cell_t a = pop(vm); cell_t b = pop(vm); push(vm, a); push(vm, b); }
static void p_over(vm_t *vm)  { push(vm, vm->sp[1]); }
static void p_rot(vm_t *vm)   { cell_t c = pop(vm); cell_t b = pop(vm); cell_t a = pop(vm); push(vm, b); push(vm, c); push(vm, a); }
static void p_nip(vm_t *vm)   { cell_t a = pop(vm); pop(vm); push(vm, a); }
static void p_tuck(vm_t *vm)  { cell_t a = pop(vm); cell_t b = pop(vm); push(vm, a); push(vm, b); push(vm, a); }
static void p_qdup(vm_t *vm)  { if (tos(vm)) push(vm, tos(vm)); }
static void p_2dup(vm_t *vm)  { push(vm, vm->sp[1]); push(vm, vm->sp[1]); }
static void p_2drop(vm_t *vm) { pop(vm); pop(vm); }
static void p_2swap(vm_t *vm) { cell_t d = pop(vm); cell_t c = pop(vm); cell_t b = pop(vm); cell_t a = pop(vm);
                                push(vm, c); push(vm, d); push(vm, a); push(vm, b); }
static void p_2over(vm_t *vm) { push(vm, vm->sp[3]); push(vm, vm->sp[3]); }

static void p_to_r(vm_t *vm)    { rpush(vm, pop(vm)); }
static void p_r_from(vm_t *vm)  { push(vm, rpop(vm)); }
static void p_r_fetch(vm_t *vm) { push(vm, rtos(vm)); }
static void p_2to_r(vm_t *vm)   { cell_t b = pop(vm); cell_t a = pop(vm); rpush(vm, a); rpush(vm, b); }
static void p_2r_from(vm_t *vm) { cell_t b = rpop(vm); cell_t a = rpop(vm); push(vm, a); push(vm, b); }
static void p_2r_fetch(vm_t *vm) { push(vm, vm->rsp[1]); push(vm, vm->rsp[0]); }

static void p_depth(vm_t *vm) { push(vm, depth(vm)); }
static void p_mrot(vm_t *vm)  { cell_t c = pop(vm); cell_t b = pop(vm); cell_t a = pop(vm); push(vm, c); push(vm, a); push(vm, b); }
static void p_pick(vm_t *vm)  { cell_t n = pop(vm); push(vm, vm->sp[n]); }

/* ============================================================
 * Arithmetic
 * ============================================================ */

static void p_add(vm_t *vm)    { cell_t b = pop(vm); *vm->sp += b; }
static void p_sub(vm_t *vm)    { cell_t b = pop(vm); *vm->sp -= b; }
static void p_mul(vm_t *vm)    { cell_t b = pop(vm); *vm->sp *= b; }
static void p_div(vm_t *vm)    { cell_t b = pop(vm); if (b) *vm->sp /= b; else vm_abort(vm, "division by zero"); }
static void p_mod(vm_t *vm)    { cell_t b = pop(vm); if (b) *vm->sp %= b; else vm_abort(vm, "division by zero"); }
static void p_divmod(vm_t *vm) { cell_t b = pop(vm); cell_t a = pop(vm);
                                 if (b) { push(vm, a % b); push(vm, a / b); }
                                 else vm_abort(vm, "division by zero"); }
static void p_negate(vm_t *vm) { *vm->sp = -(*vm->sp); }
static void p_abs(vm_t *vm)    { if (*vm->sp < 0) *vm->sp = -(*vm->sp); }
static void p_min(vm_t *vm)    { cell_t b = pop(vm); if (b < *vm->sp) *vm->sp = b; }
static void p_max(vm_t *vm)    { cell_t b = pop(vm); if (b > *vm->sp) *vm->sp = b; }
static void p_1add(vm_t *vm)   { (*vm->sp)++; }
static void p_1sub(vm_t *vm)   { (*vm->sp)--; }
static void p_star_slash(vm_t *vm) { cell_t c = pop(vm); cell_t b = pop(vm); cell_t a = pop(vm);
                                     push(vm, (cell_t)(((long long)a * b) / c)); }

/* ============================================================
 * Comparison
 * ============================================================ */

static void p_eq(vm_t *vm)  { cell_t b = pop(vm); *vm->sp = (*vm->sp == b) ? -1 : 0; }
static void p_neq(vm_t *vm) { cell_t b = pop(vm); *vm->sp = (*vm->sp != b) ? -1 : 0; }
static void p_lt(vm_t *vm)  { cell_t b = pop(vm); *vm->sp = (*vm->sp < b)  ? -1 : 0; }
static void p_gt(vm_t *vm)  { cell_t b = pop(vm); *vm->sp = (*vm->sp > b)  ? -1 : 0; }
static void p_ult(vm_t *vm) { ucell_t b = (ucell_t)pop(vm); *vm->sp = ((ucell_t)*vm->sp < b) ? -1 : 0; }
static void p_0eq(vm_t *vm) { *vm->sp = (*vm->sp == 0) ? -1 : 0; }
static void p_0lt(vm_t *vm) { *vm->sp = (*vm->sp < 0)  ? -1 : 0; }
static void p_0gt(vm_t *vm) { *vm->sp = (*vm->sp > 0)  ? -1 : 0; }

/* ============================================================
 * Logic / Bitwise
 * ============================================================ */

static void p_and(vm_t *vm)    { cell_t b = pop(vm); *vm->sp &= b; }
static void p_or(vm_t *vm)     { cell_t b = pop(vm); *vm->sp |= b; }
static void p_xor(vm_t *vm)    { cell_t b = pop(vm); *vm->sp ^= b; }
static void p_invert(vm_t *vm) { *vm->sp = ~(*vm->sp); }
static void p_lshift(vm_t *vm) { cell_t n = pop(vm); *vm->sp = (ucell_t)(*vm->sp) << n; }
static void p_rshift(vm_t *vm) { cell_t n = pop(vm); *vm->sp = (ucell_t)(*vm->sp) >> n; }

/* ============================================================
 * Memory
 * ============================================================ */

static void p_fetch(vm_t *vm)  { *vm->sp = mem_fetch(vm, *vm->sp); }
static void p_store(vm_t *vm)  { cell_t addr = pop(vm); cell_t val = pop(vm); mem_store(vm, addr, val); }
static void p_cfetch(vm_t *vm) { *vm->sp = (cell_t)mem_c_fetch(vm, *vm->sp); }
static void p_cstore(vm_t *vm) { cell_t addr = pop(vm); cell_t val = pop(vm); mem_c_store(vm, addr, (uint8_t)val); }
static void p_pstore(vm_t *vm) { cell_t addr = pop(vm); cell_t val = pop(vm);
                                 mem_store(vm, addr, mem_fetch(vm, addr) + val); }

static void p_here(vm_t *vm)  { push(vm, vm->here); }
static void p_allot(vm_t *vm) { vm->here += pop(vm); }
static void p_cells(vm_t *vm) { *vm->sp *= sizeof(cell_t); }
static void p_cell_plus(vm_t *vm) { *vm->sp += sizeof(cell_t); }

static void p_comma(vm_t *vm) {
    vm->here = vm_align(vm->here);
    vm_compile_cell(vm, pop(vm));
}
static void p_c_comma(vm_t *vm) {
    mem_c_store(vm, vm->here, (uint8_t)pop(vm));
    vm->here++;
}

static void p_move(vm_t *vm) {
    cell_t n = pop(vm);
    cell_t dst = pop(vm);
    cell_t src = pop(vm);
    memmove(vm->mem + dst, vm->mem + src, n);
}

static void p_fill(vm_t *vm) {
    cell_t c = pop(vm);
    cell_t n = pop(vm);
    cell_t addr = pop(vm);
    memset(vm->mem + addr, (int)c, n);
}

static void p_slash_string(vm_t *vm) {
    cell_t n = pop(vm);
    cell_t u = pop(vm);
    cell_t addr = pop(vm);
    if (n > u) n = u;
    push(vm, addr + n);
    push(vm, u - n);
}

static void p_count(vm_t *vm) {
    cell_t addr = pop(vm);
    uint8_t len = mem_c_fetch(vm, addr);
    push(vm, addr + 1);
    push(vm, (cell_t)len);
}

/* ============================================================
 * Compiler Words
 * ============================================================ */

/* : ( "name" -- ) Start a new colon definition */
static void p_colon(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    if (len == 0) { vm_abort(vm, ": requires a name"); return; }

    int idx = vm->dict_count++;
    vm->dict[idx].link = vm->latest;
    vm->dict[idx].flags = (uint8_t)len | F_HIDDEN;
    memcpy(vm->dict[idx].name, name, len);
    vm->dict[idx].name[len] = '\0';
    vm->dict[idx].code = docol;
    vm->here = vm_align(vm->here);
    vm->dict[idx].param = vm->here;
    vm->dict[idx].does = -1;
    vm->latest = idx;
    vm->state = -1; /* compile mode */
}

/* ; ( -- ) End colon definition (IMMEDIATE) */
static void p_semicolon(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_exit);
    vm->dict[vm->latest].flags &= ~F_HIDDEN;
    vm->state = 0;
}

/* IMMEDIATE ( -- ) Mark latest word as immediate */
static void p_immediate(vm_t *vm) {
    vm->dict[vm->latest].flags |= F_IMMEDIATE;
}

/* [ ( -- ) Switch to interpret mode (IMMEDIATE) */
static void p_lbracket(vm_t *vm) { vm->state = 0; }

/* ] ( -- ) Switch to compile mode */
static void p_rbracket(vm_t *vm) { vm->state = -1; }

/* STATE ( -- addr ) Push address of state variable */
static void p_state(vm_t *vm) {
    /* We cheat: state isn't in mem[], so we use a fixed location */
    /* Store state at mem[0..7], reserve that space */
    mem_store(vm, 0, vm->state);
    push(vm, 0);
}

/* ' ( "name" -- xt ) Find word, push XT */
static void p_tick(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    int xt = vm_find(vm, name, len);
    if (xt < 0) {
        fprintf(stderr, "%s ?\n", name);
        vm_abort(vm, "' cannot find word");
        return;
    }
    push(vm, xt);
}

/* ['] ( "name" -- ) Compile XT as literal (IMMEDIATE) */
static void p_bracket_tick(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    int xt = vm_find(vm, name, len);
    if (xt < 0) {
        fprintf(stderr, "%s ?\n", name);
        vm_abort(vm, "['] cannot find word");
        return;
    }
    vm_compile_cell(vm, vm->xt_lit);
    vm_compile_cell(vm, xt);
}

/* EXECUTE ( xt -- ) Execute XT */
static void p_execute(vm_t *vm) {
    cell_t xt = pop(vm);
    vm_execute(vm, (int)xt);
}

/* >BODY ( xt -- addr ) Get body address of a CREATEd word */
static void p_to_body(vm_t *vm) {
    cell_t xt = pop(vm);
    push(vm, vm->dict[xt].param);
}

/* CREATE ( "name" -- ) Create a new dictionary entry */
static void p_create(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    if (len == 0) { vm_abort(vm, "CREATE requires a name"); return; }

    int idx = vm->dict_count++;
    vm->dict[idx].link = vm->latest;
    vm->dict[idx].flags = (uint8_t)len;
    memcpy(vm->dict[idx].name, name, len);
    vm->dict[idx].name[len] = '\0';
    vm->dict[idx].code = dovar;
    vm->here = vm_align(vm->here);
    vm->dict[idx].param = vm->here;
    vm->dict[idx].does = -1;
    vm->latest = idx;
}

/* FIND ( addr u -- xt 1 | xt -1 | addr u 0 ) */
static void p_find(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    char name[NAME_MAX_LEN + 1];
    int n = (len > NAME_MAX_LEN) ? NAME_MAX_LEN : (int)len;
    memcpy(name, vm->mem + addr, n);
    name[n] = '\0';

    int xt = vm_find(vm, name, n);
    if (xt >= 0) {
        push(vm, xt);
        push(vm, (vm->dict[xt].flags & F_IMMEDIATE) ? 1 : -1);
    } else {
        push(vm, addr);
        push(vm, len);
        push(vm, 0);
    }
}

/* LITERAL ( x -- ) Compile top of stack as literal (IMMEDIATE) */
static void p_literal(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_lit);
    vm_compile_cell(vm, pop(vm));
}

/* COMPILE, ( xt -- ) Compile an XT into current definition */
static void p_compile_comma(vm_t *vm) {
    vm_compile_cell(vm, pop(vm));
}

/* POSTPONE ( "name" -- ) Compile semantics of next word (IMMEDIATE) */
static void p_postpone(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    int xt = vm_find(vm, name, len);
    if (xt < 0) { vm_abort(vm, "POSTPONE: word not found"); return; }

    if (vm->dict[xt].flags & F_IMMEDIATE) {
        /* Immediate: compile directly */
        vm_compile_cell(vm, xt);
    } else {
        /* Non-immediate: compile code to compile it */
        vm_compile_cell(vm, vm->xt_lit);
        vm_compile_cell(vm, xt);
        int cc = vm_find(vm, "compile,", 8);
        vm_compile_cell(vm, cc);
    }
}

/* ============================================================
 * Runtime Support (not directly user-visible)
 * ============================================================ */

/* (lit) -- push inline literal */
static void p_lit(vm_t *vm) {
    push(vm, vm_fetch_ip(vm));
}

/* (branch) -- unconditional branch */
static void p_branch(vm_t *vm) {
    vm->ip = vm_fetch_ip(vm);
}

/* (0branch) -- branch if TOS is zero */
static void p_0branch(vm_t *vm) {
    cell_t dest = vm_fetch_ip(vm);
    if (pop(vm) == 0) vm->ip = dest;
}

/* (exit) -- return from colon definition */
static void p_exit(vm_t *vm) {
    vm->ip = rpop(vm);
}

/* (does>) -- runtime for DOES> */
static void p_does_runtime(vm_t *vm) {
    vm->dict[vm->latest].code = dodoes;
    vm->dict[vm->latest].does = vm->ip;
    vm->ip = rpop(vm); /* EXIT the defining word */
}

/* DOES> -- compile-time: compile (does>) into current definition */
static void p_does_compile(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_does);
}

/* (s") -- runtime: push inline string address and length */
static void p_slit(vm_t *vm) {
    cell_t len = vm_fetch_ip(vm);
    cell_t addr = vm->ip;
    push(vm, addr);
    push(vm, len);
    /* Advance IP past the string (cell-aligned) */
    vm->ip += vm_align(len);
}

/* ============================================================
 * Control Flow (IMMEDIATE compile-time words)
 * ============================================================ */

/* IF ( -- fwd ) */
static void p_if(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_0branch);
    push(vm, vm->here);
    vm_compile_cell(vm, 0); /* placeholder */
}

/* ELSE ( fwd1 -- fwd2 ) */
static void p_else(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_branch);
    cell_t fwd2 = vm->here;
    vm_compile_cell(vm, 0); /* placeholder */
    /* Resolve IF's forward ref */
    cell_t fwd1 = pop(vm);
    mem_store(vm, fwd1, vm->here);
    push(vm, fwd2);
}

/* THEN ( fwd -- ) */
static void p_then(vm_t *vm) {
    cell_t fwd = pop(vm);
    mem_store(vm, fwd, vm->here);
}

/* BEGIN ( -- back ) */
static void p_begin(vm_t *vm) {
    push(vm, vm->here);
}

/* REPEAT ( orig back -- ) */
static void p_repeat(vm_t *vm) {
    cell_t back = pop(vm);
    cell_t orig = pop(vm);
    vm_compile_cell(vm, vm->xt_branch);
    vm_compile_cell(vm, back);
    mem_store(vm, orig, vm->here);
}

/* UNTIL ( back -- ) */
static void p_until(vm_t *vm) {
    cell_t back = pop(vm);
    vm_compile_cell(vm, vm->xt_0branch);
    vm_compile_cell(vm, back);
}

/* AGAIN ( back -- ) */
static void p_again(vm_t *vm) {
    cell_t back = pop(vm);
    vm_compile_cell(vm, vm->xt_branch);
    vm_compile_cell(vm, back);
}

/* === DO / LOOP runtime === */

/* (do) runtime: ( limit index -- ) R: ( -- limit index ) */
static void p_do_rt(vm_t *vm) {
    cell_t idx = pop(vm);
    cell_t lim = pop(vm);
    rpush(vm, lim);
    rpush(vm, idx);
}

/* (?do) runtime: ( limit index -- ) skip loop if equal */
static void p_qdo_rt(vm_t *vm) {
    cell_t dest = vm_fetch_ip(vm);
    cell_t idx = pop(vm);
    cell_t lim = pop(vm);
    if (idx == lim) {
        vm->ip = dest;
    } else {
        rpush(vm, lim);
        rpush(vm, idx);
    }
}

/* (loop) runtime: increment index, check, branch */
static void p_loop_rt(vm_t *vm) {
    cell_t dest = vm_fetch_ip(vm);
    cell_t idx = rpop(vm) + 1;
    cell_t lim = rtos(vm);
    if (idx == lim) {
        rpop(vm); /* remove limit */
    } else {
        rpush(vm, idx);
        vm->ip = dest;
    }
}

/* (+loop) runtime: add step, check boundary crossing */
static void p_ploop_rt(vm_t *vm) {
    cell_t dest = vm_fetch_ip(vm);
    cell_t step = pop(vm);
    cell_t old_idx = rpop(vm);
    cell_t new_idx = old_idx + step;
    cell_t lim = rtos(vm);

    cell_t old_diff = old_idx - lim;
    cell_t new_diff = new_idx - lim;
    bool crossed = ((old_diff ^ new_diff) < 0) && ((old_diff ^ step) < 0);
    bool done = crossed || (new_diff == 0);

    if (done) {
        rpop(vm); /* remove limit */
    } else {
        rpush(vm, new_idx);
        vm->ip = dest;
    }
}

/* === DO / LOOP compile-time (IMMEDIATE) === */

/* DO ( -- orig back ) */
static void p_do_compile(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_do);
    push(vm, 0); /* no forward ref for DO (only ?DO needs one) */
    push(vm, vm->here); /* back ref for LOOP */
}

/* ?DO ( -- orig back ) */
static void p_qdo_compile(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_qdo);
    cell_t orig = vm->here;
    vm_compile_cell(vm, 0); /* placeholder for skip-past-loop */
    push(vm, orig);
    push(vm, vm->here); /* back ref for LOOP */
}

/* LOOP ( orig back -- ) */
static void p_loop_compile(vm_t *vm) {
    cell_t back = pop(vm);
    cell_t orig = pop(vm);
    vm_compile_cell(vm, vm->xt_loop);
    vm_compile_cell(vm, back);
    if (orig) mem_store(vm, orig, vm->here); /* resolve ?DO forward */
}

/* +LOOP ( orig back -- ) */
static void p_ploop_compile(vm_t *vm) {
    cell_t back = pop(vm);
    cell_t orig = pop(vm);
    vm_compile_cell(vm, vm->xt_ploop);
    vm_compile_cell(vm, back);
    if (orig) mem_store(vm, orig, vm->here);
}

/* I ( -- index ) */
static void p_i(vm_t *vm) { push(vm, *vm->rsp); }

/* J ( -- index ) Outer loop index */
static void p_j(vm_t *vm) { push(vm, vm->rsp[2]); }

/* UNLOOP ( -- ) R: ( limit index -- ) */
static void p_unloop(vm_t *vm) { rpop(vm); rpop(vm); }

/* ============================================================
 * CASE / OF / ENDOF / ENDCASE (IMMEDIATE)
 * ============================================================ */

/* CASE ( -- 0 ) sentinel on compile-time stack */
static void p_case(vm_t *vm) { push(vm, 0); }

/* OF ( -- orig ) compile: OVER = 0BRANCH fwd DROP */
static void p_of(vm_t *vm) {
    int xt_over = vm_find(vm, "over", 4);
    int xt_eq = vm_find(vm, "=", 1);
    int xt_drop = vm_find(vm, "drop", 4);
    vm_compile_cell(vm, xt_over);
    vm_compile_cell(vm, xt_eq);
    vm_compile_cell(vm, vm->xt_0branch);
    cell_t orig = vm->here;
    vm_compile_cell(vm, 0); /* placeholder */
    vm_compile_cell(vm, xt_drop);
    push(vm, orig);
}

/* ENDOF ( orig -- fwd ) compile: BRANCH fwd; resolve OF */
static void p_endof(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_branch);
    cell_t fwd = vm->here;
    vm_compile_cell(vm, 0); /* placeholder */
    cell_t orig = pop(vm);
    mem_store(vm, orig, vm->here); /* resolve OF's 0branch */
    push(vm, fwd); /* push for ENDCASE to resolve */
}

/* ENDCASE ( 0 fwd... -- ) compile: DROP; resolve all ENDOF branches */
static void p_endcase(vm_t *vm) {
    int xt_drop = vm_find(vm, "drop", 4);
    vm_compile_cell(vm, xt_drop);
    /* Resolve all ENDOF forward refs until we hit the sentinel 0 */
    while (tos(vm) != 0) {
        cell_t fwd = pop(vm);
        mem_store(vm, fwd, vm->here);
    }
    pop(vm); /* remove sentinel */
}

/* ============================================================
 * String Words
 * ============================================================ */

/* S" ( -- addr u ) Parse string, either interpret or compile */
static void p_s_quote(vm_t *vm) {
    char buf[PAD_SIZE];
    int len = vm_parse(vm, '"', buf);

    if (vm->state) {
        /* Compile: (s") len bytes... */
        vm_compile_cell(vm, vm->xt_slit);
        vm_compile_cell(vm, len);
        memcpy(vm->mem + vm->here, buf, len);
        vm->here += vm_align(len);
    } else {
        /* Interpret: copy to pad, push */
        memcpy(vm->pad, buf, len);
        vm->pad[len] = '\0';
        /* Store in mem[] at a scratch area so @ works */
        cell_t addr = vm->here; /* Use space above HERE temporarily */
        memcpy(vm->mem + addr, buf, len);
        push(vm, addr);
        push(vm, (cell_t)len);
    }
}

/* S\" ( -- addr u ) Like S" but with escape processing.
 * Uses escape-aware parsing: \" inside the string is an escaped quote,
 * not the closing delimiter. Only an unescaped " closes the string.
 */
static void p_s_bs_quote(vm_t *vm) {
    /* Skip one leading space if present */
    if (vm->tib_pos < vm->tib_len && vm->tib[vm->tib_pos] == ' ')
        vm->tib_pos++;

    /* Parse with escape awareness — \" does NOT end the string */
    char buf[PAD_SIZE];
    int len = 0;
    while (vm->tib_pos < vm->tib_len && len < PAD_SIZE - 1) {
        char c = vm->tib[vm->tib_pos++];
        if (c == '"') break;  /* Unescaped quote ends the string */
        if (c == '\\' && vm->tib_pos < vm->tib_len) {
            char esc = vm->tib[vm->tib_pos++];
            switch (esc) {
                case 'n': buf[len++] = '\n'; break;
                case 'r': buf[len++] = '\r'; break;
                case 't': buf[len++] = '\t'; break;
                case '"': buf[len++] = '"';  break;
                case '\\': buf[len++] = '\\'; break;
                case '0': buf[len++] = '\0'; break;
                case 'a': buf[len++] = '\a'; break;
                case 'b': buf[len++] = '\b'; break;
                case 'e': buf[len++] = 27;   break;
                default:  buf[len++] = esc;  break;
            }
        } else {
            buf[len++] = c;
        }
    }

    if (vm->state) {
        vm_compile_cell(vm, vm->xt_slit);
        vm_compile_cell(vm, len);
        memcpy(vm->mem + vm->here, buf, len);
        vm->here += vm_align(len);
    } else {
        cell_t addr = vm->here;
        memcpy(vm->mem + addr, buf, len);
        push(vm, addr);
        push(vm, (cell_t)len);
    }
}

/* [CHAR] ( "c" -- ) Compile character literal (IMMEDIATE) */
static void p_bracket_char(vm_t *vm) {
    char buf[NAME_MAX_LEN + 1];
    int len = vm_word(vm, buf);
    if (len == 0) { vm_abort(vm, "[CHAR] needs a character"); return; }
    if (vm->state) {
        vm_compile_cell(vm, vm->xt_lit);
        vm_compile_cell(vm, (cell_t)buf[0]);
    } else {
        push(vm, (cell_t)buf[0]);
    }
}

/* CHAR ( "c" -- c ) Push character value */
static void p_char(vm_t *vm) {
    char buf[NAME_MAX_LEN + 1];
    int len = vm_word(vm, buf);
    if (len == 0) { vm_abort(vm, "CHAR needs a character"); return; }
    push(vm, (cell_t)buf[0]);
}

/* PARSE-NAME ( -- addr u ) Parse next whitespace-delimited word */
static void p_parse_name(vm_t *vm) {
    char buf[NAME_MAX_LEN + 1];
    int len = vm_word(vm, buf);
    /* Copy to dictionary space */
    cell_t dest = vm->here;
    memcpy(vm->mem + dest, buf, len);
    vm->here += len;
    push(vm, dest);
    push(vm, (cell_t)len);
}

/* ============================================================
 * Numeric Output
 * ============================================================ */

/* . ( n -- ) Print number and space */
static void p_dot(vm_t *vm) {
    cell_t n = pop(vm);
    fprintf(vm->out, "%ld ", (long)n);
}

/* U. ( u -- ) Print unsigned number and space */
static void p_u_dot(vm_t *vm) {
    ucell_t n = (ucell_t)pop(vm);
    fprintf(vm->out, "%lu ", (unsigned long)n);
}

/* .S ( -- ) Print stack contents */
static void p_dot_s(vm_t *vm) {
    int d = depth(vm);
    fprintf(vm->out, "<%d> ", d);
    for (int i = d - 1; i >= 0; i--) {
        fprintf(vm->out, "%ld ", (long)vm->sp[i]);
    }
}

/* <# ( -- ) Begin pictured numeric output */
static void p_pno_begin(vm_t *vm) {
    vm->pno_pos = sizeof(vm->pno_buf);
}

/* # ( ud -- ud' ) Add one digit */
static void p_pno_digit(vm_t *vm) {
    ucell_t d = (ucell_t)pop(vm);
    ucell_t rem = d % (ucell_t)vm->base;
    d /= (ucell_t)vm->base;
    push(vm, (cell_t)d);
    char c = (rem < 10) ? '0' + rem : 'a' + rem - 10;
    vm->pno_buf[--vm->pno_pos] = c;
}

/* #S ( ud -- 0 ) Add all digits */
static void p_pno_digits(vm_t *vm) {
    do {
        p_pno_digit(vm);
    } while (tos(vm) != 0);
}

/* #> ( ud -- addr u ) End pictured numeric output */
static void p_pno_end(vm_t *vm) {
    pop(vm); /* drop remaining */
    int len = sizeof(vm->pno_buf) - vm->pno_pos;
    cell_t addr = vm->here; /* temporary space */
    memcpy(vm->mem + addr, vm->pno_buf + vm->pno_pos, len);
    push(vm, addr);
    push(vm, (cell_t)len);
}

/* HOLD ( c -- ) Insert character into PNO buffer */
static void p_hold(vm_t *vm) {
    char c = (char)pop(vm);
    vm->pno_buf[--vm->pno_pos] = c;
}

/* SIGN ( n -- ) Add minus sign if negative */
static void p_sign(vm_t *vm) {
    if (pop(vm) < 0)
        vm->pno_buf[--vm->pno_pos] = '-';
}

/* ============================================================
 * Number Parsing (Gforth compatibility)
 * ============================================================ */

/* S>NUMBER? ( addr u -- d flag ) Try to parse string as number.
 * Gforth-compatible: returns double (we push n 0) and flag.
 * Used by sql.fs to parse COUNT results.
 */
static void p_s_to_number(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    char buf[64];
    int n = (len >= 63) ? 63 : (int)len;
    memcpy(buf, vm->mem + addr, n);
    buf[n] = '\0';
    /* Strip whitespace */
    while (n > 0 && (buf[n-1] == ' ' || buf[n-1] == '\n' || buf[n-1] == '\r'))
        buf[--n] = '\0';
    cell_t result;
    if (vm_try_number(vm, buf, strlen(buf), &result)) {
        push(vm, result);
        push(vm, 0);  /* high cell of double = 0 */
        push(vm, -1); /* flag: success */
    } else {
        push(vm, 0);
        push(vm, 0);
        push(vm, 0);  /* flag: failure */
    }
}

/* >NUMBER ( ud1 addr1 u1 -- ud2 addr2 u2 ) Partial number conversion */
static void p_to_number(vm_t *vm) {
    cell_t u = pop(vm);
    cell_t addr = pop(vm);
    cell_t dhi = pop(vm);
    cell_t dlo = pop(vm);
    (void)dhi; /* Ignore high cell for simplicity */
    while (u > 0) {
        char c = (char)mem_c_fetch(vm, addr);
        int digit;
        if (c >= '0' && c <= '9') digit = c - '0';
        else if (c >= 'a' && c <= 'z') digit = c - 'a' + 10;
        else if (c >= 'A' && c <= 'Z') digit = c - 'A' + 10;
        else break;
        if (digit >= vm->base) break;
        dlo = dlo * vm->base + digit;
        addr++;
        u--;
    }
    push(vm, dlo);
    push(vm, 0); /* dhi */
    push(vm, addr);
    push(vm, u);
}

/* ============================================================
 * Miscellaneous
 * ============================================================ */

static void p_noop(vm_t *vm)  { (void)vm; }
static void p_true(vm_t *vm)  { push(vm, -1); }
static void p_false(vm_t *vm) { push(vm, 0); }
static void p_bl(vm_t *vm)    { push(vm, 32); }
static void p_space(vm_t *vm) { fputc(' ', vm->out); }
static void p_spaces(vm_t *vm) { cell_t n = pop(vm); while (n-- > 0) fputc(' ', vm->out); }

static void p_abort(vm_t *vm) { vm_abort(vm, "ABORT called"); }
static void p_abort_quote(vm_t *vm) {
    /* ABORT" -- compile-time only */
    char buf[PAD_SIZE];
    int len = vm_parse(vm, '"', buf);
    /* Compile: IF (s") len msg TYPE ABORT THEN */
    /* Simplified: always compile the test + message */
    vm_compile_cell(vm, vm->xt_0branch);
    cell_t fwd = vm->here;
    vm_compile_cell(vm, 0);

    /* Compile the string */
    vm_compile_cell(vm, vm->xt_slit);
    vm_compile_cell(vm, len);
    memcpy(vm->mem + vm->here, buf, len);
    vm->here += vm_align(len);

    /* Compile TYPE and ABORT */
    int xt_type = vm_find(vm, "type", 4);
    int xt_abort = vm_find(vm, "abort", 5);
    if (xt_type >= 0) vm_compile_cell(vm, xt_type);
    if (xt_abort >= 0) vm_compile_cell(vm, xt_abort);

    /* Resolve forward branch (skip if flag was false) */
    /* Wait - ABORT" triggers if flag is TRUE (nonzero).
     * So we need: if flag is 0, skip. That's what 0BRANCH does. */
    /* But ABORT" should abort if TRUE. 0branch skips if 0.
     * So if flag is nonzero, 0branch does NOT skip -> we fall through to abort.
     * If flag is 0, 0branch DOES skip -> we jump past abort. Correct. */
    mem_store(vm, fwd, vm->here);
}

/* RECURSE ( -- ) Compile a call to the current definition (IMMEDIATE) */
static void p_recurse(vm_t *vm) {
    vm_compile_cell(vm, vm->latest);
}

/* EXIT ( -- ) compile (exit) for user use (IMMEDIATE in compile mode) */
static void p_user_exit(vm_t *vm) {
    if (vm->state) {
        vm_compile_cell(vm, vm->xt_exit);
    }
}

/* ." ( -- ) Parse and compile/execute string + TYPE (IMMEDIATE) */
static void p_dot_quote(vm_t *vm) {
    char buf[PAD_SIZE];
    int len = vm_parse(vm, '"', buf);

    if (vm->state) {
        vm_compile_cell(vm, vm->xt_slit);
        vm_compile_cell(vm, len);
        memcpy(vm->mem + vm->here, buf, len);
        vm->here += vm_align(len);
        int xt_type = vm_find(vm, "type", 4);
        if (xt_type >= 0) vm_compile_cell(vm, xt_type);
    } else {
        fwrite(buf, 1, len, vm->out);
    }
}

/* .( ( -- ) Print until ) immediately */
static void p_dot_paren(vm_t *vm) {
    char buf[PAD_SIZE];
    int len = vm_parse(vm, ')', buf);
    fwrite(buf, 1, len, vm->out);
}

/* ============================================================
 * WHILE fixup - redo cleanly
 * ============================================================ */

/* Proper WHILE: ( dest -- orig dest )
 * Compile 0branch with forward ref, swap so dest is on top for REPEAT
 */
static void p_while_fixed(vm_t *vm) {
    vm_compile_cell(vm, vm->xt_0branch);
    cell_t orig = vm->here;
    vm_compile_cell(vm, 0); /* placeholder */
    cell_t dest = pop(vm); /* the BEGIN address */
    push(vm, orig);        /* forward ref (for REPEAT to resolve) */
    push(vm, dest);        /* put BEGIN address back on top */
}

/* ============================================================
 * Registration
 * ============================================================ */

void prims_init(vm_t *vm) {
    /* Reserve first 64 bytes for system variables */
    vm->here = 64;

    /* Runtime support (not directly user-visible, but need XTs) */
    vm->xt_lit    = vm_add_prim(vm, "(lit)",    p_lit,    false);
    vm->xt_branch = vm_add_prim(vm, "(branch)", p_branch, false);
    vm->xt_0branch= vm_add_prim(vm, "(0branch)",p_0branch,false);
    vm->xt_exit   = vm_add_prim(vm, "(exit)",   p_exit,   false);
    vm->xt_slit   = vm_add_prim(vm, "(s\")",    p_slit,   false);
    vm->xt_do     = vm_add_prim(vm, "(do)",     p_do_rt,  false);
    vm->xt_qdo    = vm_add_prim(vm, "(?do)",    p_qdo_rt, false);
    vm->xt_loop   = vm_add_prim(vm, "(loop)",   p_loop_rt,false);
    vm->xt_ploop  = vm_add_prim(vm, "(+loop)",  p_ploop_rt,false);
    vm->xt_does   = vm_add_prim(vm, "(does>)",  p_does_runtime, false);

    /* Stack */
    vm_add_prim(vm, "dup",    p_dup,    false);
    vm_add_prim(vm, "drop",   p_drop,   false);
    vm_add_prim(vm, "swap",   p_swap,   false);
    vm_add_prim(vm, "over",   p_over,   false);
    vm_add_prim(vm, "rot",    p_rot,    false);
    vm_add_prim(vm, "-rot",   p_mrot,   false);
    vm_add_prim(vm, "nip",    p_nip,    false);
    vm_add_prim(vm, "tuck",   p_tuck,   false);
    vm_add_prim(vm, "?dup",   p_qdup,   false);
    vm_add_prim(vm, "2dup",   p_2dup,   false);
    vm_add_prim(vm, "2drop",  p_2drop,  false);
    vm_add_prim(vm, "2swap",  p_2swap,  false);
    vm_add_prim(vm, "2over",  p_2over,  false);
    vm_add_prim(vm, ">r",     p_to_r,   false);
    vm_add_prim(vm, "r>",     p_r_from, false);
    vm_add_prim(vm, "r@",     p_r_fetch,false);
    vm_add_prim(vm, "2>r",    p_2to_r,  false);
    vm_add_prim(vm, "2r>",    p_2r_from, false);
    vm_add_prim(vm, "2r@",    p_2r_fetch,false);
    vm_add_prim(vm, "depth",  p_depth,  false);
    vm_add_prim(vm, "pick",   p_pick,   false);

    /* Arithmetic */
    vm_add_prim(vm, "+",      p_add,    false);
    vm_add_prim(vm, "-",      p_sub,    false);
    vm_add_prim(vm, "*",      p_mul,    false);
    vm_add_prim(vm, "/",      p_div,    false);
    vm_add_prim(vm, "mod",    p_mod,    false);
    vm_add_prim(vm, "/mod",   p_divmod, false);
    vm_add_prim(vm, "negate", p_negate, false);
    vm_add_prim(vm, "abs",    p_abs,    false);
    vm_add_prim(vm, "min",    p_min,    false);
    vm_add_prim(vm, "max",    p_max,    false);
    vm_add_prim(vm, "1+",     p_1add,   false);
    vm_add_prim(vm, "1-",     p_1sub,   false);
    vm_add_prim(vm, "*/",     p_star_slash, false);

    /* Comparison */
    vm_add_prim(vm, "=",      p_eq,     false);
    vm_add_prim(vm, "<>",     p_neq,    false);
    vm_add_prim(vm, "<",      p_lt,     false);
    vm_add_prim(vm, ">",      p_gt,     false);
    vm_add_prim(vm, "u<",     p_ult,    false);
    vm_add_prim(vm, "0=",     p_0eq,    false);
    vm_add_prim(vm, "0<",     p_0lt,    false);
    vm_add_prim(vm, "0>",     p_0gt,    false);

    /* Logic */
    vm_add_prim(vm, "and",    p_and,    false);
    vm_add_prim(vm, "or",     p_or,     false);
    vm_add_prim(vm, "xor",    p_xor,    false);
    vm_add_prim(vm, "invert", p_invert, false);
    vm_add_prim(vm, "lshift", p_lshift, false);
    vm_add_prim(vm, "rshift", p_rshift, false);

    /* Memory */
    vm_add_prim(vm, "@",      p_fetch,  false);
    vm_add_prim(vm, "!",      p_store,  false);
    vm_add_prim(vm, "c@",     p_cfetch, false);
    vm_add_prim(vm, "c!",     p_cstore, false);
    vm_add_prim(vm, "+!",     p_pstore, false);
    vm_add_prim(vm, "here",   p_here,   false);
    vm_add_prim(vm, "allot",  p_allot,  false);
    vm_add_prim(vm, "cells",  p_cells,  false);
    vm_add_prim(vm, "cell+",  p_cell_plus, false);
    vm_add_prim(vm, ",",      p_comma,  false);
    vm_add_prim(vm, "c,",     p_c_comma,false);
    vm_add_prim(vm, "move",   p_move,   false);
    vm_add_prim(vm, "fill",   p_fill,   false);
    vm_add_prim(vm, "/string",p_slash_string, false);
    vm_add_prim(vm, "count",  p_count,  false);

    /* Compiler */
    vm_add_prim(vm, ":",        p_colon,     false);
    vm_add_prim(vm, ";",        p_semicolon, true);
    vm_add_prim(vm, "immediate",p_immediate, false);
    vm_add_prim(vm, "[",        p_lbracket,  true);
    vm_add_prim(vm, "]",        p_rbracket,  false);
    vm_add_prim(vm, "state",    p_state,     false);
    vm_add_prim(vm, "'",        p_tick,      false);
    vm_add_prim(vm, "[']",      p_bracket_tick, true);
    vm_add_prim(vm, "execute",  p_execute,   false);
    vm_add_prim(vm, ">body",    p_to_body,   false);
    vm_add_prim(vm, "create",   p_create,    false);
    vm_add_prim(vm, "find",     p_find,      false);
    vm_add_prim(vm, "literal",  p_literal,   true);
    vm_add_prim(vm, "compile,", p_compile_comma, false);
    vm_add_prim(vm, "postpone", p_postpone,  true);
    vm_add_prim(vm, "does>",    p_does_compile, true);  /* compile-time: compile (does>) */
    vm_add_prim(vm, "recurse",  p_recurse,   true);

    /* Control Flow (IMMEDIATE) */
    vm_add_prim(vm, "if",      p_if,         true);
    vm_add_prim(vm, "else",    p_else,       true);
    vm_add_prim(vm, "then",    p_then,       true);
    vm_add_prim(vm, "begin",   p_begin,      true);
    vm_add_prim(vm, "while",   p_while_fixed,true);
    vm_add_prim(vm, "repeat",  p_repeat,     true);
    vm_add_prim(vm, "until",   p_until,      true);
    vm_add_prim(vm, "again",   p_again,      true);
    vm_add_prim(vm, "do",      p_do_compile, true);
    vm_add_prim(vm, "?do",     p_qdo_compile,true);
    vm_add_prim(vm, "loop",    p_loop_compile,true);
    vm_add_prim(vm, "+loop",   p_ploop_compile,true);
    vm_add_prim(vm, "i",       p_i,          false);
    vm_add_prim(vm, "j",       p_j,          false);
    vm_add_prim(vm, "unloop",  p_unloop,     false);
    vm_add_prim(vm, "case",    p_case,       true);
    vm_add_prim(vm, "of",      p_of,         true);
    vm_add_prim(vm, "endof",   p_endof,      true);
    vm_add_prim(vm, "endcase", p_endcase,    true);
    vm_add_prim(vm, "exit",    p_user_exit,  true);

    /* Strings */
    vm_add_prim(vm, "s\"",      p_s_quote,    true);
    vm_add_prim(vm, "s\\\"",    p_s_bs_quote, true);
    vm_add_prim(vm, "[char]",     p_bracket_char, true);
    vm_add_prim(vm, "char",       p_char,         false);
    vm_add_prim(vm, "parse-name", p_parse_name,   false);
    vm_add_prim(vm, ".\"",      p_dot_quote,  true);
    vm_add_prim(vm, ".(",       p_dot_paren,  true);
    vm_add_prim(vm, "abort\"",  p_abort_quote,true);

    /* Numeric output */
    vm_add_prim(vm, ".",      p_dot,       false);
    vm_add_prim(vm, "u.",     p_u_dot,     false);
    vm_add_prim(vm, ".s",     p_dot_s,     false);
    vm_add_prim(vm, "<#",     p_pno_begin, false);
    vm_add_prim(vm, "#",      p_pno_digit, false);
    vm_add_prim(vm, "#s",     p_pno_digits,false);
    vm_add_prim(vm, "#>",     p_pno_end,   false);
    vm_add_prim(vm, "hold",   p_hold,      false);
    vm_add_prim(vm, "sign",   p_sign,      false);

    /* Misc */
    vm_add_prim(vm, "noop",   p_noop,      false);
    vm_add_prim(vm, "true",   p_true,      false);
    vm_add_prim(vm, "false",  p_false,     false);
    vm_add_prim(vm, "bl",     p_bl,        false);
    vm_add_prim(vm, "space",  p_space,     false);
    vm_add_prim(vm, "spaces", p_spaces,    false);
    vm_add_prim(vm, "abort",  p_abort,     false);

    /* Constants */
    vm_add_constant(vm, "cell", sizeof(cell_t));

    /* Number parsing */
    vm_add_prim(vm, "s>number?", p_s_to_number, false);
    vm_add_prim(vm, ">number",   p_to_number,   false);
}
