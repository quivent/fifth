# CSV Transformer

Convert between data formats and enrich CSV data.

## Features

- Parse CSV with configurable delimiter
- Transform fields with stack operations
- Join with SQLite lookup tables
- Output to various formats (CSV, JSON, HTML)
- Field mapping and filtering

## Usage

```bash
./fifth examples/csv-transformer/main.fs input.csv output.csv
```

## Structure

```
csv-transformer/
├── main.fs          # Entry point
├── parser.fs        # CSV parsing
├── transform.fs     # Field transformations
├── sample.csv       # Example input
└── lookups.db       # Lookup tables
```

## Transformations

Define transformations as Forth words:
```forth
: uppercase-name ( row -- row' )
  0 field@ upcase 0 field! ;
```
