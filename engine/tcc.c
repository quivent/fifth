/* tcc.c - Embedded TinyCC JIT Compiler for Fifth
 *
 * Compiles Forth to C, then to native code via libtcc.
 * All in-memory, no external toolchain needed.
 *
 * Not built by default. Use: make WITH_TCC=1
 */

#include "fifth.h"

#ifdef WITH_TCC
#include "libtcc.h"
#endif

#include <stdarg.h>

/* === C Code Generation Buffer === */
#define CODEGEN_SIZE (256 * 1024)  /* 256KB for generated C */

static char codegen_buf[CODEGEN_SIZE];
static int codegen_pos = 0;

static void codegen_reset(void) {
    codegen_pos = 0;
    codegen_buf[0] = '\0';
}

static void codegen_emit(const char *fmt, ...) {
    va_list args;
    va_start(args, fmt);
    codegen_pos += vsnprintf(codegen_buf + codegen_pos,
                             CODEGEN_SIZE - codegen_pos, fmt, args);
    va_end(args);
}

/* === C Runtime Header === */
static const char *c_runtime_header =
"#include <stdint.h>\n"
"#include <stdio.h>\n"
"\n"
"typedef int64_t cell_t;\n"
"#define STACK_SIZE 256\n"
"static cell_t stack[STACK_SIZE];\n"
"static cell_t *sp = stack + STACK_SIZE;\n"
"static cell_t rstack[STACK_SIZE];\n"
"static cell_t *rsp = rstack + STACK_SIZE;\n"
"\n"
"#define TOS (sp[0])\n"
"#define NOS (sp[1])\n"
"#define PUSH(x) (*--sp = (x))\n"
"#define POP() (*sp++)\n"
"#define DROP() (sp++)\n"
"\n"
"/* Primitives */\n"
"static void f_dup(void) { cell_t x = TOS; PUSH(x); }\n"
"static void f_drop(void) { DROP(); }\n"
"static void f_swap(void) { cell_t t = TOS; TOS = NOS; NOS = t; }\n"
"static void f_over(void) { PUSH(NOS); }\n"
"static void f_rot(void) { cell_t x = sp[2]; sp[2] = sp[1]; sp[1] = TOS; TOS = x; }\n"
"static void f_nip(void) { NOS = TOS; DROP(); }\n"
"static void f_tuck(void) { cell_t t = TOS; TOS = NOS; NOS = t; PUSH(t); }\n"
"\n"
"static void f_add(void) { NOS += TOS; DROP(); }\n"
"static void f_sub(void) { NOS -= TOS; DROP(); }\n"
"static void f_mul(void) { NOS *= TOS; DROP(); }\n"
"static void f_div(void) { NOS /= TOS; DROP(); }\n"
"static void f_mod(void) { NOS %= TOS; DROP(); }\n"
"static void f_neg(void) { TOS = -TOS; }\n"
"static void f_abs(void) { if (TOS < 0) TOS = -TOS; }\n"
"\n"
"static void f_and(void) { NOS &= TOS; DROP(); }\n"
"static void f_or(void) { NOS |= TOS; DROP(); }\n"
"static void f_xor(void) { NOS ^= TOS; DROP(); }\n"
"static void f_invert(void) { TOS = ~TOS; }\n"
"static void f_lshift(void) { NOS <<= TOS; DROP(); }\n"
"static void f_rshift(void) { NOS >>= TOS; DROP(); }\n"
"\n"
"static void f_eq(void) { NOS = (NOS == TOS) ? -1 : 0; DROP(); }\n"
"static void f_ne(void) { NOS = (NOS != TOS) ? -1 : 0; DROP(); }\n"
"static void f_lt(void) { NOS = (NOS < TOS) ? -1 : 0; DROP(); }\n"
"static void f_gt(void) { NOS = (NOS > TOS) ? -1 : 0; DROP(); }\n"
"static void f_le(void) { NOS = (NOS <= TOS) ? -1 : 0; DROP(); }\n"
"static void f_ge(void) { NOS = (NOS >= TOS) ? -1 : 0; DROP(); }\n"
"static void f_0eq(void) { TOS = (TOS == 0) ? -1 : 0; }\n"
"static void f_0lt(void) { TOS = (TOS < 0) ? -1 : 0; }\n"
"static void f_0gt(void) { TOS = (TOS > 0) ? -1 : 0; }\n"
"\n"
"static void f_fetch(void) { TOS = *(cell_t*)TOS; }\n"
"static void f_store(void) { *(cell_t*)TOS = NOS; sp += 2; }\n"
"static void f_cfetch(void) { TOS = *(unsigned char*)TOS; }\n"
"static void f_cstore(void) { *(unsigned char*)TOS = (unsigned char)NOS; sp += 2; }\n"
"\n"
"static void f_tor(void) { *--rsp = POP(); }\n"
"static void f_fromr(void) { PUSH(*rsp++); }\n"
"static void f_rfetch(void) { PUSH(*rsp); }\n"
"\n"
"static void f_dot(void) { printf(\"%ld \", (long)POP()); }\n"
"static void f_cr(void) { printf(\"\\n\"); }\n"
"static void f_emit(void) { putchar((int)POP()); }\n"
"\n";

#ifdef WITH_TCC

/* === TCC Error Handler === */
static void tcc_error_handler(void *opaque, const char *msg) {
    (void)opaque;
    fprintf(stderr, "TCC: %s\n", msg);
}

/* === Compile and Run C Code === */
static TCCState *tcc_state = NULL;

typedef void (*forth_main_fn)(void);

static int tcc_compile_and_run(const char *c_source) {
    if (tcc_state) {
        tcc_delete(tcc_state);
    }

    tcc_state = tcc_new();
    if (!tcc_state) {
        fprintf(stderr, "TCC: Cannot create state\n");
        return -1;
    }

    tcc_set_error_func(tcc_state, NULL, tcc_error_handler);
    tcc_set_output_type(tcc_state, TCC_OUTPUT_MEMORY);

    if (tcc_compile_string(tcc_state, c_source) < 0) {
        fprintf(stderr, "TCC: Compilation failed\n");
        return -1;
    }

    if (tcc_relocate(tcc_state) < 0) {
        fprintf(stderr, "TCC: Relocation failed\n");
        return -1;
    }

    forth_main_fn fn = (forth_main_fn)tcc_get_symbol(tcc_state, "forth_main");
    if (!fn) {
        fprintf(stderr, "TCC: forth_main not found\n");
        return -1;
    }

    /* Execute */
    fn();

    return 0;
}

#endif /* WITH_TCC */

/* === Forth to C Code Generation === */

/* Generate C code for a compiled Forth word */
static void codegen_word(vm_t *vm, int xt) {
    dict_entry_t *entry = &vm->dict[xt];

    /* Sanitize name for C identifier */
    char c_name[64];
    int j = 0;
    for (int i = 0; entry->name[i] && j < 60; i++) {
        char c = entry->name[i];
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c == '_') {
            c_name[j++] = c;
        } else {
            c_name[j++] = '_';
        }
    }
    c_name[j] = '\0';

    codegen_emit("static void word_%s(void) {\n", c_name);

    if (entry->code == docol) {
        /* Colon definition - compile the body */
        cell_t ip = entry->param;
        while (1) {
            cell_t sub_xt = *(cell_t *)(vm->mem + ip);
            ip += sizeof(cell_t);

            dict_entry_t *sub = &vm->dict[sub_xt];

            /* Handle special words */
            if (sub_xt == vm->xt_exit) {
                break;
            } else if (sub_xt == vm->xt_lit) {
                cell_t val = *(cell_t *)(vm->mem + ip);
                ip += sizeof(cell_t);
                codegen_emit("    PUSH(%ld);\n", (long)val);
            } else if (sub_xt == vm->xt_branch) {
                cell_t offset = *(cell_t *)(vm->mem + ip);
                ip += sizeof(cell_t);
                codegen_emit("    goto L%ld;\n", (long)(ip + offset));
            } else if (sub_xt == vm->xt_0branch) {
                cell_t offset = *(cell_t *)(vm->mem + ip);
                ip += sizeof(cell_t);
                codegen_emit("    if (POP() == 0) goto L%ld;\n", (long)(ip + offset));
            } else if (strcmp(sub->name, "+") == 0) {
                codegen_emit("    f_add();\n");
            } else if (strcmp(sub->name, "-") == 0) {
                codegen_emit("    f_sub();\n");
            } else if (strcmp(sub->name, "*") == 0) {
                codegen_emit("    f_mul();\n");
            } else if (strcmp(sub->name, "/") == 0) {
                codegen_emit("    f_div();\n");
            } else if (strcmp(sub->name, "mod") == 0) {
                codegen_emit("    f_mod();\n");
            } else if (strcmp(sub->name, "dup") == 0) {
                codegen_emit("    f_dup();\n");
            } else if (strcmp(sub->name, "drop") == 0) {
                codegen_emit("    f_drop();\n");
            } else if (strcmp(sub->name, "swap") == 0) {
                codegen_emit("    f_swap();\n");
            } else if (strcmp(sub->name, "over") == 0) {
                codegen_emit("    f_over();\n");
            } else if (strcmp(sub->name, "rot") == 0) {
                codegen_emit("    f_rot();\n");
            } else if (strcmp(sub->name, ".") == 0) {
                codegen_emit("    f_dot();\n");
            } else if (strcmp(sub->name, "cr") == 0) {
                codegen_emit("    f_cr();\n");
            } else if (strcmp(sub->name, "emit") == 0) {
                codegen_emit("    f_emit();\n");
            } else if (strcmp(sub->name, "=") == 0) {
                codegen_emit("    f_eq();\n");
            } else if (strcmp(sub->name, "<>") == 0) {
                codegen_emit("    f_ne();\n");
            } else if (strcmp(sub->name, "<") == 0) {
                codegen_emit("    f_lt();\n");
            } else if (strcmp(sub->name, ">") == 0) {
                codegen_emit("    f_gt();\n");
            } else if (strcmp(sub->name, "<=") == 0) {
                codegen_emit("    f_le();\n");
            } else if (strcmp(sub->name, ">=") == 0) {
                codegen_emit("    f_ge();\n");
            } else if (strcmp(sub->name, "0=") == 0) {
                codegen_emit("    f_0eq();\n");
            } else if (strcmp(sub->name, "0<") == 0) {
                codegen_emit("    f_0lt();\n");
            } else if (strcmp(sub->name, "0>") == 0) {
                codegen_emit("    f_0gt();\n");
            } else if (strcmp(sub->name, "and") == 0) {
                codegen_emit("    f_and();\n");
            } else if (strcmp(sub->name, "or") == 0) {
                codegen_emit("    f_or();\n");
            } else if (strcmp(sub->name, "xor") == 0) {
                codegen_emit("    f_xor();\n");
            } else if (strcmp(sub->name, "invert") == 0) {
                codegen_emit("    f_invert();\n");
            } else if (strcmp(sub->name, "negate") == 0) {
                codegen_emit("    f_neg();\n");
            } else if (strcmp(sub->name, "abs") == 0) {
                codegen_emit("    f_abs();\n");
            } else if (strcmp(sub->name, "@") == 0) {
                codegen_emit("    f_fetch();\n");
            } else if (strcmp(sub->name, "!") == 0) {
                codegen_emit("    f_store();\n");
            } else if (strcmp(sub->name, "c@") == 0) {
                codegen_emit("    f_cfetch();\n");
            } else if (strcmp(sub->name, "c!") == 0) {
                codegen_emit("    f_cstore();\n");
            } else if (strcmp(sub->name, ">r") == 0) {
                codegen_emit("    f_tor();\n");
            } else if (strcmp(sub->name, "r>") == 0) {
                codegen_emit("    f_fromr();\n");
            } else if (strcmp(sub->name, "r@") == 0) {
                codegen_emit("    f_rfetch();\n");
            } else if (sub->code == docol) {
                /* Call another colon definition */
                char sub_name[64];
                int k = 0;
                for (int i = 0; sub->name[i] && k < 60; i++) {
                    char c = sub->name[i];
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                        (c >= '0' && c <= '9') || c == '_') {
                        sub_name[k++] = c;
                    } else {
                        sub_name[k++] = '_';
                    }
                }
                sub_name[k] = '\0';
                codegen_emit("    word_%s();\n", sub_name);
            } else {
                codegen_emit("    /* TODO: %s */\n", sub->name);
            }
        }
    }

    codegen_emit("}\n\n");
}

/* === Generate C for all words === */
static void codegen_all(vm_t *vm) {
    codegen_reset();

    /* Emit runtime header */
    codegen_emit("%s", c_runtime_header);

    /* Forward declarations for all colon definitions */
    for (int i = 0; i <= vm->latest; i++) {
        if (vm->dict[i].code == docol) {
            char c_name[64];
            int j = 0;
            for (int k = 0; vm->dict[i].name[k] && j < 60; k++) {
                char c = vm->dict[i].name[k];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '_') {
                    c_name[j++] = c;
                } else {
                    c_name[j++] = '_';
                }
            }
            c_name[j] = '\0';
            codegen_emit("static void word_%s(void);\n", c_name);
        }
    }
    codegen_emit("\n");

    /* Generate code for all colon definitions */
    for (int i = 0; i <= vm->latest; i++) {
        if (vm->dict[i].code == docol) {
            codegen_word(vm, i);
        }
    }

    /* Generate main that calls the last defined word */
    codegen_emit("void forth_main(void) {\n");
    if (vm->latest >= 0 && vm->dict[vm->latest].code == docol) {
        char c_name[64];
        int j = 0;
        for (int k = 0; vm->dict[vm->latest].name[k] && j < 60; k++) {
            char c = vm->dict[vm->latest].name[k];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == '_') {
                c_name[j++] = c;
            } else {
                c_name[j++] = '_';
            }
        }
        c_name[j] = '\0';
        codegen_emit("    word_%s();\n", c_name);
    }
    codegen_emit("}\n");
}

/* === Fifth Primitives === */

/* JIT ( -- ) Compile and run all defined words */
static void p_jit(vm_t *vm) {
    codegen_all(vm);
#ifdef WITH_TCC
    tcc_compile_and_run(codegen_buf);
#else
    fprintf(stderr, "JIT not available (build with WITH_TCC=1)\n");
    (void)vm;
#endif
}

/* EMIT-C ( -- ) Print generated C code */
static void p_emit_c(vm_t *vm) {
    codegen_all(vm);
    printf("%s", codegen_buf);
    (void)vm;
}

/* === Initialize TCC Primitives === */
void tcc_init(vm_t *vm) {
    vm_add_prim(vm, "jit", p_jit, false);
    vm_add_prim(vm, "emit-c", p_emit_c, false);
}

/* === Cleanup === */
void tcc_cleanup(void) {
#ifdef WITH_TCC
    extern TCCState *tcc_state;
    if (tcc_state) {
        tcc_delete(tcc_state);
        tcc_state = NULL;
    }
#endif
}
