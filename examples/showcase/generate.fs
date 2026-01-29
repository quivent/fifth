\ fifth/examples/showcase/generate.fs
\ Generate showcase gallery for Fifth examples

require ~/.fifth/lib/core.fs

\ --- Configuration ---

: output-file ( -- addr u ) s" index.html" ;

\ --- Styles ---

: showcase-styles ( -- )
  <style>
  s" :root {" raw nl
  s"   --bg: #0f0f1a;" raw nl
  s"   --surface: #1a1a2e;" raw nl
  s"   --surface-hover: #252542;" raw nl
  s"   --primary: #6366f1;" raw nl
  s"   --primary-glow: rgba(99, 102, 241, 0.3);" raw nl
  s"   --text: #e2e8f0;" raw nl
  s"   --text-muted: #94a3b8;" raw nl
  s"   --border: #2d2d4a;" raw nl
  s"   --green: #10b981;" raw nl
  s"   --blue: #3b82f6;" raw nl
  s"   --purple: #8b5cf6;" raw nl
  s"   --orange: #f59e0b;" raw nl
  s"   --pink: #ec4899;" raw nl
  s"   --cyan: #06b6d4;" raw nl
  s"   --red: #ef4444;" raw nl
  s" }" raw nl
  s" * { margin: 0; padding: 0; box-sizing: border-box; }" raw nl
  s" body {" raw nl
  s"   font-family: 'Inter', system-ui, -apple-system, sans-serif;" raw nl
  s"   background: var(--bg);" raw nl
  s"   color: var(--text);" raw nl
  s"   line-height: 1.6;" raw nl
  s"   min-height: 100vh;" raw nl
  s" }" raw nl
  s" .container { max-width: 1400px; margin: 0 auto; padding: 2rem; }" raw nl
  s" " raw nl
  s" /* Header */" raw nl
  s" .hero {" raw nl
  s"   text-align: center;" raw nl
  s"   padding: 4rem 2rem;" raw nl
  s"   background: linear-gradient(180deg, var(--surface) 0%, var(--bg) 100%);" raw nl
  s"   border-bottom: 1px solid var(--border);" raw nl
  s" }" raw nl
  s" .hero h1 {" raw nl
  s"   font-size: 3.5rem;" raw nl
  s"   font-weight: 800;" raw nl
  s"   background: linear-gradient(135deg, var(--primary) 0%, var(--purple) 50%, var(--pink) 100%);" raw nl
  s"   -webkit-background-clip: text;" raw nl
  s"   -webkit-text-fill-color: transparent;" raw nl
  s"   margin-bottom: 1rem;" raw nl
  s" }" raw nl
  s" .hero .tagline {" raw nl
  s"   font-size: 1.25rem;" raw nl
  s"   color: var(--text-muted);" raw nl
  s"   max-width: 600px;" raw nl
  s"   margin: 0 auto;" raw nl
  s" }" raw nl
  s" .hero .stats {" raw nl
  s"   display: flex;" raw nl
  s"   justify-content: center;" raw nl
  s"   gap: 3rem;" raw nl
  s"   margin-top: 2rem;" raw nl
  s" }" raw nl
  s" .hero .stat {" raw nl
  s"   text-align: center;" raw nl
  s" }" raw nl
  s" .hero .stat-value {" raw nl
  s"   font-size: 2.5rem;" raw nl
  s"   font-weight: 700;" raw nl
  s"   color: var(--primary);" raw nl
  s" }" raw nl
  s" .hero .stat-label {" raw nl
  s"   font-size: 0.875rem;" raw nl
  s"   color: var(--text-muted);" raw nl
  s"   text-transform: uppercase;" raw nl
  s"   letter-spacing: 0.05em;" raw nl
  s" }" raw nl
  s" " raw nl
  s" /* Category sections */" raw nl
  s" .category {" raw nl
  s"   margin: 3rem 0;" raw nl
  s" }" raw nl
  s" .category-header {" raw nl
  s"   display: flex;" raw nl
  s"   align-items: center;" raw nl
  s"   gap: 1rem;" raw nl
  s"   margin-bottom: 1.5rem;" raw nl
  s"   padding-bottom: 0.75rem;" raw nl
  s"   border-bottom: 1px solid var(--border);" raw nl
  s" }" raw nl
  s" .category-icon {" raw nl
  s"   width: 40px;" raw nl
  s"   height: 40px;" raw nl
  s"   border-radius: 10px;" raw nl
  s"   display: flex;" raw nl
  s"   align-items: center;" raw nl
  s"   justify-content: center;" raw nl
  s"   font-size: 1.25rem;" raw nl
  s" }" raw nl
  s" .category h2 {" raw nl
  s"   font-size: 1.5rem;" raw nl
  s"   font-weight: 600;" raw nl
  s" }" raw nl
  s" .category-count {" raw nl
  s"   background: var(--surface);" raw nl
  s"   padding: 0.25rem 0.75rem;" raw nl
  s"   border-radius: 9999px;" raw nl
  s"   font-size: 0.875rem;" raw nl
  s"   color: var(--text-muted);" raw nl
  s" }" raw nl
  s" " raw nl
  s" /* Card grid */" raw nl
  s" .card-grid {" raw nl
  s"   display: grid;" raw nl
  s"   grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));" raw nl
  s"   gap: 1.25rem;" raw nl
  s" }" raw nl
  s" " raw nl
  s" /* Cards */" raw nl
  s" .card {" raw nl
  s"   background: var(--surface);" raw nl
  s"   border: 1px solid var(--border);" raw nl
  s"   border-radius: 12px;" raw nl
  s"   padding: 1.5rem;" raw nl
  s"   transition: all 0.2s ease;" raw nl
  s"   cursor: pointer;" raw nl
  s"   text-decoration: none;" raw nl
  s"   color: inherit;" raw nl
  s"   display: block;" raw nl
  s" }" raw nl
  s" .card:hover {" raw nl
  s"   background: var(--surface-hover);" raw nl
  s"   border-color: var(--primary);" raw nl
  s"   transform: translateY(-2px);" raw nl
  s"   box-shadow: 0 8px 30px var(--primary-glow);" raw nl
  s" }" raw nl
  s" .card-header {" raw nl
  s"   display: flex;" raw nl
  s"   align-items: flex-start;" raw nl
  s"   justify-content: space-between;" raw nl
  s"   margin-bottom: 0.75rem;" raw nl
  s" }" raw nl
  s" .card h3 {" raw nl
  s"   font-size: 1.125rem;" raw nl
  s"   font-weight: 600;" raw nl
  s"   color: var(--text);" raw nl
  s" }" raw nl
  s" .card-badge {" raw nl
  s"   font-size: 0.75rem;" raw nl
  s"   padding: 0.25rem 0.5rem;" raw nl
  s"   border-radius: 6px;" raw nl
  s"   font-weight: 500;" raw nl
  s" }" raw nl
  s" .card p {" raw nl
  s"   font-size: 0.9rem;" raw nl
  s"   color: var(--text-muted);" raw nl
  s"   margin-bottom: 1rem;" raw nl
  s" }" raw nl
  s" .card-tags {" raw nl
  s"   display: flex;" raw nl
  s"   flex-wrap: wrap;" raw nl
  s"   gap: 0.5rem;" raw nl
  s" }" raw nl
  s" .tag {" raw nl
  s"   font-size: 0.75rem;" raw nl
  s"   padding: 0.25rem 0.5rem;" raw nl
  s"   background: rgba(99, 102, 241, 0.1);" raw nl
  s"   color: var(--primary);" raw nl
  s"   border-radius: 4px;" raw nl
  s" }" raw nl
  s" " raw nl
  s" /* Color variations */" raw nl
  s" .bg-green { background: rgba(16, 185, 129, 0.15); }" raw nl
  s" .bg-blue { background: rgba(59, 130, 246, 0.15); }" raw nl
  s" .bg-purple { background: rgba(139, 92, 246, 0.15); }" raw nl
  s" .bg-orange { background: rgba(245, 158, 11, 0.15); }" raw nl
  s" .bg-pink { background: rgba(236, 72, 153, 0.15); }" raw nl
  s" .bg-cyan { background: rgba(6, 182, 212, 0.15); }" raw nl
  s" .bg-red { background: rgba(239, 68, 68, 0.15); }" raw nl
  s" .text-green { color: var(--green); }" raw nl
  s" .text-blue { color: var(--blue); }" raw nl
  s" .text-purple { color: var(--purple); }" raw nl
  s" .text-orange { color: var(--orange); }" raw nl
  s" .text-pink { color: var(--pink); }" raw nl
  s" .text-cyan { color: var(--cyan); }" raw nl
  s" .text-red { color: var(--red); }" raw nl
  s" " raw nl
  s" /* Footer */" raw nl
  s" .footer {" raw nl
  s"   text-align: center;" raw nl
  s"   padding: 3rem 2rem;" raw nl
  s"   border-top: 1px solid var(--border);" raw nl
  s"   margin-top: 4rem;" raw nl
  s"   color: var(--text-muted);" raw nl
  s" }" raw nl
  s" .footer a { color: var(--primary); text-decoration: none; }" raw nl
  s" .footer a:hover { text-decoration: underline; }" raw nl
  s" " raw nl
  s" /* Responsive */" raw nl
  s" @media (max-width: 768px) {" raw nl
  s"   .hero h1 { font-size: 2.5rem; }" raw nl
  s"   .hero .stats { flex-direction: column; gap: 1.5rem; }" raw nl
  s"   .container { padding: 1rem; }" raw nl
  s" }" raw nl
  </style> ;

\ --- Components ---

: hero-section ( -- )
  <header.> s" hero" raw q s" >" raw nl
    <h1> s" Fifth Examples" text </h1> nl
    <p.> s" tagline" raw q s" >" raw
      s" A curated collection of practical applications built with Fifth, " text
      s" showcasing stack-based programming for the modern age." text
    </p> nl
    <div.> s" stats" raw q s" >" raw nl
      <div.> s" stat" raw q s" >" raw
        <div.> s" stat-value" raw q s" >" raw s" 22" text </div>
        <div.> s" stat-label" raw q s" >" raw s" Examples" text </div>
      </div> nl
      <div.> s" stat" raw q s" >" raw
        <div.> s" stat-value" raw q s" >" raw s" 7" text </div>
        <div.> s" stat-label" raw q s" >" raw s" Categories" text </div>
      </div> nl
      <div.> s" stat" raw q s" >" raw
        <div.> s" stat-value" raw q s" >" raw s" 100%" text </div>
        <div.> s" stat-label" raw q s" >" raw s" Forth" text </div>
      </div> nl
    </div> nl
  </header> nl ;

: category-start ( icon-addr icon-u title-addr title-u count color-class-addr color-class-u -- )
  <section.> s" category" raw q s" >" raw nl
  <div.> s" category-header" raw q s" >" raw nl
    <div.> s" category-icon " raw 2swap raw q s" >" raw text </div>
    <h2> text </h2>
    <span.> s" category-count" raw q s" >" raw . s"  examples" text </span>
  </div> nl
  <div.> s" card-grid" raw q s" >" raw nl ;

: category-end ( -- )
  </div> nl
  </section> nl ;

: example-card ( folder-addr folder-u title-addr title-u desc-addr desc-u tags-addr tags-u -- )
  s" <a href=" raw q s" ../" raw 2>r 2>r 2>r raw s" /README.md" raw q
  s"  class=" raw q s" card" raw q s" >" raw nl
  <div.> s" card-header" raw q s" >" raw
    <h3> 2r> text </h3>
  </div> nl
  <p> 2r> text </p>
  <div.> s" card-tags" raw q s" >" raw
    2r> type  \ tags (pre-formatted)
  </div> nl
  s" </a>" raw nl ;

: tag ( text-addr text-u -- )
  s" <span class=" raw q s" tag" raw q s" >" raw text s" </span>" raw ;

\ --- Category: Web & Reports ---

: section-web ( -- )
  s" \xf0\x9f\x8c\x90" s" Web & Report Generation" 3 s" bg-blue text-blue" category-start

  s" static-site-generator" s" Static Site Generator"
  s" Generate blogs and documentation from markdown with pandoc integration."
  s" " str-reset s" markdown" tag s" pandoc" tag s" templates" tag str$
  example-card

  s" dashboard-generator" s" Dashboard Generator"
  s" Pull metrics from multiple sources into a single-page dashboard with Chart.js."
  s" " str-reset s" metrics" tag s" charts" tag s" real-time" tag str$
  example-card

  s" invoice-system" s" Invoice System"
  s" Generate PDF invoices from database records with print-optimized CSS."
  s" " str-reset s" pdf" tag s" sqlite" tag s" business" tag str$
  example-card

  category-end ;

\ --- Category: Data Processing ---

: section-data ( -- )
  s" \xf0\x9f\x94\xa7" s" Data Processing" 3 s" bg-green text-green" category-start

  s" log-analyzer" s" Log Analyzer"
  s" Parse and summarize application logs with pattern matching and HTML reports."
  s" " str-reset s" parsing" tag s" reports" tag s" monitoring" tag str$
  example-card

  s" csv-transformer" s" CSV Transformer"
  s" Convert between data formats with field transformations and lookups."
  s" " str-reset s" csv" tag s" etl" tag s" transform" tag str$
  example-card

  s" db-migration" s" Database Migration"
  s" Schema versioning with up/down migrations and rollback support."
  s" " str-reset s" sqlite" tag s" schema" tag s" versioning" tag str$
  example-card

  category-end ;

\ --- Category: System Administration ---

: section-sysadmin ( -- )
  s" \xf0\x9f\x96\xa5\xef\xb8\x8f" s" System Administration" 3 s" bg-orange text-orange" category-start

  s" config-generator" s" Config Generator"
  s" Generate nginx, systemd, and other configs from composable Forth words."
  s" " str-reset s" nginx" tag s" systemd" tag s" templates" tag str$
  example-card

  s" server-health" s" Server Health Dashboard"
  s" Aggregate system metrics (df, free, uptime) into a status page."
  s" " str-reset s" monitoring" tag s" metrics" tag s" alerts" tag str$
  example-card

  s" deployment-script" s" Deployment Script"
  s" Multi-step deployment orchestration with rollback on failure."
  s" " str-reset s" deploy" tag s" rollback" tag s" automation" tag str$
  example-card

  category-end ;

\ --- Category: Developer Tools ---

: section-devtools ( -- )
  s" \xf0\x9f\x9b\xa0\xef\xb8\x8f" s" Developer Tools" 3 s" bg-purple text-purple" category-start

  s" code-generator" s" Code Generator"
  s" Generate models, routes, and tests from JSON schema definitions."
  s" " str-reset s" codegen" tag s" schema" tag s" boilerplate" tag str$
  example-card

  s" doc-generator" s" Documentation Generator"
  s" Extract and format documentation from Forth source comments."
  s" " str-reset s" docs" tag s" api" tag s" extraction" tag str$
  example-card

  s" project-scaffolder" s" Project Scaffolder"
  s" Create new projects from templates with variable substitution."
  s" " str-reset s" scaffold" tag s" templates" tag s" init" tag str$
  example-card

  category-end ;

\ --- Category: Domain-Specific ---

: section-domain ( -- )
  s" \xf0\x9f\x8e\xaf" s" Domain-Specific" 4 s" bg-pink text-pink" category-start

  s" financial-calculator" s" Financial Calculator"
  s" RPN-style loan amortization, compound interest, and investment projections."
  s" " str-reset s" finance" tag s" rpn" tag s" calculations" tag str$
  example-card

  s" quiz-system" s" Quiz System"
  s" Generate assessments, track scores, and analyze results."
  s" " str-reset s" quiz" tag s" scoring" tag s" education" tag str$
  example-card

  s" recipe-manager" s" Recipe Manager"
  s" Store, scale, and generate shopping lists from recipes."
  s" " str-reset s" recipes" tag s" scaling" tag s" inventory" tag str$
  example-card

  s" bookmark-manager" s" Bookmark Manager"
  s" Personal knowledge management with FTS5 search and tags."
  s" " str-reset s" bookmarks" tag s" search" tag s" tags" tag str$
  example-card

  category-end ;

\ --- Category: Embedded & Constrained ---

: section-embedded ( -- )
  s" \xf0\x9f\x94\x8c" s" Embedded & Constrained" 2 s" bg-cyan text-cyan" category-start

  s" iot-scripting" s" IoT Scripting"
  s" Lightweight sensor reading, threshold rules, and actuator control."
  s" " str-reset s" iot" tag s" sensors" tag s" embedded" tag str$
  example-card

  s" kiosk-display" s" Kiosk Display"
  s" Full-screen information displays with auto-refresh."
  s" " str-reset s" kiosk" tag s" display" tag s" signage" tag str$
  example-card

  category-end ;

\ --- Category: Integration Patterns ---

: section-integration ( -- )
  s" \xf0\x9f\x94\x97" s" Integration Patterns" 3 s" bg-red text-red" category-start

  s" webhook-handler" s" Webhook Handler"
  s" Process incoming webhooks with validation, storage, and replay."
  s" " str-reset s" webhooks" tag s" github" tag s" events" tag str$
  example-card

  s" cron-orchestrator" s" Cron Orchestrator"
  s" Job scheduling with dependencies, retries, and monitoring."
  s" " str-reset s" cron" tag s" jobs" tag s" scheduling" tag str$
  example-card

  s" api-client" s" API Client"
  s" REST API wrapper with response caching and rate limiting."
  s" " str-reset s" api" tag s" rest" tag s" caching" tag str$
  example-card

  category-end ;

\ --- Category: Agentic ---

: section-agentic ( -- )
  s" \xf0\x9f\xa4\x96" s" Agentic Coding" 1 s" bg-purple text-purple" category-start

  s" agentic-coder" s" Agentic Coder"
  s" AI coding assistant with tool use, memory, and task planning."
  s" " str-reset s" llm" tag s" tools" tag s" ai" tag s" agent" tag str$
  example-card

  category-end ;

\ --- Footer ---

: footer-section ( -- )
  <footer.> s" footer" raw q s" >" raw nl
    <p>
      s" Built with " text
      s" <a href=" raw q s" https://github.com/example/fifth" raw q s" >" raw
      s" Fifth" text s" </a>" raw
      s"  \xe2\x80\x94 Forth for the Fifth Age" text
    </p> nl
    <p>
      s" View the " text
      s" <a href=" raw q s" USE-CASES.md" raw q s" >" raw
      s" Use Cases Guide" text s" </a>" raw
      s"  for detailed documentation." text
    </p> nl
  </footer> nl ;

\ --- Main ---

: generate-showcase ( -- )
  output-file w/o create-file throw html>file

  s" Fifth Examples Showcase" html-head
  s" <link rel=" raw q s" preconnect" raw q s"  href=" raw q s" https://fonts.googleapis.com" raw q s" >" raw nl
  s" <link href=" raw q s" https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" raw q s"  rel=" raw q s" stylesheet" raw q s" >" raw nl
  showcase-styles
  html-body

  hero-section

  <main.> s" container" raw q s" >" raw nl
    section-web
    section-data
    section-sysadmin
    section-devtools
    section-domain
    section-embedded
    section-integration
    section-agentic
  </main> nl

  footer-section

  html-end
  html-fid @ close-file throw

  s" Generated: " type output-file type cr ;

generate-showcase
bye
