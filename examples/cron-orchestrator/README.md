# Cron Job Orchestrator

Manage scheduled tasks with dependencies and monitoring.

## Features

- Define jobs as Forth words
- Track execution in SQLite
- Handle job dependencies
- Retry on failure
- Generate status reports
- Alerting on failures

## Usage

```bash
# Run all due jobs
./fifth examples/cron-orchestrator/main.fs run

# Run specific job
./fifth examples/cron-orchestrator/main.fs run backup

# Check status
./fifth examples/cron-orchestrator/main.fs status

# View history
./fifth examples/cron-orchestrator/main.fs history
```

## Structure

```
cron-orchestrator/
├── main.fs          # Entry point
├── jobs.fs          # Job definitions
├── scheduler.fs     # Scheduling logic
├── jobs.db          # Execution history
└── output/
    └── status.html
```

## Job Definition

```forth
: job-backup ( -- success )
  s" Backing up database..." type cr
  s" tar -czf backup.tar.gz data/" system
  0= ;  \ return success flag

: job-cleanup ( -- success )
  s" Cleaning old files..." type cr
  s" find /tmp -mtime +7 -delete" system
  0= ;
```

## Cron Integration

```cron
# Run orchestrator every minute
* * * * * /path/to/fifth cron-orchestrator/main.fs run
```
