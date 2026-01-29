# Server Health Dashboard

Aggregate system metrics into a self-contained status page.

## Features

- Collect metrics via shell commands (df, free, uptime, etc.)
- Store historical data in SQLite
- Generate HTML dashboard
- Configurable refresh interval
- Alert thresholds

## Usage

```bash
# Generate dashboard once
./fifth examples/server-health/main.fs

# Continuous monitoring (via cron)
*/5 * * * * /path/to/fifth examples/server-health/main.fs
```

## Structure

```
server-health/
├── main.fs          # Entry point
├── collectors.fs    # Metric collection
├── dashboard.fs     # HTML generation
├── metrics.db       # Historical data
└── output/
    └── health.html  # Generated dashboard
```

## Metrics Collected

- Disk usage (df)
- Memory usage (free)
- CPU load (uptime)
- Process count (ps)
- Network connections (netstat)
