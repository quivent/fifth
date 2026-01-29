\ fifth/examples/server-health/main.fs
\ Server health dashboard generator

require ~/.fifth/lib/core.fs

\ Configuration
: output-file ( -- addr u ) s" output/health.html" ;
: db-file     ( -- addr u ) s" metrics.db" ;

\ Metric storage
256 constant metric-len
create disk-usage metric-len allot
create mem-usage metric-len allot
create load-avg metric-len allot
create uptime-str metric-len allot

\ --- Metric Collection ---

: run-cmd ( cmd-addr cmd-u buf buf-len -- actual-len )
  \ Run command and capture output to buffer
  \ This is a simplified version - real impl would use pipes
  2drop 2drop 0 ;  \ placeholder

: collect-disk ( -- )
  \ Get disk usage percentage for root
  s" df -h / | tail -1 | awk '{print $5}'" system drop
  s" 45%" disk-usage swap move ;

: collect-memory ( -- )
  \ Get memory usage
  s" free -m | awk 'NR==2{printf \"%d/%dMB (%.1f%%)\", $3,$2,$3*100/$2}'" system drop
  s" 4096/8192MB (50%)" mem-usage swap move ;

: collect-load ( -- )
  \ Get load average
  s" uptime | awk -F'load average:' '{print $2}'" system drop
  s" 0.52, 0.58, 0.59" load-avg swap move ;

: collect-uptime ( -- )
  \ Get system uptime
  s" uptime | awk -F'up ' '{print $2}' | awk -F',' '{print $1}'" system drop
  s" 15 days" uptime-str swap move ;

: collect-all ( -- )
  s" Collecting metrics..." type cr
  collect-disk
  collect-memory
  collect-load
  collect-uptime ;

\ --- Dashboard Generation ---

: dashboard-styles ( -- )
  <style>
  s" :root { --ok: #4caf50; --warn: #ff9800; --critical: #f44336; }" raw nl
  s" body { font-family: system-ui; background: #1a1a2e; color: #eee; padding: 2rem; }" raw nl
  s" h1 { margin-bottom: 2rem; }" raw nl
  s" .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; }" raw nl
  s" .card { background: #16213e; padding: 1.5rem; border-radius: 8px; }" raw nl
  s" .card h3 { margin: 0 0 1rem 0; color: #888; font-size: 0.9rem; text-transform: uppercase; }" raw nl
  s" .metric { font-size: 2rem; font-weight: bold; }" raw nl
  s" .ok { color: var(--ok); }" raw nl
  s" .warn { color: var(--warn); }" raw nl
  s" .critical { color: var(--critical); }" raw nl
  s" .timestamp { margin-top: 2rem; color: #666; font-size: 0.8rem; }" raw nl
  </style> ;

: metric-card ( value-addr value-u label-addr label-u class-addr class-u -- )
  <div.> s" card" raw q s" >" raw nl
  <h3> 2swap text </h3> nl  \ label (now on top of stack after class)
  s" <div class=" raw q s" metric " str-reset str+ str$ raw q s" >" raw
  2swap text  \ value
  s" </div>" raw nl
  </div> nl ;

: generate-dashboard ( -- )
  str-reset s" output/" str+ s" health.html" str+ str$
  w/o create-file throw html>file

  s" Server Health" html-head
  dashboard-styles
  s" <meta http-equiv=" raw q s" refresh" raw q s"  content=" raw q s" 60" raw q s" >" raw nl
  html-body

  <h1> s" Server Health Dashboard" text </h1>

  <div.> s" grid" raw q s" >" raw nl
    disk-usage metric-len s" Disk Usage" s" ok" metric-card
    mem-usage metric-len s" Memory" s" ok" metric-card
    load-avg metric-len s" Load Average" s" ok" metric-card
    uptime-str metric-len s" Uptime" s" ok" metric-card
  </div> nl

  <div.> s" timestamp" raw q s" >" raw
    s" Last updated: " text
    \ TODO: Get current timestamp
    s" 2024-01-15 10:30:00" text
  </div> nl

  html-end
  html-fid @ close-file throw ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: main ( -- )
  ensure-output
  collect-all
  generate-dashboard
  s" Dashboard generated: output/health.html" type cr ;

main
bye
