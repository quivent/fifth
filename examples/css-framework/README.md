# CSS Framework Generator

Generate a modular CSS framework with design tokens, utility classes, and component styles using Fifth.

## Features

- **Design Tokens**: Colors, spacing, typography as CSS custom properties
- **Utility Classes**: Flexbox, grid, spacing, typography utilities
- **Component Styles**: Cards, buttons, forms, navigation
- **Responsive Breakpoints**: Mobile-first media queries
- **Theme Support**: Light/dark mode via CSS variables

## Usage

```bash
cd examples/css-framework
../../fifth main.fs
open /tmp/framework.css
```

## Output

Generates a complete CSS framework to `/tmp/framework.css` with:

```css
/* Design Tokens */
:root {
  --color-primary: #6366f1;
  --space-1: 0.25rem;
  --font-sans: 'Inter', sans-serif;
  ...
}

/* Utility Classes */
.flex { display: flex; }
.grid { display: grid; }
.p-4 { padding: var(--space-4); }
...

/* Components */
.btn { ... }
.card { ... }
.form-input { ... }
```

## Architecture

```
css-framework/
├── README.md      # This file
└── main.fs        # CSS generation logic
```

### Key Concepts

1. **Composable Generation**: Each section (tokens, utilities, components) is a separate word
2. **Parameterized Output**: Scale values and breakpoints defined as data
3. **Buffer Pattern**: Uses str-reset/str+ for safe string building
4. **File Output**: Writes directly to CSS file

## Customization

Edit `main.fs` to:

- Change color palette in `emit-colors`
- Adjust spacing scale in `emit-spacing`
- Add/remove utility classes
- Modify component styles
- Add new breakpoints

## Example Customization

```forth
\ Add a new color
: emit-colors
  ...
  s" --color-brand: #ff6b6b;" css-line
  ...
;
```

## Why Fifth?

- **No templating engine needed**: Forth words compose naturally
- **Predictable output**: What you write is what you get
- **Fast generation**: Regenerate framework instantly
- **Self-documenting**: Code structure mirrors CSS structure
