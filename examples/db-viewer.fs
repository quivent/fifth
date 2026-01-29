\ fifth/examples/db-viewer.fs - Claude Database Viewer
\ Demonstrates Fifth libraries: html.fs, sql.fs, str.fs
\
\ Usage: ./fifth examples/db-viewer.fs

require ~/fifth/lib/core.fs

\ ============================================================
\ Configuration
\ ============================================================

s" /tmp/claude-db-viewer.html" 2constant output-file
: agents-db s" ~/.claude/db/agents.db" ;
: projects-db s" ~/.claude/db/projects.db" ;

\ ============================================================
\ CSS Theme
\ ============================================================

: dark-theme ( -- )
  <style>
  s" *" s" box-sizing:border-box;margin:0;padding:0" css-rule
  s" body" s" font-family:system-ui,sans-serif;background:#0f0f14;color:#e4e4e7;padding:2rem;max-width:1200px;margin:0 auto" css-rule
  s" h1" s" color:#a78bfa;margin-bottom:1.5rem" css-rule
  s" h2" s" color:#8b5cf6;margin:2rem 0 1rem;border-bottom:1px solid #333;padding-bottom:0.5rem" css-rule
  s" .stats" s" display:flex;gap:1rem;flex-wrap:wrap;margin-bottom:2rem" css-rule
  s" .stat" s" background:linear-gradient(135deg,rgba(139,92,246,0.15),rgba(99,102,241,0.1));border:1px solid rgba(139,92,246,0.3);border-radius:0.75rem;padding:1.25rem;text-align:center;min-width:120px" css-rule
  s" .stat b" s" display:block;font-size:2rem;color:#a78bfa" css-rule
  s" .stat span" s" font-size:0.75rem;color:#71717a" css-rule
  s" .grid" s" display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:1rem" css-rule
  s" .card" s" background:#18181b;border:1px solid rgba(255,255,255,0.1);border-radius:0.75rem;padding:1.25rem;margin-bottom:0.75rem" css-rule
  s" .card h3" s" color:#c4b5fd;margin-bottom:0.5rem" css-rule
  s" .card p" s" color:#a1a1aa;font-size:0.875rem" css-rule
  s" .badge" s" display:inline-block;padding:0.2rem 0.6rem;border-radius:9999px;font-size:0.7rem;margin-right:0.4rem" css-rule
  s" .b-stage" s" background:#065f46;color:#6ee7b7" css-rule
  s" .b-domain" s" background:#1e40af;color:#93c5fd" css-rule
  s" .b-crit" s" background:#991b1b;color:#fca5a5" css-rule
  s" .b-strong" s" background:#92400e;color:#fcd34d" css-rule
  s" table" s" width:100%;border-collapse:collapse;font-size:0.85rem" css-rule
  s" th" s" text-align:left;padding:0.75rem;color:#8b5cf6;border-bottom:1px solid rgba(255,255,255,0.15)" css-rule
  s" td" s" padding:0.75rem;border-bottom:1px solid rgba(255,255,255,0.05);color:#a1a1aa" css-rule
  s" code" s" background:rgba(139,92,246,0.15);padding:0.15rem 0.4rem;border-radius:0.25rem;color:#c4b5fd" css-rule
  s" .term" s" color:#c084fc;font-weight:600" css-rule
  </style> ;

\ ============================================================
\ Stats Section
\ ============================================================

: stat-card ( n label$ -- )
  2>r
  s" stat" <div.>
    s" <b>" raw swap n>str raw s" </b>" raw
    s" <span>" raw 2r> text s" </span>" raw
  </div>nl ;

: emit-stats ( -- )
  s" stats" <div.>nl
    agents-db s" SELECT COUNT(*) FROM agents" sql-count s" Agents" stat-card
    projects-db s" SELECT COUNT(*) FROM projects" sql-count s" Projects" stat-card
    projects-db s" SELECT COUNT(*) FROM constraints" sql-count s" Constraints" stat-card
    projects-db s" SELECT COUNT(*) FROM navigation" sql-count s" Navigation" stat-card
    projects-db s" SELECT COUNT(*) FROM commands" sql-count s" Commands" stat-card
    projects-db s" SELECT COUNT(*) FROM glossary" sql-count s" Glossary" stat-card
  </div>nl ;

\ ============================================================
\ Agents Section
\ ============================================================

: agent-card ( row$ -- )
  s" card" <div.>
    <h3> 2dup 0 sql-field text 2drop </h3>
    <p>
      s" badge b-stage" <span.> 2dup 2 sql-field text 2drop </span>
      2dup 1 sql-field text 2drop
    </p>
  </div>nl ;

: emit-agents ( -- )
  s" Agents (agents.db)" h2.
  s" grid" <div.>nl
  agents-db s" SELECT name, role, stage FROM agents ORDER BY name LIMIT 50" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if agent-card else 2drop then
  repeat 2drop
  sql-close
  </div>nl ;

\ ============================================================
\ Projects Section
\ ============================================================

: project-card ( row$ -- )
  s" card" <div.>
    <h3> 2dup 0 sql-field text 2drop </h3>
    <p>
      s" badge b-domain" <span.> 2dup 1 sql-field text 2drop </span>
      s" badge b-crit" <span.> 2dup 2 sql-field text 2drop </span>
    </p>
    <p> 2dup 3 sql-field text 2drop </p>
  </div>nl ;

: emit-projects ( -- )
  s" Projects (projects.db)" h2.
  projects-db s" SELECT name, domain, sensitivity, description FROM projects" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if project-card else 2drop then
  repeat 2drop
  sql-close ;

\ ============================================================
\ Navigation Section
\ ============================================================

: nav-row ( row$ -- )
  <tr>
    <td> <code> 2dup 0 sql-field text 2drop </code> </td>
    <td> <code> 2dup 1 sql-field text 2drop </code> </td>
    2dup 2 sql-field td. 2drop
  </tr> ;

: emit-navigation ( -- )
  s" Navigation" h2.
  <table>
    <tr> s" Category" th. s" Path" th. s" Description" th. </tr>
    projects-db s" SELECT category, path, description FROM navigation" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if nav-row else 2drop then
    repeat 2drop
    sql-close
  </table> ;

\ ============================================================
\ Commands Section
\ ============================================================

: cmd-row ( row$ -- )
  <tr>
    <td> <strong> 2dup 0 sql-field text 2drop </strong> </td>
    <td> <code> 2dup 1 sql-field text 2drop </code> </td>
    2dup 2 sql-field td. 2drop
  </tr> ;

: emit-commands ( -- )
  s" Commands" h2.
  <table>
    <tr> s" Name" th. s" Command" th. s" Category" th. </tr>
    projects-db s" SELECT name, command, category FROM commands" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if cmd-row else 2drop then
    repeat 2drop
    sql-close
  </table> ;

\ ============================================================
\ Glossary Section
\ ============================================================

: glossary-card ( row$ -- )
  s" card" <div.>
    s" term" <span.> 2dup 0 sql-field text 2drop </span>
    <p> 2dup 1 sql-field text 2drop </p>
  </div>nl ;

: emit-glossary ( -- )
  s" Glossary" h2.
  s" grid" <div.>nl
  projects-db s" SELECT term, definition FROM glossary ORDER BY term" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if glossary-card else 2drop then
  repeat 2drop
  sql-close
  </div>nl ;

\ ============================================================
\ Main
\ ============================================================

: generate ( -- )
  output-file w/o create-file throw html>file

  s" Claude Databases" html-head
  dark-theme
  html-body
  s" Claude Databases" h1.
  emit-stats
  emit-agents
  emit-projects
  emit-navigation
  emit-commands
  emit-glossary
  html-end

  html-fid @ close-file throw ;

: view ( -- )
  output-file open-file-cmd ;

: main ( -- )
  ." Fifth DB Viewer" cr
  ." Generating HTML..." cr
  generate
  ." Opening browser..." cr
  view
  ." Done." cr ;

main bye
