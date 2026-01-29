# Dashboard Generator

Pull metrics from multiple sources and render a single-page dashboard.

## Features

- Query SQLite for historical data
- Fetch API endpoints via curl
- Generate self-contained HTML
- Embed charts via CDN (Chart.js)
- Auto-refresh capability

## Usage

```bash
./fifth examples/dashboard-generator/main.fs
open /tmp/dashboard.html
```

## Structure

```
dashboard-generator/
├── main.fs          # Entry point
├── widgets.fs       # Dashboard widgets
├── data.fs          # Data fetching
└── metrics.db       # Sample database
```

## Dependencies

- curl (API fetching)
- sqlite3 (local data)
