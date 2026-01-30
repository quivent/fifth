/* spawn.c - Native concurrency for Fifth
 *
 * Adds spawn/wait primitives using pthreads.
 * Each spawned word runs in its own thread with its own stack.
 */

#include "fifth.h"
#include <pthread.h>

#define MAX_THREADS 64

typedef struct {
    pthread_t thread;
    vm_t *vm;           /* Each thread gets its own VM */
    int xt;             /* Word to execute */
    bool active;
    bool done;
    cell_t result;      /* TOS after execution */
} thread_slot_t;

static thread_slot_t threads[MAX_THREADS];
static pthread_mutex_t thread_mutex = PTHREAD_MUTEX_INITIALIZER;
static int next_thread_id = 0;

/* Thread entry point */
static void *thread_runner(void *arg) {
    thread_slot_t *slot = (thread_slot_t *)arg;

    /* Execute the word */
    vm_execute(slot->vm, slot->xt);

    /* Capture result (TOS) */
    if (depth(slot->vm) > 0) {
        slot->result = pop(slot->vm);
    } else {
        slot->result = 0;
    }

    slot->done = true;
    return NULL;
}

/* Clone a VM for a new thread */
static vm_t *vm_clone(vm_t *parent) {
    vm_t *child = calloc(1, sizeof(vm_t));
    if (!child) return NULL;

    /* Copy dictionary and memory */
    memcpy(child->dict, parent->dict, sizeof(parent->dict));
    child->dict_count = parent->dict_count;
    child->latest = parent->latest;
    memcpy(child->mem, parent->mem, parent->here);
    child->here = parent->here;

    /* Fresh stacks */
    child->sp = child->dstack + DSTACK_SIZE;
    child->rsp = child->rstack + RSTACK_SIZE;

    /* Copy state */
    child->base = parent->base;
    child->running = true;
    child->out = parent->out;

    /* Copy cached XTs */
    child->xt_lit = parent->xt_lit;
    child->xt_branch = parent->xt_branch;
    child->xt_0branch = parent->xt_0branch;
    child->xt_exit = parent->xt_exit;
    child->xt_slit = parent->xt_slit;
    child->xt_do = parent->xt_do;
    child->xt_qdo = parent->xt_qdo;
    child->xt_loop = parent->xt_loop;
    child->xt_ploop = parent->xt_ploop;
    child->xt_does = parent->xt_does;

    return child;
}

/* SPAWN ( xt -- thread-id )
 * Execute xt in a new thread, return thread ID
 */
static void p_spawn(vm_t *vm) {
    int xt = (int)pop(vm);

    pthread_mutex_lock(&thread_mutex);

    /* Find free slot */
    int id = -1;
    for (int i = 0; i < MAX_THREADS; i++) {
        int idx = (next_thread_id + i) % MAX_THREADS;
        if (!threads[idx].active) {
            id = idx;
            next_thread_id = (idx + 1) % MAX_THREADS;
            break;
        }
    }

    if (id < 0) {
        pthread_mutex_unlock(&thread_mutex);
        fprintf(stderr, "SPAWN: No free thread slots\n");
        push(vm, -1);
        return;
    }

    /* Set up thread */
    threads[id].vm = vm_clone(vm);
    threads[id].xt = xt;
    threads[id].active = true;
    threads[id].done = false;
    threads[id].result = 0;

    /* Copy arguments from parent stack to child */
    /* (For now, child starts with empty stack) */

    /* Create thread */
    if (pthread_create(&threads[id].thread, NULL, thread_runner, &threads[id]) != 0) {
        free(threads[id].vm);
        threads[id].active = false;
        pthread_mutex_unlock(&thread_mutex);
        fprintf(stderr, "SPAWN: pthread_create failed\n");
        push(vm, -1);
        return;
    }

    pthread_mutex_unlock(&thread_mutex);
    push(vm, id);
}

/* WAIT ( thread-id -- result )
 * Wait for thread to complete, return its result
 */
static void p_wait(vm_t *vm) {
    int id = (int)pop(vm);

    if (id < 0 || id >= MAX_THREADS || !threads[id].active) {
        fprintf(stderr, "WAIT: Invalid thread ID %d\n", id);
        push(vm, 0);
        return;
    }

    pthread_join(threads[id].thread, NULL);

    cell_t result = threads[id].result;

    /* Cleanup */
    free(threads[id].vm);
    threads[id].active = false;

    push(vm, result);
}

/* WAIT-ALL ( -- )
 * Wait for all spawned threads
 */
static void p_wait_all(vm_t *vm) {
    (void)vm;

    for (int i = 0; i < MAX_THREADS; i++) {
        if (threads[i].active) {
            pthread_join(threads[i].thread, NULL);
            free(threads[i].vm);
            threads[i].active = false;
        }
    }
}

/* THREAD-DONE? ( thread-id -- flag )
 * Check if thread is done without blocking
 */
static void p_thread_done(vm_t *vm) {
    int id = (int)pop(vm);

    if (id < 0 || id >= MAX_THREADS || !threads[id].active) {
        push(vm, -1);  /* Invalid = done */
        return;
    }

    push(vm, threads[id].done ? -1 : 0);
}

/* NPROC ( -- n )
 * Return number of available processors
 */
static void p_nproc(vm_t *vm) {
    long n = sysconf(_SC_NPROCESSORS_ONLN);
    push(vm, n > 0 ? n : 1);
}

/* Initialize spawn primitives */
void spawn_init(vm_t *vm) {
    vm_add_prim(vm, "spawn", p_spawn, false);
    vm_add_prim(vm, "wait", p_wait, false);
    vm_add_prim(vm, "wait-all", p_wait_all, false);
    vm_add_prim(vm, "thread-done?", p_thread_done, false);
    vm_add_prim(vm, "nproc", p_nproc, false);
}
