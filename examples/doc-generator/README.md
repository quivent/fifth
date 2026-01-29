# Documentation Generator

Extract and format documentation from source code.

## Features

- Parse source files for doc comments
- Cross-reference definitions
- Generate HTML or Markdown output
- Build searchable index
- Multiple output formats

## Usage

```bash
# Generate docs for current directory
./fifth examples/doc-generator/main.fs .

# Generate docs for specific path
./fifth examples/doc-generator/main.fs src/
```

## Structure

```
doc-generator/
├── main.fs          # Entry point
├── parser.fs        # Comment extraction
├── templates.fs     # Output templates
└── output/
    ├── index.html
    └── api/
```

## Doc Comment Format

```forth
\ word-name ( stack-before -- stack-after )
\ Description of what this word does.
\ Can span multiple lines.
```

The generator extracts:
- Word name
- Stack effect
- Description
- Cross-references to other words
