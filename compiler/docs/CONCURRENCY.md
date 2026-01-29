# Concurrency

## Primitives

Five words implemented as a thin C runtime layer over pthreads:

```forth
spawn    ( xt -- thread-id )    \ Create OS thread executing xt
channel  ( size -- chan )        \ Create bounded message queue
send     ( value chan -- )       \ Send to channel (blocks if full)
recv     ( chan -- value )       \ Receive from channel (blocks if empty)
join     ( thread-id -- )        \ Wait for thread completion
```

Binary impact: +15 KB. Channels use bounded ring buffers with mutex/condvar synchronization.

### Measured Latency

| Operation | Latency |
|-----------|---------|
| spawn | 10.9 us |
| send/recv (unlocked) | 12 ns |
| send/recv (contended) | ~500 ns |
| Channel throughput | 82.4M ops/sec |

## Database Architecture

SQLite degrades under concurrent writes (database-level locking). For multi-agent workloads:

| Agents | Recommended |
|--------|-------------|
| 1 | SQLite |
| 2-10 | SQLite + queue or PostgreSQL |
| 10+ | PostgreSQL |

Hybrid approach: SQLite for pattern library (read-only, embedded), PostgreSQL for agent results and provenance (concurrent writes, MVCC).

## Multi-Agent Orchestration

Fast Forth is the worker, not the coordinator. Each instance runs single-threaded, optimized for iteration speed.

```
Coordinator → Fast Forth Agent 1 →
            → Fast Forth Agent 2 → PostgreSQL
            → Fast Forth Agent N →
```

Pure Forth orchestration is also possible using the concurrency primitives:

```forth
100 channel constant work-queue
100 channel constant result-queue

: agent-worker ( -- )
  begin work-queue recv dup 0= if drop exit then
    validate-spec generate-code verify-stack-effect
    result-queue send again ;

10 0 do ['] agent-worker spawn drop loop
```

### Orchestrator Tradeoffs

| | Pure Forth | Go |
|---|-----------|-----|
| Binary size | 2.6 MB | 4.1 MB |
| Memory (10 agents) | 60 MB | 10.7 MB |
| Channel throughput | 82M ops/s | ~50M ops/s |
| Dev time | 2-3 weeks | 2-3 days |

Both achieve the same throughput. Bottleneck is worker speed (10s/spec), not orchestration overhead.
