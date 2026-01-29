/* main.c - Fifth Engine Entry Point
 *
 * Usage:
 *   fifth                      Interactive REPL
 *   fifth file.fs              Load and execute file
 *   fifth file.fs -e "code"    Load file, then execute code
 *   fifth -e "code"            Execute code
 */

#include "fifth.h"
#include <libgen.h>
#include <limits.h>
#include <unistd.h>

/* Find the boot directory relative to the executable */
static void load_boot(vm_t *vm, const char *argv0) {
    char path[PATH_MAX];

    /* Try relative to executable: ../boot/core.fs or ./boot/core.fs */
    /* First: try alongside the binary */
    char exe_dir[PATH_MAX];
    strncpy(exe_dir, argv0, PATH_MAX - 1);
    exe_dir[PATH_MAX - 1] = '\0';
    char *dir = dirname(exe_dir);

    snprintf(path, sizeof(path), "%s/boot/core.fs", dir);
    if (access(path, R_OK) == 0) {
        vm_load_file(vm, path);
        return;
    }

    /* Try: same directory */
    snprintf(path, sizeof(path), "%s/../boot/core.fs", dir);
    if (access(path, R_OK) == 0) {
        vm_load_file(vm, path);
        return;
    }

    /* Try ~/fifth/engine/boot/core.fs */
    const char *home = getenv("HOME");
    if (home) {
        snprintf(path, sizeof(path), "%s/fifth/engine/boot/core.fs", home);
        if (access(path, R_OK) == 0) {
            vm_load_file(vm, path);
            return;
        }
    }

    /* No boot file found -- continue without it */
    fprintf(stderr, "Note: boot/core.fs not found (standalone mode)\n");
}

int main(int argc, char **argv) {
    vm_t *vm = vm_create();

    /* Load bootstrap */
    load_boot(vm, argv[0]);

    /* Process arguments */
    bool interactive = true;
    for (int i = 1; i < argc && vm->running; i++) {
        if (strcmp(argv[i], "-e") == 0 && i + 1 < argc) {
            i++;
            vm_interpret_line(vm, argv[i]);
            interactive = false;
        } else if (strcmp(argv[i], "--help") == 0 || strcmp(argv[i], "-h") == 0) {
            printf("Fifth - A minimal Forth engine\n");
            printf("Usage: fifth [file.fs ...] [-e \"code\"]\n");
            printf("\n");
            printf("  file.fs    Load and execute Forth source file(s)\n");
            printf("  -e code    Execute Forth code from command line\n");
            printf("  -h         Show this help\n");
            printf("\n");
            printf("With no arguments, starts interactive REPL.\n");
            vm->running = false;
            interactive = false;
        } else {
            /* Treat as filename */
            char path[PATH_MAX];
            if (argv[i][0] == '~') {
                const char *home = getenv("HOME");
                if (home)
                    snprintf(path, sizeof(path), "%s%s", home, argv[i] + 1);
                else
                    strncpy(path, argv[i], sizeof(path) - 1);
            } else {
                strncpy(path, argv[i], sizeof(path) - 1);
            }
            path[sizeof(path) - 1] = '\0';
            vm_load_file(vm, path);
            interactive = false;
        }
    }

    /* Interactive REPL if no files were loaded */
    if (interactive && vm->running) {
        printf("Fifth Engine v0.1.0\n");
        printf("Type 'bye' to exit.\n");
        vm_repl(vm);
    }

    int code = vm->exit_code;
    vm_destroy(vm);
    return code;
}
