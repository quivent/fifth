# Code Generator

Generate boilerplate code from specifications.

## Features

- Read schema definitions (JSON/YAML via jq/yq)
- Generate models, routes, tests
- Configurable templates
- Multiple language targets
- Dry-run mode

## Usage

```bash
# Generate from schema
./fifth examples/code-generator/main.fs schema.json

# Generate specific component
./fifth examples/code-generator/main.fs schema.json --only models
```

## Structure

```
code-generator/
├── main.fs          # Entry point
├── templates/       # Code templates
│   ├── model.fs
│   ├── route.fs
│   └── test.fs
├── schema.json      # Example schema
└── output/          # Generated code
```

## Schema Format

```json
{
  "entities": [
    {
      "name": "User",
      "fields": [
        {"name": "id", "type": "integer"},
        {"name": "email", "type": "string"}
      ]
    }
  ]
}
```
