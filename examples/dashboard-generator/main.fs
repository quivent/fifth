\ fifth/examples/dashboard-generator/main.fs
\ Dashboard generator - metrics visualization

require ~/.fifth/lib/core.fs

\ Configuration
: output-file ( -- addr u ) s" /tmp/dashboard.html" ;
: db-path     ( -- addr u ) s" metrics.db" ;

\ --- Chart.js CDN ---

: chart-cdn ( -- )
  s" <script src=" raw q
  s" https://cdn.jsdelivr.net/npm/chart.js" raw
  q s" ></script>" raw nl ;

\ --- Widget Components ---

: stat-card ( value-addr value-u label-addr label-u -- )
  \ Render a stat card widget
  <div.> s" stat-card" raw q s" >" raw nl
    <div.> s" stat-value" raw q s" >" raw
    2swap text  \ value
    </div> nl
    <div.> s" stat-label" raw q s" >" raw
    text        \ label
    </div> nl
  </div> nl ;

: chart-container ( id-addr id-u -- )
  \ Placeholder for Chart.js canvas
  s" <canvas id=" raw q 2dup raw q
  s"  width=" raw q s" 400" raw q
  s"  height=" raw q s" 200" raw q
  s" ></canvas>" raw nl ;

\ --- Data Fetching ---

: fetch-api ( url-addr url-u -- result-addr result-u )
  \ Shell to curl, return response
  \ TODO: Implement with proper error handling
  str-reset
  s" curl -s " str+
  str+
  str$ system drop
  s" {}" ;  \ placeholder

: query-metric ( sql-addr sql-u -- value-addr value-u )
  \ Query SQLite for single value
  \ TODO: Implement with sql.fs
  2drop s" 42" ;

\ --- Dashboard Layout ---

: dashboard-styles ( -- )
  <style>
  s" :root { --bg: #1a1a2e; --card: #16213e; --text: #eee; --accent: #0f3460; }" raw nl
  s" body { font-family: system-ui; background: var(--bg); color: var(--text); padding: 2rem; }" raw nl
  s" .dashboard { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 1rem; }" raw nl
  s" .stat-card { background: var(--card); padding: 1.5rem; border-radius: 8px; }" raw nl
  s" .stat-value { font-size: 2.5rem; font-weight: bold; }" raw nl
  s" .stat-label { color: #888; margin-top: 0.5rem; }" raw nl
  s" .chart-card { background: var(--card); padding: 1.5rem; border-radius: 8px; grid-column: span 2; }" raw nl
  </style> ;

: dashboard-content ( -- )
  <main.> s" dashboard" raw q s" >" raw nl
    \ Stat cards
    s" 1,234" s" Total Users" stat-card
    s" 567" s" Active Today" stat-card
    s" 89%" s" Uptime" stat-card
    s" 12ms" s" Avg Response" stat-card

    \ Chart placeholder
    <div.> s" chart-card" raw q s" >" raw nl
      <h3> s" Activity Over Time" text </h3>
      s" activityChart" chart-container
    </div> nl
  </main> nl ;

: dashboard-scripts ( -- )
  chart-cdn
  <script>
  s" // Chart.js initialization would go here" raw nl
  s" // const ctx = document.getElementById('activityChart');" raw nl
  </script> ;

\ --- Main ---

: generate-dashboard ( -- )
  output-file w/o create-file throw html>file

  s" Metrics Dashboard" html-head
  dashboard-styles
  html-body

  <h1> s" System Dashboard" text </h1>
  dashboard-content
  dashboard-scripts

  html-end
  html-fid @ close-file throw

  s" Dashboard generated: " type output-file type cr ;

generate-dashboard
bye
