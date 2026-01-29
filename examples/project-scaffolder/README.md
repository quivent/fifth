# Project Scaffolder

Create new project structures from templates.

## Features

- Prompt for project parameters
- Generate directory structure
- Render template files with substitutions
- Initialize git repository
- Install dependencies
- Multiple project templates

## Usage

```bash
# Interactive scaffolding
./fifth examples/project-scaffolder/main.fs

# Create from template
./fifth examples/project-scaffolder/main.fs --template webapp --name myproject
```

## Structure

```
project-scaffolder/
├── main.fs              # Entry point
├── templates/           # Project templates
│   ├── webapp/
│   ├── cli/
│   └── library/
└── output/              # Generated projects
```

## Templates

### webapp
Basic web application with HTML, CSS, JS

### cli
Command-line application structure

### library
Reusable library with tests and docs
