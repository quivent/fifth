# Invoice System

Generate PDF invoices from database records.

## Features

- Query orders from SQLite
- Generate HTML with print-optimized CSS
- Convert to PDF via wkhtmltopdf
- Batch invoice generation
- Invoice numbering and tracking

## Usage

```bash
./fifth examples/invoice-system/main.fs
# Generates invoices in output/
```

## Structure

```
invoice-system/
├── main.fs          # Entry point
├── templates.fs     # Invoice templates
├── data.fs          # Database queries
├── orders.db        # Sample database
└── output/          # Generated PDFs
```

## Dependencies

- wkhtmltopdf or weasyprint (PDF generation)
- sqlite3 (order data)
