\ fifth/examples/project-dashboard.fs - Single Database Dashboard
\ Clean template showing projects.db with tabbed interface
\
\ Usage: ./fifth examples/project-dashboard.fs

require ~/fifth/lib/core.fs
require ~/fifth/lib/ui.fs

\ ============================================================
\ Configuration
\ ============================================================

s" /tmp/project-dashboard.html" 2constant output-file
: db s" ~/.claude/db/projects.db" ;

\ ============================================================
\ Page Styles (dark theme)
\ ============================================================

: page-css ( -- )
  <style>
  s" *" s" box-sizing:border-box;margin:0;padding:0" css-rule
  s" body" s" font-family:system-ui,-apple-system,sans-serif;background:#0a0a0f;color:#e4e4e7;min-height:100vh" css-rule
  s" h1" s" font-size:1.5rem;color:#a78bfa" css-rule
  s" h2" s" font-size:1.1rem;color:#8b5cf6;margin-bottom:1rem" css-rule
  s" code" s" background:rgba(139,92,246,0.15);padding:0.15rem 0.4rem;border-radius:0.25rem;font-size:0.8rem;color:#c4b5fd" css-rule
  s" strong" s" color:#c4b5fd" css-rule
  </style>
  ui-css ;

\ ============================================================
\ Header
\ ============================================================

: page-header ( -- )
  s" dashboard-header" <header.> nl
    <h1> s" Project Database" text </h1>
    s" subtitle" <p.>
      s" Encoded project identities from " text
      <code> s" ~/.claude/db/projects.db" text </code>
    </p> nl
  </header> nl ;

\ ============================================================
\ Stats Row
\ ============================================================

: emit-stats ( -- )
  s" grid grid-4" <div.>nl  \ Changed from grid-auto for fixed layout
    db s" SELECT COUNT(*) FROM projects" sql-count s" Projects" stat-card-n
    db s" SELECT COUNT(*) FROM constraints" sql-count s" Constraints" stat-card-n
    db s" SELECT COUNT(*) FROM navigation" sql-count s" Navigation" stat-card-n
    db s" SELECT COUNT(*) FROM glossary" sql-count s" Glossary" stat-card-n
  </div>nl
  s" grid grid-4" <div.>nl
    db s" SELECT COUNT(*) FROM commands" sql-count s" Commands" stat-card-n
    db s" SELECT COUNT(*) FROM conventions" sql-count s" Conventions" stat-card-n
    db s" SELECT COUNT(*) FROM integrations" sql-count s" Integrations" stat-card-n
    db s" SELECT COUNT(*) FROM personas" sql-count s" Personas" stat-card-n
  </div>nl ;

\ ============================================================
\ Tab Navigation
\ ============================================================

: emit-tabs ( -- )
  tabs-begin
    s" Overview" s" overview" true tab
    s" Constraints" s" constraints" false tab
    s" Navigation" s" navigation" false tab
    s" Commands" s" commands" false tab
    s" Glossary" s" glossary" false tab
    s" Personas" s" personas" false tab
  tabs-end ;

\ ============================================================
\ Overview Panel
\ ============================================================

: project-card ( row$ -- )
  card-begin
    <h3> 2dup 0 sql-field text 2drop </h3>
    <p>
      2dup 1 sql-field s" bg-primary" badge  \ domain
      2dup 2 sql-field s" bg-danger" badge   \ sensitivity
      2drop
    </p>
    <p> 2dup 3 sql-field text 2drop </p>
    s" subtitle" <p.> 2dup 4 sql-field text 2drop </p> nl
  card-end ;

: panel-overview ( -- )
  s" overview" true panel-begin
    s" Encoded Projects" h2.
    db s" SELECT name, domain, sensitivity, description, purpose FROM projects" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if project-card else 2drop then
    repeat 2drop
    sql-close
  panel-end ;

\ ============================================================
\ Constraints Panel
\ ============================================================

: constraint-row ( row$ -- )
  <tr>
    <td>
      2dup 0 sql-field   \ severity
      2dup s" absolute" str= if s" bg-danger" else s" bg-warning" then
      badge
      2drop
    </td>
    <td> <code> 2dup 1 sql-field text 2drop </code> </td>
    <td> 2dup 2 sql-field text 2drop </td>
  </tr> ;

: panel-constraints ( -- )
  s" constraints" false panel-begin
    s" Project Constraints" h2.
    table-begin
      <thead> <tr> s" Severity" th. s" Type" th. s" Constraint" th. </tr> </thead>
      <tbody>
      db s" SELECT severity, type, content FROM constraints ORDER BY severity DESC" sql-exec
      sql-open
      begin sql-row? while
        dup 0> if constraint-row else 2drop then
      repeat 2drop
      sql-close
      </tbody>
    table-end
  panel-end ;

\ ============================================================
\ Navigation Panel
\ ============================================================

: nav-row ( row$ -- )
  <tr>
    <td> 2dup 0 sql-field s" bg-info" badge 2drop </td>
    <td> <code> 2dup 1 sql-field text 2drop </code> </td>
    <td> 2dup 2 sql-field text 2drop </td>
  </tr> ;

: panel-navigation ( -- )
  s" navigation" false panel-begin
    s" Key File Locations" h2.
    table-begin
      <thead> <tr> s" Category" th. s" Path" th. s" Description" th. </tr> </thead>
      <tbody>
      db s" SELECT category, path, description FROM navigation ORDER BY category" sql-exec
      sql-open
      begin sql-row? while
        dup 0> if nav-row else 2drop then
      repeat 2drop
      sql-close
      </tbody>
    table-end
  panel-end ;

\ ============================================================
\ Commands Panel
\ ============================================================

: cmd-row ( row$ -- )
  <tr>
    <td> <strong> 2dup 0 sql-field text 2drop </strong> </td>
    <td> <code> 2dup 1 sql-field text 2drop </code> </td>
    <td> 2dup 2 sql-field s" bg-success" badge 2drop </td>
  </tr> ;

: panel-commands ( -- )
  s" commands" false panel-begin
    s" Build & Development Commands" h2.
    table-begin
      <thead> <tr> s" Name" th. s" Command" th. s" Category" th. </tr> </thead>
      <tbody>
      db s" SELECT name, command, category FROM commands ORDER BY category, name" sql-exec
      sql-open
      begin sql-row? while
        dup 0> if cmd-row else 2drop then
      repeat 2drop
      sql-close
      </tbody>
    table-end
  panel-end ;

\ ============================================================
\ Glossary Panel
\ ============================================================

: term-card ( row$ -- )
  card-begin
    s" term" <span.> 2dup 0 sql-field text 2drop </span>
    <p> 2dup 1 sql-field text 2drop </p>
  card-end ;

: panel-glossary ( -- )
  s" glossary" false panel-begin
    s" Domain Terminology" h2.
    s" grid-auto" <div.>nl
    db s" SELECT term, definition FROM glossary ORDER BY term" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if term-card else 2drop then
    repeat 2drop
    sql-close
    </div>nl
  panel-end ;

\ ============================================================
\ Personas Panel
\ ============================================================

: persona-card ( row$ -- )
  card-begin
    <h3> 2dup 0 sql-field text 2drop </h3>
    <p> 2dup 1 sql-field text 2drop </p>
    s" subtitle" <p.>
      <strong> s" Goals: " text </strong>
      2dup 2 sql-field text 2drop
    </p> nl
    s" subtitle" <p.>
      <strong> s" Pain Points: " text </strong>
      2dup 3 sql-field text 2drop
    </p> nl
  card-end ;

: panel-personas ( -- )
  s" personas" false panel-begin
    s" User Personas" h2.
    db s" SELECT name, description, goals, pain_points FROM personas" sql-exec
    sql-open
    begin sql-row? while
      dup 0> if persona-card else 2drop then
    repeat 2drop
    sql-close
  panel-end ;

\ ============================================================
\ Main Layout
\ ============================================================

: generate ( -- )
  output-file w/o create-file throw html>file

  s" Project Dashboard" html-head
  page-css
  html-body

  dashboard-begin
    page-header
    dashboard-main-begin
      emit-stats
      nl s" <br>" raw nl  \ spacing
      emit-tabs
      panel-overview
      panel-constraints
      panel-navigation
      panel-commands
      panel-glossary
      panel-personas
    dashboard-main-end
  dashboard-end

  ui-js
  html-end

  html-fid @ close-file throw ;

: view ( -- )
  output-file open-file-cmd ;

: main ( -- )
  ." Project Dashboard" cr
  ." Generating..." cr
  generate
  ." Opening browser..." cr
  view
  ." Done." cr ;

main bye
