# Log Analyzer

Parse and summarize application logs.

## Features

- Read log files line by line
- Pattern matching and extraction
- Aggregate statistics into SQLite
- Generate HTML reports with charts
- Filter by date range, level, pattern

## Usage

```bash
./fifth examples/log-analyzer/main.fs sample.log
# Generates report.html
```

## Structure

```
log-analyzer/
├── main.fs          # Entry point
├── parser.fs        # Log parsing logic
├── report.fs        # Report generation
├── sample.log       # Example log file
└── logs.db          # Aggregated data
```

## Log Format

Expects standard format:
```
2024-01-15 10:30:45 [INFO] Message here
2024-01-15 10:30:46 [ERROR] Something failed
```
