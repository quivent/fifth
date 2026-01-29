/*
 * seed.c - Minimal Forth seed for Fifth metacompilation
 *
 * ~200 lines of C. The ONLY non-Forth code in the system.
 * Everything else bootstraps from this.
 *
 * Compile: cc -o seed seed.c
 * Usage:   ./seed meta.fs → generates fifth binary
 */

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <unistd.h>
#include <sys/syscall.h>

/* Configuration */
#define STACK_SIZE  1024
#define RSTACK_SIZE 1024
#define MEMORY_SIZE 65536
#define DICT_SIZE   4096

/* Stacks */
int64_t stack[STACK_SIZE];
int64_t rstack[RSTACK_SIZE];
int sp = 0;    /* Stack pointer */
int rp = 0;    /* Return stack pointer */

/* Memory */
uint8_t memory[MEMORY_SIZE];
int here = 0;  /* Next free byte */

/* Dictionary entry */
typedef struct {
    char name[32];
    int  code;     /* Primitive number or address */
    int  flags;    /* 0=primitive, 1=compiled */
} Entry;

Entry dict[DICT_SIZE];
int dict_count = 0;

/* Primitives */
enum {
    P_EXIT, P_LIT, P_FETCH, P_STORE, P_CFETCH, P_CSTORE,
    P_ADD, P_SUB, P_MUL, P_DIV, P_AND, P_OR, P_XOR, P_LESS,
    P_EMIT, P_KEY, P_SYSCALL, P_BRANCH, P_ZBRANCH, P_EXECUTE,
    P_DUP, P_DROP, P_SWAP, P_OVER, P_ROT,
    P_TOR, P_FROMR, P_RFETCH,
    P_HERE, P_COMMA, P_CCOMMA, P_ALLOT,
    P_COUNT
};

/* Stack operations */
#define PUSH(x) (stack[sp++] = (x))
#define POP()   (stack[--sp])
#define TOS     (stack[sp-1])
#define NOS     (stack[sp-2])

#define RPUSH(x) (rstack[rp++] = (x))
#define RPOP()   (rstack[--rp])

/* Find word in dictionary */
int find(const char *name) {
    for (int i = dict_count - 1; i >= 0; i--) {
        if (strcmp(dict[i].name, name) == 0) return i;
    }
    return -1;
}

/* Add word to dictionary */
void add_word(const char *name, int code, int flags) {
    strcpy(dict[dict_count].name, name);
    dict[dict_count].code = code;
    dict[dict_count].flags = flags;
    dict_count++;
}

/* Execute a primitive */
void primitive(int p) {
    int64_t a, b, n;
    switch (p) {
        case P_DUP:    PUSH(TOS); break;
        case P_DROP:   sp--; break;
        case P_SWAP:   a = TOS; TOS = NOS; NOS = a; break;
        case P_OVER:   PUSH(NOS); break;
        case P_ROT:    a = stack[sp-3]; stack[sp-3] = NOS; NOS = TOS; TOS = a; break;

        case P_FETCH:  TOS = *(int64_t*)(memory + TOS); break;
        case P_STORE:  a = POP(); *(int64_t*)(memory + TOS) = a; sp--; break;
        case P_CFETCH: TOS = memory[TOS]; break;
        case P_CSTORE: a = POP(); memory[TOS] = a; sp--; break;

        case P_ADD:    NOS += TOS; sp--; break;
        case P_SUB:    NOS -= TOS; sp--; break;
        case P_MUL:    NOS *= TOS; sp--; break;
        case P_DIV:    NOS /= TOS; sp--; break;
        case P_AND:    NOS &= TOS; sp--; break;
        case P_OR:     NOS |= TOS; sp--; break;
        case P_XOR:    NOS ^= TOS; sp--; break;
        case P_LESS:   NOS = (NOS < TOS) ? -1 : 0; sp--; break;

        case P_EMIT:   putchar(POP()); fflush(stdout); break;
        case P_KEY:    PUSH(getchar()); break;

        case P_TOR:    RPUSH(POP()); break;
        case P_FROMR:  PUSH(RPOP()); break;
        case P_RFETCH: PUSH(rstack[rp-1]); break;

        case P_HERE:   PUSH(here); break;
        case P_COMMA:  *(int64_t*)(memory + here) = POP(); here += 8; break;
        case P_CCOMMA: memory[here++] = POP(); break;
        case P_ALLOT:  here += POP(); break;

        case P_SYSCALL:
            n = POP();  /* syscall number */
            /* Simplified: just handle write(1, buf, len) for now */
            if (n == SYS_write) {
                int64_t len = POP();
                int64_t buf = POP();
                int64_t fd = POP();
                PUSH(write(fd, memory + buf, len));
            }
            break;

        case P_EXECUTE:
            a = POP();
            if (dict[a].flags == 0) {
                primitive(dict[a].code);
            }
            /* TODO: compiled words */
            break;
    }
}

/* Initialize primitives */
void init_dict(void) {
    add_word("exit",    P_EXIT,    0);
    add_word("lit",     P_LIT,     0);
    add_word("@",       P_FETCH,   0);
    add_word("!",       P_STORE,   0);
    add_word("c@",      P_CFETCH,  0);
    add_word("c!",      P_CSTORE,  0);
    add_word("+",       P_ADD,     0);
    add_word("-",       P_SUB,     0);
    add_word("*",       P_MUL,     0);
    add_word("/",       P_DIV,     0);
    add_word("and",     P_AND,     0);
    add_word("or",      P_OR,      0);
    add_word("xor",     P_XOR,     0);
    add_word("<",       P_LESS,    0);
    add_word("emit",    P_EMIT,    0);
    add_word("key",     P_KEY,     0);
    add_word("syscall", P_SYSCALL, 0);
    add_word("branch",  P_BRANCH,  0);
    add_word("0branch", P_ZBRANCH, 0);
    add_word("execute", P_EXECUTE, 0);
    add_word("dup",     P_DUP,     0);
    add_word("drop",    P_DROP,    0);
    add_word("swap",    P_SWAP,    0);
    add_word("over",    P_OVER,    0);
    add_word("rot",     P_ROT,     0);
    add_word(">r",      P_TOR,     0);
    add_word("r>",      P_FROMR,   0);
    add_word("r@",      P_RFETCH,  0);
    add_word("here",    P_HERE,    0);
    add_word(",",       P_COMMA,   0);
    add_word("c,",      P_CCOMMA,  0);
    add_word("allot",   P_ALLOT,   0);
}

/* Simple tokenizer */
char token[256];
FILE *input;

int next_token(void) {
    int c, i = 0;
    /* Skip whitespace */
    while ((c = fgetc(input)) != EOF && (c == ' ' || c == '\n' || c == '\t'));
    if (c == EOF) return 0;
    /* Skip comments */
    if (c == '\\') {
        while ((c = fgetc(input)) != EOF && c != '\n');
        return next_token();
    }
    /* Read token */
    token[i++] = c;
    while ((c = fgetc(input)) != EOF && c != ' ' && c != '\n' && c != '\t') {
        token[i++] = c;
    }
    token[i] = 0;
    return 1;
}

/* Interpret */
void interpret(void) {
    while (next_token()) {
        int idx = find(token);
        if (idx >= 0) {
            /* Execute word */
            if (dict[idx].flags == 0) {
                primitive(dict[idx].code);
            }
        } else {
            /* Try as number */
            char *end;
            int64_t n = strtoll(token, &end, 0);
            if (*end == 0) {
                PUSH(n);
            } else {
                fprintf(stderr, "Unknown: %s\n", token);
            }
        }
    }
}

int main(int argc, char **argv) {
    init_dict();

    if (argc > 1) {
        input = fopen(argv[1], "r");
        if (!input) {
            perror(argv[1]);
            return 1;
        }
    } else {
        input = stdin;
    }

    interpret();

    if (input != stdin) fclose(input);
    return 0;
}
