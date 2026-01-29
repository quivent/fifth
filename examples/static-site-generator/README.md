# Static Site Generator

Generate a blog or documentation site from markdown files.

## Features

- Scan posts directory for markdown files
- Convert to HTML via pandoc
- Apply consistent templates and styling
- Generate navigation and index pages
- Output to dist/ directory

## Usage

```bash
./fifth examples/static-site-generator/main.fs
```

## Structure

```
static-site-generator/
├── main.fs          # Entry point
├── templates.fs     # HTML templates
├── posts/           # Source markdown
│   └── example.md
└── dist/            # Generated output
```

## Dependencies

- pandoc (markdown conversion)
