# Fifth Brand Guide

A minimal, purposeful identity for stack-based programming.

---

## Philosophy

**Less, but better.**

Fifth's brand reflects its technical philosophy: no unnecessary complexity, no wasted cycles, no dependencies. Every element serves a purpose.

The identity is built on three principles:

1. **Precision** — Every detail is intentional
2. **Restraint** — Add nothing superfluous
3. **Confidence** — Let the work speak

---

## Logo

### The Mark

The logo is a stylized "V" — the Roman numeral for five. The subtle horizontal line above suggests stack layers, the core data structure in Forth.

```
    ─────────
      \   /
       \ /
        V
```

### Construction

- Primary stroke: 2.5px weight, rounded caps
- Secondary stroke: 2px weight, 30% opacity
- Corner radius: rounded joins
- Clear space: 25% of mark width on all sides

### Usage

| Context | File |
|---------|------|
| Icon only | `logo.svg` |
| With wordmark | `logo-wordmark.svg` |
| Favicon | `favicon.svg` |

### Don'ts

- Don't rotate or skew the mark
- Don't add effects (shadows, gradients, glows)
- Don't change the stroke proportions
- Don't use colors outside the palette

---

## Typography

### Primary: Inter

Clean, neutral, optimized for screens.

| Use | Weight |
|-----|--------|
| Headlines | 600 (Semibold) |
| Body | 400 (Regular) |
| UI labels | 500 (Medium) |
| Captions | 400 (Regular) |

Letter spacing: -0.02em for headlines, default for body.

### Monospace: JetBrains Mono

For code, data, and technical content.

| Use | Weight |
|-----|--------|
| Code blocks | 400 (Regular) |
| Terminal | 400 (Regular) |
| Data/stats | 500 (Medium) |

---

## Color

### Palette

A monochromatic system with a single accent.

#### Grays

| Name | Hex | Use |
|------|-----|-----|
| Black | `#0a0a0a` | Primary background |
| Gray 950 | `#0f0f0f` | Surfaces |
| Gray 900 | `#171717` | Elevated surfaces |
| Gray 800 | `#262626` | Borders |
| Gray 700 | `#404040` | Hover borders |
| Gray 600 | `#525252` | — |
| Gray 500 | `#737373` | Tertiary text |
| Gray 400 | `#a3a3a3` | Secondary text |
| Gray 300 | `#d4d4d4` | — |
| Gray 200 | `#e5e5e5` | — |
| Gray 100 | `#f5f5f5` | Primary text |
| White | `#fafafa` | — |

#### Accent

| Name | Hex | Use |
|------|-----|-----|
| Indigo | `#6366f1` | Links, highlights, interactive elements |
| Indigo (dim) | `rgba(99,102,241,0.15)` | Subtle backgrounds |

### Usage Rules

1. **Dark mode default** — The brand is designed dark-first
2. **One accent** — Indigo only. No other colors
3. **High contrast text** — Gray 100 on Black for readability
4. **Subtle borders** — Gray 800, never lighter

---

## Spacing

Based on a 4px grid.

| Token | Value | Common use |
|-------|-------|------------|
| space-1 | 4px | Tight gaps |
| space-2 | 8px | Icon gaps, small padding |
| space-3 | 12px | Button padding |
| space-4 | 16px | Card padding, section gaps |
| space-6 | 24px | Component padding |
| space-8 | 32px | Section margins |
| space-12 | 48px | Large gaps |
| space-16 | 64px | Section padding |
| space-24 | 96px | Hero padding |

---

## Motion

### Principles

- **Subtle** — Never distract from content
- **Fast** — 200ms default duration
- **Purposeful** — Motion indicates interaction, not decoration

### Tokens

```css
--ease: cubic-bezier(0.4, 0, 0.2, 1);
--duration: 200ms;
```

### Patterns

| Element | Property | Duration |
|---------|----------|----------|
| Hover states | color, background | 200ms |
| Cards | transform, border | 200ms |
| Page transitions | opacity, transform | 300ms |
| Reveals | opacity, transform | 500ms |

---

## Voice

### Tone

- **Direct** — Say what you mean
- **Technical** — Assume competence
- **Humble** — Let results speak

### Examples

| Instead of | Write |
|------------|-------|
| "Fifth is an incredibly powerful and revolutionary..." | "Fifth is a Forth ecosystem." |
| "We're excited to announce..." | "Now available:" |
| "Effortlessly create amazing..." | "Generate HTML from Forth." |

### Punctuation

- Use periods. Not exclamation marks.
- Commas are fine. Em dashes—sparingly.
- No emoji in official communications.

---

## Application

### Website

- Dark background (#0a0a0a)
- Fixed nav with backdrop blur
- Card-based content layout
- Accent on interactive elements only

### Documentation

- Same color system
- Generous whitespace
- Code blocks in JetBrains Mono
- Headers in Inter Semibold

### Terminal/CLI

- Default terminal colors
- No ASCII art
- Terse, informative output

---

## Assets

```
brand/
├── BRAND.md          # This guide
├── logo.svg          # Mark only
├── logo-wordmark.svg # Mark + wordmark
└── favicon.svg       # Favicon with background
```

---

## Summary

| Element | Value |
|---------|-------|
| Primary font | Inter |
| Mono font | JetBrains Mono |
| Background | #0a0a0a |
| Text | #f5f5f5 |
| Accent | #6366f1 |
| Border | #262626 |
| Border radius | 12px (cards), 8px (buttons) |
| Transition | 200ms ease |

---

*Fifth. Stack-based programming for the modern age.*
