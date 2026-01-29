# Performance

## Runtime Targets (vs gcc -O2)

| Benchmark | C Baseline (ARM64) | Target |
|-----------|-------------------|--------|
| Sieve (8190) | 0.004 ms | 85-110% of C |
| Fibonacci recursive (35) | 1.968 ms | 85-110% of C |
| Matrix multiply (100x100) | 0.465 ms | 85-110% of C |
| Bubble sort (1000) | 0.266 ms | 85-110% of C |

## Agent Workflow Latency

| Component | Current | Target |
|-----------|---------|--------|
| End-to-end workflow | 56.9 ms | 15 ms |
| Stack verification | 0.4 ms | 0.1 ms |
| Pattern query | 1.2 ms | 0.3 ms |
| Code generation | 52.3 ms | 12 ms |

Code generation dominates at 92% of workflow time. Highest-leverage optimization target.

## Bottleneck Profile

| Component | Time | Share |
|-----------|------|-------|
| Code generation | 52.3 ms | 92% |
| Spec validation | 4.2 ms | 7% |
| Stack verification | 0.4 ms | 1% |

Code generation breakdown: JSON parsing 12.4 ms, template lookup 8.7 ms, string formatting 15.2 ms, validation 16.0 ms.

## Optimization Multipliers

| Category | vs C | vs Python | vs Go |
|----------|------|-----------|-------|
| Runtime speed | 0.85-1.1x | 20-100x faster | 1.5-3x faster |
| Compilation time | 1-5x faster | N/A | 2-8x faster |
| Binary size | Comparable | 400-2000x smaller | 20-40x smaller |

Primary differentiator: agent iteration speed (20-100x over traditional workflows), not runtime execution.

## Tuning Path

1. **HashMap template lookup**: 8.7 ms → 0.1 ms
2. **Pre-allocated string buffers**: 15.2 ms → 5 ms
3. **LRU cache for pattern queries**: 1.2 ms → 0.3 ms
4. **SIMD JSON parsing**: 12.4 ms → 8 ms
5. **JIT-compiled templates**: further gains

## Measurement

```bash
cargo bench --bench inference_bench
python3 benchmarks/run_benchmarks.py --all
```
