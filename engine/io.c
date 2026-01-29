/* io.c - Fifth I/O Primitives
 *
 * Console I/O, file operations, system(), include/require,
 * and comment words.
 */

#include "fifth.h"
#include <limits.h>
#include <libgen.h>
#include <unistd.h>

/* ============================================================
 * Console I/O
 * ============================================================ */

/* EMIT ( c -- ) Output a character */
static void p_emit(vm_t *vm) {
    fputc((int)pop(vm), vm->out);
}

/* TYPE ( addr u -- ) Output a string */
static void p_type(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    fwrite(vm->mem + addr, 1, len, vm->out);
}

/* CR ( -- ) Output newline */
static void p_cr(vm_t *vm) {
    fputc('\n', vm->out);
}

/* KEY ( -- c ) Read a character from stdin */
static void p_key(vm_t *vm) {
    push(vm, fgetc(stdin));
}

/* ACCEPT ( addr u1 -- u2 ) Read a line into buffer */
static void p_accept(vm_t *vm) {
    cell_t maxlen = pop(vm);
    cell_t addr = pop(vm);
    char *buf = (char *)(vm->mem + addr);
    if (fgets(buf, (int)maxlen, stdin)) {
        int len = strlen(buf);
        while (len > 0 && (buf[len-1] == '\n' || buf[len-1] == '\r'))
            buf[--len] = '\0';
        push(vm, (cell_t)len);
    } else {
        push(vm, 0);
    }
}

/* ============================================================
 * File I/O
 *
 * File handles are small integers (indices into vm->files[]).
 * Forth file operations return ( ior ) where 0 = success.
 * ============================================================ */

/* Find a free file slot */
static int file_alloc(vm_t *vm) {
    for (int i = 0; i < MAX_FILES; i++) {
        if (vm->files[i] == NULL)
            return i;
    }
    return -1;
}

/* Helper: extract C string from Forth string */
static void forth_to_cstr(vm_t *vm, cell_t addr, cell_t len, char *out, int max) {
    int n = (len >= max) ? max - 1 : (int)len;
    memcpy(out, vm->mem + addr, n);
    out[n] = '\0';
}

/* Expand ~ in paths */
static void expand_path(const char *in, char *out, int max) {
    if (in[0] == '~' && (in[1] == '/' || in[1] == '\0')) {
        const char *home = getenv("HOME");
        if (home) {
            snprintf(out, max, "%s%s", home, in + 1);
            return;
        }
    }
    strncpy(out, in, max - 1);
    out[max - 1] = '\0';
}

/* OPEN-FILE ( addr u mode -- fid ior ) */
static void p_open_file(vm_t *vm) {
    cell_t mode = pop(vm);
    cell_t len = pop(vm);
    cell_t addr = pop(vm);

    char path_raw[PATH_MAX], path[PATH_MAX];
    forth_to_cstr(vm, addr, len, path_raw, sizeof(path_raw));
    expand_path(path_raw, path, sizeof(path));

    const char *fmode;
    switch (mode) {
        case 0: fmode = "r";  break; /* r/o */
        case 1: fmode = "w";  break; /* w/o */
        case 2: fmode = "r+"; break; /* r/w */
        default: fmode = "r"; break;
    }

    int slot = file_alloc(vm);
    if (slot < 0) {
        push(vm, 0);
        push(vm, -1); /* no slots */
        return;
    }

    FILE *f = fopen(path, fmode);
    if (f) {
        vm->files[slot] = f;
        push(vm, (cell_t)slot);
        push(vm, 0); /* success */
    } else {
        push(vm, 0);
        push(vm, -1); /* error */
    }
}

/* CREATE-FILE ( addr u mode -- fid ior ) */
static void p_create_file(vm_t *vm) {
    cell_t mode = pop(vm);
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    (void)mode;

    char path_raw[PATH_MAX], path[PATH_MAX];
    forth_to_cstr(vm, addr, len, path_raw, sizeof(path_raw));
    expand_path(path_raw, path, sizeof(path));

    int slot = file_alloc(vm);
    if (slot < 0) {
        push(vm, 0);
        push(vm, -1);
        return;
    }

    FILE *f = fopen(path, "w");
    if (f) {
        vm->files[slot] = f;
        push(vm, (cell_t)slot);
        push(vm, 0);
    } else {
        push(vm, 0);
        push(vm, -1);
    }
}

/* CLOSE-FILE ( fid -- ior ) */
static void p_close_file(vm_t *vm) {
    cell_t fid = pop(vm);
    if (fid >= 0 && fid < MAX_FILES && vm->files[fid]) {
        fclose(vm->files[fid]);
        vm->files[fid] = NULL;
        push(vm, 0);
    } else {
        push(vm, -1);
    }
}

/* WRITE-FILE ( addr u fid -- ior ) */
static void p_write_file(vm_t *vm) {
    cell_t fid = pop(vm);
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    if (fid >= 0 && fid < MAX_FILES && vm->files[fid]) {
        size_t written = fwrite(vm->mem + addr, 1, len, vm->files[fid]);
        push(vm, (written == (size_t)len) ? 0 : -1);
    } else {
        push(vm, -1);
    }
}

/* READ-LINE ( addr u fid -- u2 flag ior ) */
static void p_read_line(vm_t *vm) {
    cell_t fid = pop(vm);
    cell_t maxlen = pop(vm);
    cell_t addr = pop(vm);
    if (fid >= 0 && fid < MAX_FILES && vm->files[fid]) {
        char *buf = (char *)(vm->mem + addr);
        if (fgets(buf, (int)maxlen, vm->files[fid])) {
            int len = strlen(buf);
            while (len > 0 && (buf[len-1] == '\n' || buf[len-1] == '\r'))
                buf[--len] = '\0';
            push(vm, (cell_t)len);
            push(vm, -1); /* flag: line read successfully */
            push(vm, 0);  /* ior: no error */
        } else {
            push(vm, 0);
            push(vm, 0);  /* flag: no more lines */
            push(vm, 0);  /* ior: no error (just EOF) */
        }
    } else {
        push(vm, 0);
        push(vm, 0);
        push(vm, -1); /* ior: error */
    }
}

/* EMIT-FILE ( c fid -- ior ) */
static void p_emit_file(vm_t *vm) {
    cell_t fid = pop(vm);
    cell_t c = pop(vm);
    if (fid >= 0 && fid < MAX_FILES && vm->files[fid]) {
        fputc((int)c, vm->files[fid]);
        push(vm, 0);
    } else {
        push(vm, -1);
    }
}

/* FLUSH-FILE ( fid -- ior ) */
static void p_flush_file(vm_t *vm) {
    cell_t fid = pop(vm);
    if (fid >= 0 && fid < MAX_FILES && vm->files[fid]) {
        fflush(vm->files[fid]);
        push(vm, 0);
    } else {
        push(vm, -1);
    }
}

/* File mode constants */
static void p_ro(vm_t *vm) { push(vm, 0); }
static void p_wo(vm_t *vm) { push(vm, 1); }
static void p_rw(vm_t *vm) { push(vm, 2); }

/* THROW ( ior -- ) If nonzero, abort */
static void p_throw(vm_t *vm) {
    cell_t ior = pop(vm);
    if (ior != 0) {
        char msg[64];
        snprintf(msg, sizeof(msg), "THROW %ld", (long)ior);
        vm_abort(vm, msg);
    }
}

/* STDOUT ( -- fid ) Push stdout file handle.
 * We use a special sentinel (-1) that output words recognize. */
static void p_stdout(vm_t *vm) { push(vm, -2); /* special: stdout */ }

/* ============================================================
 * System
 * ============================================================ */

/* SYSTEM ( addr u -- ) Execute shell command */
static void p_system(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    char cmd[8192];
    int n = (len >= (cell_t)sizeof(cmd)) ? (int)sizeof(cmd) - 1 : (int)len;
    memcpy(cmd, vm->mem + addr, n);
    cmd[n] = '\0';
    system(cmd);
}

/* BYE ( -- ) Exit the interpreter */
static void p_bye(vm_t *vm) {
    vm->running = false;
}

/* GETENV ( addr u -- addr' u' ) Get environment variable, returns 0 0 if not found */
static void p_getenv(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    char name[256];
    forth_to_cstr(vm, addr, len, name, sizeof(name));

    const char *val = getenv(name);
    if (val) {
        size_t vlen = strlen(val);
        /* Copy value to dictionary space */
        cell_t dest = vm->here;
        memcpy(vm->mem + dest, val, vlen);
        vm->here += vlen;
        push(vm, dest);
        push(vm, (cell_t)vlen);
    } else {
        push(vm, 0);
        push(vm, 0);
    }
}

/* ============================================================
 * File Loading: INCLUDE and REQUIRE
 * ============================================================ */

/* INCLUDE ( "filename" -- ) Load and interpret a file */
static void p_include(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    if (len == 0) { vm_abort(vm, "INCLUDE requires a filename"); return; }

    char path[PATH_MAX];
    expand_path(name, path, sizeof(path));
    vm_load_file(vm, path);
}

/* REQUIRE ( "filename" -- ) Load file if not already loaded */
static void p_require(vm_t *vm) {
    char name[NAME_MAX_LEN + 1];
    int len = vm_word(vm, name);
    if (len == 0) { vm_abort(vm, "REQUIRE requires a filename"); return; }

    char path[PATH_MAX];
    expand_path(name, path, sizeof(path));

    /* Resolve to absolute path for comparison */
    char resolved[PATH_MAX];
    if (realpath(path, resolved) == NULL) {
        /* File doesn't exist yet or can't resolve -- try loading anyway */
        strncpy(resolved, path, PATH_MAX - 1);
        resolved[PATH_MAX - 1] = '\0';
    }

    /* Check if already loaded */
    for (int i = 0; i < vm->loaded_count; i++) {
        if (strcmp(vm->loaded_files[i], resolved) == 0)
            return; /* Already loaded */
    }

    /* Record and load */
    if (vm->loaded_count < 256) {
        vm->loaded_files[vm->loaded_count++] = strdup(resolved);
    }
    vm_load_file(vm, path);
}

/* INCLUDED ( addr u -- ) Load file by string on stack */
static void p_included(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);
    char path_raw[PATH_MAX], path[PATH_MAX];
    forth_to_cstr(vm, addr, len, path_raw, sizeof(path_raw));
    expand_path(path_raw, path, sizeof(path));
    vm_load_file(vm, path);
}

/* ============================================================
 * Comments
 * ============================================================ */

/* \ ( -- ) Line comment: skip rest of line (IMMEDIATE) */
static void p_backslash(vm_t *vm) {
    vm->tib_pos = vm->tib_len; /* skip to end */
}

/* ( ( -- ) Block comment: skip until ) (IMMEDIATE) */
static void p_paren(vm_t *vm) {
    char buf[PAD_SIZE];
    vm_parse(vm, ')', buf); /* discard */
}

/* ============================================================
 * Base
 * ============================================================ */

static void p_base(vm_t *vm) {
    /* Return address where base is stored -- we use mem[8..15] */
    mem_store(vm, 8, vm->base);
    push(vm, 8);
}

static void p_decimal(vm_t *vm) { vm->base = 10; }
static void p_hex(vm_t *vm)     { vm->base = 16; }

/* Update base from memory after any store to base address */
/* This is a simplification -- in a full Forth, BASE would be a real variable */

/* ============================================================
 * Slurp (convenience for reading entire files)
 * ============================================================ */

/* SLURP-FILE ( addr u -- addr2 u2 ) Read entire file into memory */
static void p_slurp_file(vm_t *vm) {
    cell_t len = pop(vm);
    cell_t addr = pop(vm);

    char path_raw[PATH_MAX], path[PATH_MAX];
    forth_to_cstr(vm, addr, len, path_raw, sizeof(path_raw));
    expand_path(path_raw, path, sizeof(path));

    FILE *f = fopen(path, "r");
    if (!f) {
        push(vm, 0);
        push(vm, 0);
        return;
    }

    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    fseek(f, 0, SEEK_SET);

    /* Store at HERE (temporary) */
    cell_t dest = vm->here;
    if (size > 0 && dest + size < MEM_SIZE) {
        size_t got = fread(vm->mem + dest, 1, size, f);
        push(vm, dest);
        push(vm, (cell_t)got);
    } else {
        push(vm, 0);
        push(vm, 0);
    }
    fclose(f);
}

/* ============================================================
 * Registration
 * ============================================================ */

void io_init(vm_t *vm) {
    /* Console */
    vm_add_prim(vm, "emit",   p_emit,   false);
    vm_add_prim(vm, "type",   p_type,   false);
    vm_add_prim(vm, "cr",     p_cr,     false);
    vm_add_prim(vm, "key",    p_key,    false);
    vm_add_prim(vm, "accept", p_accept, false);

    /* File */
    vm_add_prim(vm, "open-file",   p_open_file,   false);
    vm_add_prim(vm, "create-file", p_create_file,  false);
    vm_add_prim(vm, "close-file",  p_close_file,   false);
    vm_add_prim(vm, "write-file",  p_write_file,   false);
    vm_add_prim(vm, "read-line",   p_read_line,    false);
    vm_add_prim(vm, "emit-file",   p_emit_file,    false);
    vm_add_prim(vm, "flush-file",  p_flush_file,   false);
    vm_add_prim(vm, "r/o",         p_ro,           false);
    vm_add_prim(vm, "w/o",         p_wo,           false);
    vm_add_prim(vm, "r/w",         p_rw,           false);
    vm_add_prim(vm, "throw",       p_throw,        false);
    vm_add_prim(vm, "stdout",      p_stdout,       false);
    vm_add_prim(vm, "slurp-file",  p_slurp_file,   false);

    /* System */
    vm_add_prim(vm, "system",  p_system,  false);
    vm_add_prim(vm, "bye",     p_bye,     false);
    vm_add_prim(vm, "getenv",  p_getenv,  false);

    /* File loading */
    vm_add_prim(vm, "include",  p_include,  false);
    vm_add_prim(vm, "require",  p_require,  false);
    vm_add_prim(vm, "included", p_included, false);

    /* Comments */
    vm_add_prim(vm, "\\",  p_backslash, true);
    vm_add_prim(vm, "(",   p_paren,     true);

    /* Base */
    vm_add_prim(vm, "base",    p_base,    false);
    vm_add_prim(vm, "decimal", p_decimal, false);
    vm_add_prim(vm, "hex",     p_hex,     false);
}
