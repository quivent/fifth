/* fifth.h - The Fifth Forth Engine
 *
 * A minimal, MIT-licensed Forth implementation.
 * Implements the ~65 words Fifth actually uses, nothing more.
 *
 * Memory model: flat byte array. All Forth addresses are byte offsets
 * into vm->mem[]. Cells are stored/fetched via aligned pointer casts.
 * IP (instruction pointer) is a byte offset into compiled code.
 *
 * Threading: indirect via C function pointers. Each dictionary entry
 * has a code field (prim_fn). For colon definitions, code = docol.
 * For variables, code = dovar. For constants, code = docon.
 */

#ifndef FIFTH_H
#define FIFTH_H

#include <stdint.h>
#include <stdbool.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

/* === Configuration === */
#define DSTACK_SIZE   256
#define RSTACK_SIZE   256
#define MEM_SIZE      (1024 * 1024)   /* 1 MB dictionary/data space */
#define TIB_SIZE      1024
#define PAD_SIZE      4096
#define MAX_FILES     16
#define NAME_MAX_LEN  31
#define MAX_DICT      8192

/* === Types === */
typedef intptr_t  cell_t;
typedef uintptr_t ucell_t;

typedef struct vm vm_t;
typedef void (*prim_fn)(vm_t *vm);

/* === Dictionary Entry Flags === */
#define F_IMMEDIATE  0x80
#define F_HIDDEN     0x40
#define F_LENMASK    0x3F

/* === Dictionary Entry ===
 * Stored in a C struct array (not in flat memory).
 * This simplifies the C code and is fine because Fifth
 * doesn't need FORGET or MARKER.
 */
typedef struct {
    int          link;               /* Index of previous entry (-1 = end) */
    uint8_t      flags;              /* F_IMMEDIATE | F_HIDDEN | name length */
    char         name[NAME_MAX_LEN + 1];
    prim_fn      code;               /* Handler: primitive, docol, dovar, docon, dodoes */
    cell_t       param;              /* Body: byte offset in mem[] or constant value */
    cell_t       does;               /* DOES> IP (byte offset), -1 if unused */
} dict_entry_t;

/* === Virtual Machine === */
struct vm {
    /* Dictionary */
    dict_entry_t dict[MAX_DICT];
    int          dict_count;
    int          latest;             /* Index of most recent visible entry */

    /* Data space (byte-addressable) */
    uint8_t      mem[MEM_SIZE];
    cell_t       here;               /* Next free byte offset */

    /* Data stack (grows downward) */
    cell_t       dstack[DSTACK_SIZE];
    cell_t      *sp;

    /* Return stack (grows downward) */
    cell_t       rstack[RSTACK_SIZE];
    cell_t      *rsp;

    /* Interpreter */
    cell_t       ip;                 /* Instruction pointer (byte offset into mem[]) */
    cell_t       w;                  /* Current execution token (dict index) */
    cell_t       state;              /* 0 = interpret, -1 = compile */
    cell_t       base;               /* Number base (default 10) */

    /* Input */
    char         tib[TIB_SIZE];
    int          tib_len;
    int          tib_pos;

    /* Input source stack (for INCLUDE/REQUIRE) */
    FILE        *input[MAX_FILES];
    int          input_depth;        /* 0 = stdin/tib */

    /* Output */
    FILE        *out;                /* Current output (stdout default) */

    /* File handles for Forth-level file ops */
    FILE        *files[MAX_FILES];

    /* Pad (scratch buffer for string building) */
    char         pad[PAD_SIZE];

    /* Pictured numeric output */
    char         pno_buf[128];
    int          pno_pos;

    /* State */
    bool         running;
    int          exit_code;

    /* Cached XTs for compiler internals */
    int          xt_lit;
    int          xt_branch;
    int          xt_0branch;
    int          xt_exit;
    int          xt_slit;
    int          xt_do;
    int          xt_qdo;
    int          xt_loop;
    int          xt_ploop;
    int          xt_does;

    /* Require tracking (prevent double-load) */
    char        *loaded_files[256];
    int          loaded_count;
};

/* === Inline Stack Operations === */
static inline void   push(vm_t *vm, cell_t v)  { *(--vm->sp) = v; }
static inline cell_t pop(vm_t *vm)              { return *(vm->sp++); }
static inline cell_t tos(vm_t *vm)              { return *vm->sp; }
static inline void   rpush(vm_t *vm, cell_t v)  { *(--vm->rsp) = v; }
static inline cell_t rpop(vm_t *vm)             { return *(vm->rsp++); }
static inline cell_t rtos(vm_t *vm)             { return *vm->rsp; }

static inline int depth(vm_t *vm) {
    return (int)(vm->dstack + DSTACK_SIZE - vm->sp);
}
static inline int rdepth(vm_t *vm) {
    return (int)(vm->rstack + RSTACK_SIZE - vm->rsp);
}

/* === Memory Access (byte offset addressing) === */
static inline cell_t mem_fetch(vm_t *vm, cell_t addr) {
    return *(cell_t *)(vm->mem + addr);
}
static inline void mem_store(vm_t *vm, cell_t addr, cell_t val) {
    *(cell_t *)(vm->mem + addr) = val;
}
static inline uint8_t mem_c_fetch(vm_t *vm, cell_t addr) {
    return vm->mem[addr];
}
static inline void mem_c_store(vm_t *vm, cell_t addr, uint8_t val) {
    vm->mem[addr] = val;
}

/* === Compilation Helpers === */
static inline void vm_compile_cell(vm_t *vm, cell_t val) {
    *(cell_t *)(vm->mem + vm->here) = val;
    vm->here += sizeof(cell_t);
}

static inline cell_t vm_align(cell_t n) {
    return (n + sizeof(cell_t) - 1) & ~(sizeof(cell_t) - 1);
}

/* === Instruction Fetch === */
static inline cell_t vm_fetch_ip(vm_t *vm) {
    cell_t val = *(cell_t *)(vm->mem + vm->ip);
    vm->ip += sizeof(cell_t);
    return val;
}

/* === API === */

/* Lifecycle */
vm_t *vm_create(void);
void  vm_destroy(vm_t *vm);

/* Execution */
void  vm_repl(vm_t *vm);
int   vm_load_file(vm_t *vm, const char *path);
void  vm_interpret_line(vm_t *vm, const char *line);

/* Dictionary */
int   vm_find(vm_t *vm, const char *name, int len);
int   vm_add_prim(vm_t *vm, const char *name, prim_fn fn, bool immediate);
void  vm_add_constant(vm_t *vm, const char *name, cell_t value);
void  vm_add_variable(vm_t *vm, const char *name, cell_t initial);

/* Word handlers */
void  docol(vm_t *vm);
void  dovar(vm_t *vm);
void  docon(vm_t *vm);
void  dodoes(vm_t *vm);

/* Interpreter internals (used by prims.c, io.c) */
int   vm_word(vm_t *vm, char *buf);          /* Parse next whitespace-delimited word */
int   vm_parse(vm_t *vm, char delim, char *buf); /* Parse until delimiter */
bool  vm_try_number(vm_t *vm, const char *s, int len, cell_t *result);
void  vm_execute(vm_t *vm, int xt);          /* Execute a single XT */
void  vm_run(vm_t *vm);                      /* Run from current IP until EXIT */
void  vm_abort(vm_t *vm, const char *msg);   /* Abort with message */

/* Registration */
void  prims_init(vm_t *vm);
void  io_init(vm_t *vm);
void  spawn_init(vm_t *vm);
void  tcc_init(vm_t *vm);
void  tcc_cleanup(void);

/* Codegen (for tf tool) */
const char *codegen_to_c(vm_t *vm, bool standalone);

#endif /* FIFTH_H */
