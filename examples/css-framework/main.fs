\ fifth/examples/css-framework/main.fs - CSS Framework Generator
\ Generates a modular CSS framework with design tokens, utilities, and components

require ~/.fifth/lib/str.fs

\ ---------------------------------------------------------------------------
\ Output Setup
\ ---------------------------------------------------------------------------

variable css-fid

: css-open  ( -- )
  s" /tmp/framework.css" w/o create-file throw css-fid ! ;

: css-close ( -- )
  css-fid @ close-file throw ;

: css-emit  ( addr u -- )
  css-fid @ write-file throw ;

: css-nl    ( -- )
  s\" \n" css-emit ;

: css-line  ( addr u -- )
  css-emit css-nl ;

: css-comment ( addr u -- )
  s" /* " css-emit css-emit s"  */" css-line ;

: css-section ( addr u -- )
  css-nl
  s" /* ========================================" css-line
  s"    " css-emit css-line
  s"    ======================================== */" css-line
  css-nl ;

\ ---------------------------------------------------------------------------
\ Design Tokens - Colors
\ ---------------------------------------------------------------------------

: emit-color-tokens ( -- )
  s" Colors" css-section
  s" :root {" css-line

  \ Primary palette
  s"   /* Primary */" css-line
  s"   --color-primary-50: #eef2ff;" css-line
  s"   --color-primary-100: #e0e7ff;" css-line
  s"   --color-primary-200: #c7d2fe;" css-line
  s"   --color-primary-300: #a5b4fc;" css-line
  s"   --color-primary-400: #818cf8;" css-line
  s"   --color-primary-500: #6366f1;" css-line
  s"   --color-primary-600: #4f46e5;" css-line
  s"   --color-primary-700: #4338ca;" css-line
  s"   --color-primary-800: #3730a3;" css-line
  s"   --color-primary-900: #312e81;" css-line
  css-nl

  \ Neutral palette
  s"   /* Neutral */" css-line
  s"   --color-neutral-50: #fafafa;" css-line
  s"   --color-neutral-100: #f4f4f5;" css-line
  s"   --color-neutral-200: #e4e4e7;" css-line
  s"   --color-neutral-300: #d4d4d8;" css-line
  s"   --color-neutral-400: #a1a1aa;" css-line
  s"   --color-neutral-500: #71717a;" css-line
  s"   --color-neutral-600: #52525b;" css-line
  s"   --color-neutral-700: #3f3f46;" css-line
  s"   --color-neutral-800: #27272a;" css-line
  s"   --color-neutral-900: #18181b;" css-line
  s"   --color-neutral-950: #09090b;" css-line
  css-nl

  \ Semantic colors
  s"   /* Semantic */" css-line
  s"   --color-success: #22c55e;" css-line
  s"   --color-warning: #f59e0b;" css-line
  s"   --color-error: #ef4444;" css-line
  s"   --color-info: #3b82f6;" css-line
  css-nl

  \ Background/foreground
  s"   /* Theme */" css-line
  s"   --bg: var(--color-neutral-950);" css-line
  s"   --bg-surface: var(--color-neutral-900);" css-line
  s"   --bg-elevated: var(--color-neutral-800);" css-line
  s"   --fg: var(--color-neutral-100);" css-line
  s"   --fg-muted: var(--color-neutral-400);" css-line
  s"   --border: var(--color-neutral-700);" css-line

  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Design Tokens - Spacing
\ ---------------------------------------------------------------------------

: emit-spacing-tokens ( -- )
  s" Spacing" css-section
  s" :root {" css-line
  s"   --space-0: 0;" css-line
  s"   --space-px: 1px;" css-line
  s"   --space-0-5: 0.125rem;" css-line
  s"   --space-1: 0.25rem;" css-line
  s"   --space-1-5: 0.375rem;" css-line
  s"   --space-2: 0.5rem;" css-line
  s"   --space-2-5: 0.625rem;" css-line
  s"   --space-3: 0.75rem;" css-line
  s"   --space-3-5: 0.875rem;" css-line
  s"   --space-4: 1rem;" css-line
  s"   --space-5: 1.25rem;" css-line
  s"   --space-6: 1.5rem;" css-line
  s"   --space-7: 1.75rem;" css-line
  s"   --space-8: 2rem;" css-line
  s"   --space-9: 2.25rem;" css-line
  s"   --space-10: 2.5rem;" css-line
  s"   --space-11: 2.75rem;" css-line
  s"   --space-12: 3rem;" css-line
  s"   --space-14: 3.5rem;" css-line
  s"   --space-16: 4rem;" css-line
  s"   --space-20: 5rem;" css-line
  s"   --space-24: 6rem;" css-line
  s"   --space-28: 7rem;" css-line
  s"   --space-32: 8rem;" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Design Tokens - Typography
\ ---------------------------------------------------------------------------

: emit-typography-tokens ( -- )
  s" Typography" css-section
  s" :root {" css-line

  \ Font families
  s"   /* Families */" css-line
  s"   --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;" css-line
  s"   --font-mono: 'JetBrains Mono', 'Fira Code', monospace;" css-line
  css-nl

  \ Font sizes
  s"   /* Sizes */" css-line
  s"   --text-xs: 0.75rem;" css-line
  s"   --text-sm: 0.875rem;" css-line
  s"   --text-base: 1rem;" css-line
  s"   --text-lg: 1.125rem;" css-line
  s"   --text-xl: 1.25rem;" css-line
  s"   --text-2xl: 1.5rem;" css-line
  s"   --text-3xl: 1.875rem;" css-line
  s"   --text-4xl: 2.25rem;" css-line
  s"   --text-5xl: 3rem;" css-line
  css-nl

  \ Font weights
  s"   /* Weights */" css-line
  s"   --font-thin: 100;" css-line
  s"   --font-light: 300;" css-line
  s"   --font-normal: 400;" css-line
  s"   --font-medium: 500;" css-line
  s"   --font-semibold: 600;" css-line
  s"   --font-bold: 700;" css-line
  css-nl

  \ Line heights
  s"   /* Line Heights */" css-line
  s"   --leading-none: 1;" css-line
  s"   --leading-tight: 1.25;" css-line
  s"   --leading-snug: 1.375;" css-line
  s"   --leading-normal: 1.5;" css-line
  s"   --leading-relaxed: 1.625;" css-line
  s"   --leading-loose: 2;" css-line

  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Design Tokens - Effects
\ ---------------------------------------------------------------------------

: emit-effect-tokens ( -- )
  s" Effects" css-section
  s" :root {" css-line

  \ Border radius
  s"   /* Radius */" css-line
  s"   --radius-none: 0;" css-line
  s"   --radius-sm: 0.125rem;" css-line
  s"   --radius-md: 0.375rem;" css-line
  s"   --radius-lg: 0.5rem;" css-line
  s"   --radius-xl: 0.75rem;" css-line
  s"   --radius-2xl: 1rem;" css-line
  s"   --radius-full: 9999px;" css-line
  css-nl

  \ Shadows
  s"   /* Shadows */" css-line
  s"   --shadow-sm: 0 1px 2px rgba(0,0,0,0.05);" css-line
  s"   --shadow-md: 0 4px 6px -1px rgba(0,0,0,0.1);" css-line
  s"   --shadow-lg: 0 10px 15px -3px rgba(0,0,0,0.1);" css-line
  s"   --shadow-xl: 0 20px 25px -5px rgba(0,0,0,0.1);" css-line
  s"   --shadow-glow: 0 0 20px rgba(99,102,241,0.3);" css-line
  css-nl

  \ Transitions
  s"   /* Transitions */" css-line
  s"   --transition-fast: 150ms ease;" css-line
  s"   --transition-base: 200ms ease;" css-line
  s"   --transition-slow: 300ms ease;" css-line
  s"   --transition-slower: 500ms ease;" css-line

  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Base Reset
\ ---------------------------------------------------------------------------

: emit-reset ( -- )
  s" Reset" css-section

  s" *, *::before, *::after {" css-line
  s"   box-sizing: border-box;" css-line
  s"   margin: 0;" css-line
  s"   padding: 0;" css-line
  s" }" css-line
  css-nl

  s" html {" css-line
  s"   font-size: 16px;" css-line
  s"   -webkit-font-smoothing: antialiased;" css-line
  s"   -moz-osx-font-smoothing: grayscale;" css-line
  s" }" css-line
  css-nl

  s" body {" css-line
  s"   font-family: var(--font-sans);" css-line
  s"   font-size: var(--text-base);" css-line
  s"   line-height: var(--leading-normal);" css-line
  s"   color: var(--fg);" css-line
  s"   background: var(--bg);" css-line
  s" }" css-line
  css-nl

  s" a { color: inherit; text-decoration: none; }" css-line
  s" img, svg { display: block; max-width: 100%; }" css-line
  s" button, input, select, textarea { font: inherit; }" css-line ;

\ ---------------------------------------------------------------------------
\ Utility Classes - Layout
\ ---------------------------------------------------------------------------

: emit-layout-utilities ( -- )
  s" Layout Utilities" css-section

  \ Display
  s" .block { display: block; }" css-line
  s" .inline-block { display: inline-block; }" css-line
  s" .inline { display: inline; }" css-line
  s" .flex { display: flex; }" css-line
  s" .inline-flex { display: inline-flex; }" css-line
  s" .grid { display: grid; }" css-line
  s" .hidden { display: none; }" css-line
  css-nl

  \ Flexbox
  s" .flex-row { flex-direction: row; }" css-line
  s" .flex-col { flex-direction: column; }" css-line
  s" .flex-wrap { flex-wrap: wrap; }" css-line
  s" .flex-nowrap { flex-wrap: nowrap; }" css-line
  s" .flex-1 { flex: 1 1 0%; }" css-line
  s" .flex-auto { flex: 1 1 auto; }" css-line
  s" .flex-none { flex: none; }" css-line
  css-nl

  \ Alignment
  s" .items-start { align-items: flex-start; }" css-line
  s" .items-center { align-items: center; }" css-line
  s" .items-end { align-items: flex-end; }" css-line
  s" .items-stretch { align-items: stretch; }" css-line
  s" .justify-start { justify-content: flex-start; }" css-line
  s" .justify-center { justify-content: center; }" css-line
  s" .justify-end { justify-content: flex-end; }" css-line
  s" .justify-between { justify-content: space-between; }" css-line
  s" .justify-around { justify-content: space-around; }" css-line
  css-nl

  \ Gap
  s" .gap-1 { gap: var(--space-1); }" css-line
  s" .gap-2 { gap: var(--space-2); }" css-line
  s" .gap-3 { gap: var(--space-3); }" css-line
  s" .gap-4 { gap: var(--space-4); }" css-line
  s" .gap-6 { gap: var(--space-6); }" css-line
  s" .gap-8 { gap: var(--space-8); }" css-line ;

\ ---------------------------------------------------------------------------
\ Utility Classes - Spacing
\ ---------------------------------------------------------------------------

: emit-spacing-utilities ( -- )
  s" Spacing Utilities" css-section

  \ Padding
  s" .p-0 { padding: 0; }" css-line
  s" .p-1 { padding: var(--space-1); }" css-line
  s" .p-2 { padding: var(--space-2); }" css-line
  s" .p-3 { padding: var(--space-3); }" css-line
  s" .p-4 { padding: var(--space-4); }" css-line
  s" .p-6 { padding: var(--space-6); }" css-line
  s" .p-8 { padding: var(--space-8); }" css-line
  css-nl

  s" .px-1 { padding-left: var(--space-1); padding-right: var(--space-1); }" css-line
  s" .px-2 { padding-left: var(--space-2); padding-right: var(--space-2); }" css-line
  s" .px-4 { padding-left: var(--space-4); padding-right: var(--space-4); }" css-line
  s" .px-6 { padding-left: var(--space-6); padding-right: var(--space-6); }" css-line
  css-nl

  s" .py-1 { padding-top: var(--space-1); padding-bottom: var(--space-1); }" css-line
  s" .py-2 { padding-top: var(--space-2); padding-bottom: var(--space-2); }" css-line
  s" .py-4 { padding-top: var(--space-4); padding-bottom: var(--space-4); }" css-line
  s" .py-6 { padding-top: var(--space-6); padding-bottom: var(--space-6); }" css-line
  css-nl

  \ Margin
  s" .m-0 { margin: 0; }" css-line
  s" .m-auto { margin: auto; }" css-line
  s" .mx-auto { margin-left: auto; margin-right: auto; }" css-line
  css-nl

  s" .mt-1 { margin-top: var(--space-1); }" css-line
  s" .mt-2 { margin-top: var(--space-2); }" css-line
  s" .mt-4 { margin-top: var(--space-4); }" css-line
  s" .mt-6 { margin-top: var(--space-6); }" css-line
  s" .mt-8 { margin-top: var(--space-8); }" css-line
  css-nl

  s" .mb-1 { margin-bottom: var(--space-1); }" css-line
  s" .mb-2 { margin-bottom: var(--space-2); }" css-line
  s" .mb-4 { margin-bottom: var(--space-4); }" css-line
  s" .mb-6 { margin-bottom: var(--space-6); }" css-line
  s" .mb-8 { margin-bottom: var(--space-8); }" css-line ;

\ ---------------------------------------------------------------------------
\ Utility Classes - Typography
\ ---------------------------------------------------------------------------

: emit-typography-utilities ( -- )
  s" Typography Utilities" css-section

  \ Font size
  s" .text-xs { font-size: var(--text-xs); }" css-line
  s" .text-sm { font-size: var(--text-sm); }" css-line
  s" .text-base { font-size: var(--text-base); }" css-line
  s" .text-lg { font-size: var(--text-lg); }" css-line
  s" .text-xl { font-size: var(--text-xl); }" css-line
  s" .text-2xl { font-size: var(--text-2xl); }" css-line
  s" .text-3xl { font-size: var(--text-3xl); }" css-line
  s" .text-4xl { font-size: var(--text-4xl); }" css-line
  css-nl

  \ Font weight
  s" .font-light { font-weight: var(--font-light); }" css-line
  s" .font-normal { font-weight: var(--font-normal); }" css-line
  s" .font-medium { font-weight: var(--font-medium); }" css-line
  s" .font-semibold { font-weight: var(--font-semibold); }" css-line
  s" .font-bold { font-weight: var(--font-bold); }" css-line
  css-nl

  \ Text alignment
  s" .text-left { text-align: left; }" css-line
  s" .text-center { text-align: center; }" css-line
  s" .text-right { text-align: right; }" css-line
  css-nl

  \ Text color
  s" .text-primary { color: var(--color-primary-500); }" css-line
  s" .text-muted { color: var(--fg-muted); }" css-line
  s" .text-success { color: var(--color-success); }" css-line
  s" .text-warning { color: var(--color-warning); }" css-line
  s" .text-error { color: var(--color-error); }" css-line
  css-nl

  \ Font family
  s" .font-sans { font-family: var(--font-sans); }" css-line
  s" .font-mono { font-family: var(--font-mono); }" css-line ;

\ ---------------------------------------------------------------------------
\ Utility Classes - Colors & Backgrounds
\ ---------------------------------------------------------------------------

: emit-color-utilities ( -- )
  s" Color Utilities" css-section

  \ Backgrounds
  s" .bg-primary { background-color: var(--color-primary-500); }" css-line
  s" .bg-surface { background-color: var(--bg-surface); }" css-line
  s" .bg-elevated { background-color: var(--bg-elevated); }" css-line
  s" .bg-transparent { background-color: transparent; }" css-line
  css-nl

  \ Borders
  s" .border { border: 1px solid var(--border); }" css-line
  s" .border-0 { border: none; }" css-line
  s" .border-primary { border-color: var(--color-primary-500); }" css-line
  css-nl

  \ Border radius
  s" .rounded-none { border-radius: var(--radius-none); }" css-line
  s" .rounded-sm { border-radius: var(--radius-sm); }" css-line
  s" .rounded { border-radius: var(--radius-md); }" css-line
  s" .rounded-lg { border-radius: var(--radius-lg); }" css-line
  s" .rounded-xl { border-radius: var(--radius-xl); }" css-line
  s" .rounded-full { border-radius: var(--radius-full); }" css-line ;

\ ---------------------------------------------------------------------------
\ Component - Buttons
\ ---------------------------------------------------------------------------

: emit-button-component ( -- )
  s" Button Component" css-section

  s" .btn {" css-line
  s"   display: inline-flex;" css-line
  s"   align-items: center;" css-line
  s"   justify-content: center;" css-line
  s"   gap: var(--space-2);" css-line
  s"   padding: var(--space-2) var(--space-4);" css-line
  s"   font-size: var(--text-sm);" css-line
  s"   font-weight: var(--font-medium);" css-line
  s"   line-height: var(--leading-tight);" css-line
  s"   border-radius: var(--radius-lg);" css-line
  s"   border: 1px solid transparent;" css-line
  s"   cursor: pointer;" css-line
  s"   transition: all var(--transition-fast);" css-line
  s" }" css-line
  css-nl

  s" .btn:disabled {" css-line
  s"   opacity: 0.5;" css-line
  s"   cursor: not-allowed;" css-line
  s" }" css-line
  css-nl

  s" .btn-primary {" css-line
  s"   background: var(--color-primary-500);" css-line
  s"   color: white;" css-line
  s" }" css-line
  css-nl

  s" .btn-primary:hover:not(:disabled) {" css-line
  s"   background: var(--color-primary-600);" css-line
  s"   box-shadow: var(--shadow-glow);" css-line
  s" }" css-line
  css-nl

  s" .btn-secondary {" css-line
  s"   background: var(--bg-elevated);" css-line
  s"   color: var(--fg);" css-line
  s"   border-color: var(--border);" css-line
  s" }" css-line
  css-nl

  s" .btn-secondary:hover:not(:disabled) {" css-line
  s"   background: var(--color-neutral-700);" css-line
  s"   border-color: var(--color-neutral-600);" css-line
  s" }" css-line
  css-nl

  s" .btn-ghost {" css-line
  s"   background: transparent;" css-line
  s"   color: var(--fg);" css-line
  s" }" css-line
  css-nl

  s" .btn-ghost:hover:not(:disabled) {" css-line
  s"   background: var(--bg-elevated);" css-line
  s" }" css-line
  css-nl

  \ Sizes
  s" .btn-sm { padding: var(--space-1) var(--space-2); font-size: var(--text-xs); }" css-line
  s" .btn-lg { padding: var(--space-3) var(--space-6); font-size: var(--text-base); }" css-line ;

\ ---------------------------------------------------------------------------
\ Component - Cards
\ ---------------------------------------------------------------------------

: emit-card-component ( -- )
  s" Card Component" css-section

  s" .card {" css-line
  s"   background: var(--bg-surface);" css-line
  s"   border: 1px solid var(--border);" css-line
  s"   border-radius: var(--radius-xl);" css-line
  s"   overflow: hidden;" css-line
  s"   transition: all var(--transition-base);" css-line
  s" }" css-line
  css-nl

  s" .card:hover {" css-line
  s"   border-color: var(--color-primary-500);" css-line
  s"   box-shadow: var(--shadow-lg);" css-line
  s" }" css-line
  css-nl

  s" .card-header {" css-line
  s"   padding: var(--space-4) var(--space-6);" css-line
  s"   border-bottom: 1px solid var(--border);" css-line
  s" }" css-line
  css-nl

  s" .card-body {" css-line
  s"   padding: var(--space-6);" css-line
  s" }" css-line
  css-nl

  s" .card-footer {" css-line
  s"   padding: var(--space-4) var(--space-6);" css-line
  s"   border-top: 1px solid var(--border);" css-line
  s"   background: var(--bg-elevated);" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Component - Forms
\ ---------------------------------------------------------------------------

: emit-form-component ( -- )
  s" Form Component" css-section

  s" .form-group {" css-line
  s"   display: flex;" css-line
  s"   flex-direction: column;" css-line
  s"   gap: var(--space-2);" css-line
  s" }" css-line
  css-nl

  s" .form-label {" css-line
  s"   font-size: var(--text-sm);" css-line
  s"   font-weight: var(--font-medium);" css-line
  s"   color: var(--fg);" css-line
  s" }" css-line
  css-nl

  s" .form-input {" css-line
  s"   padding: var(--space-2) var(--space-3);" css-line
  s"   background: var(--bg-elevated);" css-line
  s"   border: 1px solid var(--border);" css-line
  s"   border-radius: var(--radius-lg);" css-line
  s"   color: var(--fg);" css-line
  s"   font-size: var(--text-base);" css-line
  s"   transition: all var(--transition-fast);" css-line
  s" }" css-line
  css-nl

  s" .form-input:focus {" css-line
  s"   outline: none;" css-line
  s"   border-color: var(--color-primary-500);" css-line
  s"   box-shadow: 0 0 0 3px rgba(99,102,241,0.2);" css-line
  s" }" css-line
  css-nl

  s" .form-input::placeholder {" css-line
  s"   color: var(--fg-muted);" css-line
  s" }" css-line
  css-nl

  s" .form-textarea {" css-line
  s"   min-height: 100px;" css-line
  s"   resize: vertical;" css-line
  s" }" css-line
  css-nl

  s" .form-help {" css-line
  s"   font-size: var(--text-sm);" css-line
  s"   color: var(--fg-muted);" css-line
  s" }" css-line
  css-nl

  s" .form-error {" css-line
  s"   font-size: var(--text-sm);" css-line
  s"   color: var(--color-error);" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Component - Navigation
\ ---------------------------------------------------------------------------

: emit-nav-component ( -- )
  s" Navigation Component" css-section

  s" .nav {" css-line
  s"   display: flex;" css-line
  s"   align-items: center;" css-line
  s"   gap: var(--space-1);" css-line
  s" }" css-line
  css-nl

  s" .nav-link {" css-line
  s"   padding: var(--space-2) var(--space-3);" css-line
  s"   font-size: var(--text-sm);" css-line
  s"   font-weight: var(--font-medium);" css-line
  s"   color: var(--fg-muted);" css-line
  s"   border-radius: var(--radius-lg);" css-line
  s"   transition: all var(--transition-fast);" css-line
  s" }" css-line
  css-nl

  s" .nav-link:hover {" css-line
  s"   color: var(--fg);" css-line
  s"   background: var(--bg-elevated);" css-line
  s" }" css-line
  css-nl

  s" .nav-link.active {" css-line
  s"   color: var(--color-primary-500);" css-line
  s"   background: rgba(99,102,241,0.1);" css-line
  s" }" css-line
  css-nl

  \ Navbar
  s" .navbar {" css-line
  s"   display: flex;" css-line
  s"   align-items: center;" css-line
  s"   justify-content: space-between;" css-line
  s"   padding: var(--space-4) var(--space-6);" css-line
  s"   background: var(--bg-surface);" css-line
  s"   border-bottom: 1px solid var(--border);" css-line
  s" }" css-line
  css-nl

  s" .navbar-brand {" css-line
  s"   font-size: var(--text-xl);" css-line
  s"   font-weight: var(--font-bold);" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Component - Badges
\ ---------------------------------------------------------------------------

: emit-badge-component ( -- )
  s" Badge Component" css-section

  s" .badge {" css-line
  s"   display: inline-flex;" css-line
  s"   align-items: center;" css-line
  s"   padding: var(--space-1) var(--space-2);" css-line
  s"   font-size: var(--text-xs);" css-line
  s"   font-weight: var(--font-medium);" css-line
  s"   border-radius: var(--radius-full);" css-line
  s"   background: var(--bg-elevated);" css-line
  s"   color: var(--fg);" css-line
  s" }" css-line
  css-nl

  s" .badge-primary {" css-line
  s"   background: rgba(99,102,241,0.2);" css-line
  s"   color: var(--color-primary-400);" css-line
  s" }" css-line
  css-nl

  s" .badge-success {" css-line
  s"   background: rgba(34,197,94,0.2);" css-line
  s"   color: var(--color-success);" css-line
  s" }" css-line
  css-nl

  s" .badge-warning {" css-line
  s"   background: rgba(245,158,11,0.2);" css-line
  s"   color: var(--color-warning);" css-line
  s" }" css-line
  css-nl

  s" .badge-error {" css-line
  s"   background: rgba(239,68,68,0.2);" css-line
  s"   color: var(--color-error);" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Responsive Breakpoints
\ ---------------------------------------------------------------------------

: emit-responsive ( -- )
  s" Responsive Utilities" css-section

  s" /* Mobile-first breakpoints */" css-line
  s" /* sm: 640px, md: 768px, lg: 1024px, xl: 1280px */" css-line
  css-nl

  s" @media (min-width: 640px) {" css-line
  s"   .sm\\:flex { display: flex; }" css-line
  s"   .sm\\:hidden { display: none; }" css-line
  s"   .sm\\:flex-row { flex-direction: row; }" css-line
  s"   .sm\\:grid-cols-2 { grid-template-columns: repeat(2, 1fr); }" css-line
  s" }" css-line
  css-nl

  s" @media (min-width: 768px) {" css-line
  s"   .md\\:flex { display: flex; }" css-line
  s"   .md\\:hidden { display: none; }" css-line
  s"   .md\\:grid-cols-2 { grid-template-columns: repeat(2, 1fr); }" css-line
  s"   .md\\:grid-cols-3 { grid-template-columns: repeat(3, 1fr); }" css-line
  s" }" css-line
  css-nl

  s" @media (min-width: 1024px) {" css-line
  s"   .lg\\:flex { display: flex; }" css-line
  s"   .lg\\:hidden { display: none; }" css-line
  s"   .lg\\:grid-cols-3 { grid-template-columns: repeat(3, 1fr); }" css-line
  s"   .lg\\:grid-cols-4 { grid-template-columns: repeat(4, 1fr); }" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Dark Mode Support
\ ---------------------------------------------------------------------------

: emit-dark-mode ( -- )
  s" Dark Mode" css-section

  s" /* Light mode overrides */" css-line
  s" @media (prefers-color-scheme: light) {" css-line
  s"   :root {" css-line
  s"     --bg: var(--color-neutral-50);" css-line
  s"     --bg-surface: white;" css-line
  s"     --bg-elevated: var(--color-neutral-100);" css-line
  s"     --fg: var(--color-neutral-900);" css-line
  s"     --fg-muted: var(--color-neutral-500);" css-line
  s"     --border: var(--color-neutral-200);" css-line
  s"   }" css-line
  s" }" css-line
  css-nl

  s" /* Force dark mode */" css-line
  s" .dark {" css-line
  s"   --bg: var(--color-neutral-950);" css-line
  s"   --bg-surface: var(--color-neutral-900);" css-line
  s"   --bg-elevated: var(--color-neutral-800);" css-line
  s"   --fg: var(--color-neutral-100);" css-line
  s"   --fg-muted: var(--color-neutral-400);" css-line
  s"   --border: var(--color-neutral-700);" css-line
  s" }" css-line
  css-nl

  s" /* Force light mode */" css-line
  s" .light {" css-line
  s"   --bg: var(--color-neutral-50);" css-line
  s"   --bg-surface: white;" css-line
  s"   --bg-elevated: var(--color-neutral-100);" css-line
  s"   --fg: var(--color-neutral-900);" css-line
  s"   --fg-muted: var(--color-neutral-500);" css-line
  s"   --border: var(--color-neutral-200);" css-line
  s" }" css-line ;

\ ---------------------------------------------------------------------------
\ Main Generation
\ ---------------------------------------------------------------------------

: generate-framework ( -- )
  css-open

  s" Fifth CSS Framework - Generated by Fifth" css-comment
  s" https://github.com/yourname/fifth" css-comment
  css-nl

  \ Design Tokens
  emit-color-tokens
  emit-spacing-tokens
  emit-typography-tokens
  emit-effect-tokens

  \ Base
  emit-reset

  \ Utilities
  emit-layout-utilities
  emit-spacing-utilities
  emit-typography-utilities
  emit-color-utilities

  \ Components
  emit-button-component
  emit-card-component
  emit-form-component
  emit-nav-component
  emit-badge-component

  \ Responsive
  emit-responsive

  \ Theme
  emit-dark-mode

  css-close

  ." CSS Framework generated: /tmp/framework.css" cr
  ." " cr
  ." Includes:" cr
  ."   - Design tokens (colors, spacing, typography, effects)" cr
  ."   - Reset/normalize" cr
  ."   - Layout utilities (flex, grid, alignment)" cr
  ."   - Spacing utilities (padding, margin)" cr
  ."   - Typography utilities" cr
  ."   - Color utilities" cr
  ."   - Button component" cr
  ."   - Card component" cr
  ."   - Form component" cr
  ."   - Navigation component" cr
  ."   - Badge component" cr
  ."   - Responsive breakpoints" cr
  ."   - Dark/light mode support" cr ;

\ Run
generate-framework
