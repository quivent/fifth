# Fifth Examples Showcase

A visual gallery showcasing all Fifth example applications.

## View the Showcase

Open `index.html` in a browser:
```bash
open index.html
# or
firefox index.html
```

## Generate with Fifth

The showcase can be regenerated using Fifth itself:
```bash
cd examples/showcase
../../fifth generate.fs
```

This demonstrates Fifth's HTML generation capabilities by building the showcase page.

## Structure

```
showcase/
├── README.md      # This file
├── index.html     # Static showcase page
└── generate.fs    # Fifth script to generate the page
```

## Categories

The showcase organizes 23 examples into 8 categories:

| Category | Examples | Theme |
|----------|----------|-------|
| Web & Reports | 3 | HTML generation, dashboards |
| Data Processing | 3 | ETL, parsing, migrations |
| System Administration | 3 | Config, monitoring, deploy |
| Developer Tools | 4 | Code generation, scaffolding, CSS |
| Domain-Specific | 4 | Finance, quizzes, recipes |
| Embedded & Constrained | 2 | IoT, kiosk displays |
| Integration Patterns | 3 | Webhooks, cron, API clients |
| Agentic Coding | 1 | AI-powered coding assistant |

## Design

The showcase uses:
- Modern CSS (Grid, Flexbox, CSS Variables)
- Dark theme optimized for developer ergonomics
- Responsive layout for all screen sizes
- Card-based UI with hover effects
- Category color coding
- Google Fonts (Inter)

## Customization

To modify the showcase:

1. **Edit `generate.fs`** - Change content, add/remove examples
2. **Run `../../fifth generate.fs`** - Regenerate HTML
3. **Or edit `index.html` directly** - For quick CSS/layout changes

## Screenshot

```
┌─────────────────────────────────────────────────────────────┐
│                    FIFTH EXAMPLES                           │
│    A curated collection of practical applications...        │
│                                                             │
│         22            7            100%                     │
│      Examples     Categories       Forth                    │
├─────────────────────────────────────────────────────────────┤
│  🌐 Web & Report Generation                    3 examples   │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐        │
│  │ Static Site  │ │  Dashboard   │ │   Invoice    │        │
│  │  Generator   │ │  Generator   │ │   System     │        │
│  └──────────────┘ └──────────────┘ └──────────────┘        │
│                                                             │
│  🔧 Data Processing                            3 examples   │
│  ...                                                        │
└─────────────────────────────────────────────────────────────┘
```
