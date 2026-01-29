/* vm.c - Fifth Virtual Machine
 *
 * VM lifecycle, inner/outer interpreter, dictionary operations.
 * This is the heart of the engine.
 */

#include "fifth.h"
#include <ctype.h>
#include <errno.h>
#include <strings.h>

/* === Word Handlers === */

void docol(vm_t *vm) {
    rpush(vm, vm->ip);
    vm->ip = vm->dict[vm->w].param;
}

void dovar(vm_t *vm) {
    push(vm, vm->dict[vm->w].param);
}

void docon(vm_t *vm) {
    push(vm, vm->dict[vm->w].param);
}

void dodoes(vm_t *vm) {
    push(vm, vm->dict[vm->w].param);
    rpush(vm, vm->ip);
    vm->ip = vm->dict[vm->w].does;
}

/* === Inner Interpreter === */

/* Execute a single execution token.
 * For colon definitions (docol/dodoes), runs the inner interpreter
 * until the word returns via (exit).
 */
void vm_execute(vm_t *vm, int xt) {
    vm->w = xt;
    if (vm->dict[xt].code == docol || vm->dict[xt].code == dodoes) {
        vm->dict[xt].code(vm);  /* Sets up IP */
        vm_run(vm);             /* Run until (exit) */
    } else {
        vm->dict[xt].code(vm);  /* Primitive: just call */
    }
}

/* Run compiled code starting from current IP until return stack empties */
void vm_run(vm_t *vm) {
    cell_t *rsp_base = vm->rsp;
    while (vm->running && vm->rsp <= rsp_base) {
        cell_t xt = vm_fetch_ip(vm);
        vm->w = xt;
        vm->dict[xt].code(vm);
    }
}

/* === Dictionary Operations === */

/* Find a word by name. Returns dict index or -1. */
int vm_find(vm_t *vm, const char *name, int len) {
    for (int i = vm->latest; i >= 0; i = vm->dict[i].link) {
        if (vm->dict[i].flags & F_HIDDEN) continue;
        int entry_len = vm->dict[i].flags & F_LENMASK;
        if (entry_len != len) continue;
        if (strncasecmp(vm->dict[i].name, name, len) == 0)
            return i;
    }
    return -1;
}

/* Add a C primitive to the dictionary */
int vm_add_prim(vm_t *vm, const char *name, prim_fn fn, bool immediate) {
    int idx = vm->dict_count++;
    int len = strlen(name);
    if (len > NAME_MAX_LEN) len = NAME_MAX_LEN;

    vm->dict[idx].link = vm->latest;
    vm->dict[idx].flags = (uint8_t)len | (immediate ? F_IMMEDIATE : 0);
    memcpy(vm->dict[idx].name, name, len);
    vm->dict[idx].name[len] = '\0';
    vm->dict[idx].code = fn;
    vm->dict[idx].param = 0;
    vm->dict[idx].does = -1;
    vm->latest = idx;
    return idx;
}

/* Add a constant */
void vm_add_constant(vm_t *vm, const char *name, cell_t value) {
    int idx = vm_add_prim(vm, name, docon, false);
    vm->dict[idx].param = value;
}

/* Add a variable (allocates a cell in mem[]) */
void vm_add_variable(vm_t *vm, const char *name, cell_t initial) {
    int idx = vm_add_prim(vm, name, dovar, false);
    vm->here = vm_align(vm->here);
    vm->dict[idx].param = vm->here;
    mem_store(vm, vm->here, initial);
    vm->here += sizeof(cell_t);
}

/* === Input Parsing === */

/* Skip whitespace, parse next word into buf. Returns length. */
int vm_word(vm_t *vm, char *buf) {
    /* Skip leading whitespace */
    while (vm->tib_pos < vm->tib_len && vm->tib[vm->tib_pos] <= ' ')
        vm->tib_pos++;

    int len = 0;
    while (vm->tib_pos < vm->tib_len && vm->tib[vm->tib_pos] > ' ' && len < NAME_MAX_LEN) {
        buf[len++] = vm->tib[vm->tib_pos++];
    }
    buf[len] = '\0';
    return len;
}

/* Parse until delimiter (not whitespace-skipping). For S", etc. */
int vm_parse(vm_t *vm, char delim, char *buf) {
    /* Skip one leading space if present */
    if (vm->tib_pos < vm->tib_len && vm->tib[vm->tib_pos] == ' ')
        vm->tib_pos++;

    int len = 0;
    while (vm->tib_pos < vm->tib_len && vm->tib[vm->tib_pos] != delim && len < PAD_SIZE - 1) {
        buf[len++] = vm->tib[vm->tib_pos++];
    }
    if (vm->tib_pos < vm->tib_len)
        vm->tib_pos++; /* skip delimiter */
    buf[len] = '\0';
    return len;
}

/* Try to parse a string as a number in the current base. */
bool vm_try_number(vm_t *vm, const char *s, int len, cell_t *result) {
    if (len == 0) return false;

    cell_t val = 0;
    int i = 0;
    bool negative = false;

    if (s[0] == '-' && len > 1) { negative = true; i = 1; }
    else if (s[0] == '+' && len > 1) { i = 1; }

    /* Check for base prefixes */
    cell_t base = vm->base;
    if (i < len && s[i] == '$') { base = 16; i++; }
    else if (i < len && s[i] == '#') { base = 10; i++; }
    else if (i < len && s[i] == '%') { base = 2; i++; }
    else if (len > i + 2 && s[i] == '0' && (s[i+1] == 'x' || s[i+1] == 'X')) {
        base = 16; i += 2;
    }

    if (i >= len) return false;

    for (; i < len; i++) {
        int digit;
        char c = s[i];
        if (c >= '0' && c <= '9') digit = c - '0';
        else if (c >= 'a' && c <= 'z') digit = c - 'a' + 10;
        else if (c >= 'A' && c <= 'Z') digit = c - 'A' + 10;
        else return false;

        if (digit >= base) return false;
        val = val * base + digit;
    }

    *result = negative ? -val : val;
    return true;
}

/* === Abort === */

void vm_abort(vm_t *vm, const char *msg) {
    fprintf(stderr, "ABORT: %s\n", msg);
    /* Reset stacks */
    vm->sp = vm->dstack + DSTACK_SIZE;
    vm->rsp = vm->rstack + RSTACK_SIZE;
    vm->state = 0;
    /* If loading a file, close it and return to interactive */
    while (vm->input_depth > 0) {
        fclose(vm->input[vm->input_depth]);
        vm->input_depth--;
    }
}

/* === Outer Interpreter === */

/* Interpret a single line (already in TIB) */
static void vm_interpret_tib(vm_t *vm) {
    char word_buf[NAME_MAX_LEN + 1];

    while (vm->running) {
        int len = vm_word(vm, word_buf);
        if (len == 0) break; /* end of line */

        /* Try to find the word */
        int xt = vm_find(vm, word_buf, len);
        if (xt >= 0) {
            if (vm->state && !(vm->dict[xt].flags & F_IMMEDIATE)) {
                /* Compiling: compile the XT */
                vm_compile_cell(vm, xt);
            } else {
                /* Interpreting (or immediate word): execute */
                vm_execute(vm, xt);
            }
            continue;
        }

        /* Try as a number */
        cell_t num;
        if (vm_try_number(vm, word_buf, len, &num)) {
            if (vm->state) {
                /* Compiling: compile as literal */
                vm_compile_cell(vm, vm->xt_lit);
                vm_compile_cell(vm, num);
            } else {
                push(vm, num);
            }
            continue;
        }

        /* Unknown word */
        fprintf(stderr, "%s ?\n", word_buf);
        vm_abort(vm, "undefined word");
        return;
    }
}

/* Interpret a C string */
void vm_interpret_line(vm_t *vm, const char *line) {
    int len = strlen(line);
    if (len >= TIB_SIZE) len = TIB_SIZE - 1;
    memcpy(vm->tib, line, len);
    vm->tib[len] = '\0';
    vm->tib_len = len;
    vm->tib_pos = 0;
    vm_interpret_tib(vm);
}

/* === File Loading === */

int vm_load_file(vm_t *vm, const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) {
        fprintf(stderr, "Cannot open: %s\n", path);
        return -1;
    }

    vm->input_depth++;
    vm->input[vm->input_depth] = f;

    char line[TIB_SIZE];
    while (vm->running && fgets(line, sizeof(line), f)) {
        /* Strip newline */
        int len = strlen(line);
        while (len > 0 && (line[len-1] == '\n' || line[len-1] == '\r'))
            line[--len] = '\0';

        vm->tib_len = len;
        memcpy(vm->tib, line, len + 1);
        vm->tib_pos = 0;
        vm_interpret_tib(vm);
    }

    fclose(f);
    vm->input_depth--;
    return 0;
}

/* === REPL === */

void vm_repl(vm_t *vm) {
    char line[TIB_SIZE];

    while (vm->running) {
        if (vm->state)
            fprintf(stderr, "  compiled ");
        else
            fprintf(stderr, "  ok\n");

        if (!fgets(line, sizeof(line), stdin))
            break;

        int len = strlen(line);
        while (len > 0 && (line[len-1] == '\n' || line[len-1] == '\r'))
            line[--len] = '\0';

        vm->tib_len = len;
        memcpy(vm->tib, line, len + 1);
        vm->tib_pos = 0;
        vm_interpret_tib(vm);
    }
}

/* === VM Lifecycle === */

vm_t *vm_create(void) {
    vm_t *vm = calloc(1, sizeof(vm_t));
    if (!vm) {
        fprintf(stderr, "Out of memory\n");
        exit(1);
    }

    /* Init stacks */
    vm->sp = vm->dstack + DSTACK_SIZE;
    vm->rsp = vm->rstack + RSTACK_SIZE;

    /* Init state */
    vm->state = 0;
    vm->base = 10;
    vm->here = 0;
    vm->latest = -1;
    vm->dict_count = 0;
    vm->running = true;
    vm->out = stdout;
    vm->input_depth = 0;
    vm->loaded_count = 0;

    /* Register all C primitives */
    prims_init(vm);
    io_init(vm);

    /* Align HERE after primitive registration */
    vm->here = vm_align(vm->here);

    return vm;
}

void vm_destroy(vm_t *vm) {
    /* Close any open files */
    for (int i = 0; i < MAX_FILES; i++) {
        if (vm->files[i]) {
            fclose(vm->files[i]);
            vm->files[i] = NULL;
        }
    }
    /* Free loaded file names */
    for (int i = 0; i < vm->loaded_count; i++) {
        free(vm->loaded_files[i]);
    }
    free(vm);
}
