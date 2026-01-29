\ fifth/examples/kiosk-display/main.fs
\ Kiosk display system

require ~/.fifth/lib/core.fs

\ Configuration
: output-file ( -- addr u ) s" output/display.html" ;
: refresh-seconds ( -- n ) 60 ;

\ --- Display Styles ---

: kiosk-styles ( -- )
  <style>
  s" * { margin: 0; padding: 0; box-sizing: border-box; }" raw nl
  s" body { " raw nl
  s"   font-family: system-ui; " raw nl
  s"   background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); " raw nl
  s"   color: white; " raw nl
  s"   min-height: 100vh; " raw nl
  s"   display: flex; " raw nl
  s"   flex-direction: column; " raw nl
  s" }" raw nl
  s" .header { padding: 1rem 2rem; display: flex; justify-content: space-between; align-items: center; }" raw nl
  s" .title { font-size: 2rem; font-weight: bold; }" raw nl
  s" .clock { font-size: 1.5rem; font-family: monospace; }" raw nl
  s" .content { flex: 1; display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; padding: 1rem 2rem; }" raw nl
  s" .card { background: rgba(255,255,255,0.1); border-radius: 12px; padding: 1.5rem; }" raw nl
  s" .card h2 { font-size: 1rem; color: #888; text-transform: uppercase; margin-bottom: 0.5rem; }" raw nl
  s" .card .value { font-size: 3rem; font-weight: bold; }" raw nl
  s" .card .subtext { color: #666; margin-top: 0.5rem; }" raw nl
  s" .card.wide { grid-column: span 2; }" raw nl
  s" .card.tall { grid-row: span 2; }" raw nl
  s" .footer { padding: 1rem 2rem; text-align: center; color: #666; font-size: 0.8rem; }" raw nl
  s" .announcements { max-height: 200px; overflow: hidden; }" raw nl
  s" .announcement { padding: 0.5rem 0; border-bottom: 1px solid rgba(255,255,255,0.1); }" raw nl
  </style> ;

: kiosk-scripts ( -- )
  <script>
  s" function updateClock() {" raw nl
  s"   const now = new Date();" raw nl
  s"   const time = now.toLocaleTimeString('en-US', {hour: '2-digit', minute: '2-digit', second: '2-digit'});" raw nl
  s"   const date = now.toLocaleDateString('en-US', {weekday: 'long', month: 'long', day: 'numeric'});" raw nl
  s"   document.getElementById('clock').innerHTML = time + '<br><small>' + date + '</small>';" raw nl
  s" }" raw nl
  s" setInterval(updateClock, 1000);" raw nl
  s" updateClock();" raw nl
  </script> ;

\ --- Display Components ---

: display-card ( value-addr value-u label-addr label-u -- )
  <div.> s" card" raw q s" >" raw nl
    <h2> text </h2> nl
    <div.> s" value" raw q s" >" raw text </div> nl
  </div> nl ;

: display-card-wide ( value-addr value-u label-addr label-u -- )
  <div.> s" card wide" raw q s" >" raw nl
    <h2> text </h2> nl
    <div.> s" value" raw q s" >" raw text </div> nl
  </div> nl ;

: announcement-item ( text-addr text-u -- )
  <div.> s" announcement" raw q s" >" raw
  text
  </div> nl ;

: announcements-card ( -- )
  <div.> s" card wide announcements" raw q s" >" raw nl
    <h2> s" Announcements" text </h2> nl
    s" Welcome to our facility!" announcement-item
    s" Cafeteria closes at 6 PM today" announcement-item
    s" Safety first - wear your badge" announcement-item
  </div> nl ;

\ --- Data Fetching ---

: get-temperature ( -- addr u )
  \ Fetch current temperature
  s" 72F" ;

: get-visitors ( -- addr u )
  \ Count from database
  s" 1,234" ;

: get-events ( -- addr u )
  \ Today's event count
  s" 5" ;

: get-alerts ( -- addr u )
  \ Active alerts
  s" 0" ;

\ --- Main Display ---

: generate-display ( -- )
  str-reset s" output/" str+ s" display.html" str+ str$
  w/o create-file throw html>file

  s" Information Display" html-head
  kiosk-styles
  \ Auto-refresh
  s" <meta http-equiv=" raw q s" refresh" raw q
  s"  content=" raw q refresh-seconds . q s" >" raw nl
  html-body

  \ Header
  <header.> s" header" raw q s" >" raw nl
    <div.> s" title" raw q s" >" raw s" Building Info" text </div> nl
    <div.> s" clock" raw q s"  id=" raw q s" clock" raw q s" >" raw s" --:--:--" text </div> nl
  </header> nl

  \ Content grid
  <main.> s" content" raw q s" >" raw nl
    get-temperature s" Temperature" display-card
    get-visitors s" Visitors Today" display-card
    get-events s" Events Today" display-card
    get-alerts s" Active Alerts" display-card
    announcements-card
  </main> nl

  \ Footer
  <footer.> s" footer" raw q s" >" raw nl
    s" Auto-refreshes every " text refresh-seconds . s"  seconds" text
  </footer> nl

  kiosk-scripts
  html-end

  html-fid @ close-file throw ;

\ --- Browser Launch ---

: launch-chromium ( -- )
  str-reset
  s" chromium --kiosk --noerrdialogs --disable-infobars file://" str+
  s" output/display.html" str+
  s"  &" str+
  str$ system drop
  s" Launched Chromium in kiosk mode" type cr ;

: launch-firefox ( -- )
  str-reset
  s" firefox --kiosk file://" str+
  s" output/display.html" str+
  s"  &" str+
  str$ system drop
  s" Launched Firefox in kiosk mode" type cr ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: usage ( -- )
  s" Kiosk Display System" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth kiosk-display/main.fs           - Generate display" type cr
  s"   ./fifth kiosk-display/main.fs chromium  - Launch in Chromium" type cr
  s"   ./fifth kiosk-display/main.fs firefox   - Launch in Firefox" type cr ;

: main ( -- )
  ensure-output
  generate-display
  s" Generated: output/display.html" type cr

  argc @ 2 < if exit then

  1 argv
  2dup s" chromium" compare 0= if 2drop launch-chromium exit then
  2dup s" firefox" compare 0= if 2drop launch-firefox exit then
  2drop ;

main
bye
