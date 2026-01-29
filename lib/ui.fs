\ fifth/lib/ui.fs - UI Component Library
\ Cards, badges, navs, tabs, tables, dashboards

require ~/fifth/lib/html.fs
require ~/fifth/lib/template.fs

\ ============================================================
\ Badges
\ ============================================================

: badge ( text$ class$ -- )
  s" badge " str-reset str+ str+ str$
  <span.> text </span> ;

: badge-primary ( text$ -- ) s" bg-primary" badge ;
: badge-success ( text$ -- ) s" bg-success" badge ;
: badge-warning ( text$ -- ) s" bg-warning" badge ;
: badge-danger ( text$ -- ) s" bg-danger" badge ;
: badge-info ( text$ -- ) s" bg-info" badge ;

\ ============================================================
\ Cards
\ ============================================================

: card-begin ( -- ) s" card" <div.> nl ;
: card-end ( -- ) </div> nl ;

: card-header ( title$ -- )
  s" card-header" <div.>
    <h3> text </h3>
  </div> nl ;

: card-body-begin ( -- ) s" card-body" <div.> nl ;
: card-body-end ( -- ) </div> nl ;

: card ( title$ body-xt -- )
  \ Complete card with title and body
  s" card" <div.> nl 
    <h3> 2swap text </h3>
    execute
  </div> nl ;

: card-with-badge ( title$ badge$ badge-class$ body-xt -- )
  \ Card with badge after title
  s" card" <div.> nl 
    <h3>
      2rot 2rot text  \ title
      s"  " raw
      badge           \ badge with class
    </h3>
    execute           \ body
  </div> nl ;

\ ============================================================
\ Stat Cards (for dashboards)
\ ============================================================

: stat-card ( value$ label$ -- )
  s" stat-card" <div.>
    s" stat-value" <div.> 2swap text </div>
    s" stat-label" <div.> text </div>
  </div> nl ;

: stat-card-n ( n label$ -- )
  \ Stat card with numeric value
  2>r n>str 2r> stat-card ;

\ ============================================================
\ Navigation
\ ============================================================

: nav-begin ( -- ) s" nav" <nav> nl ;
: nav-end ( -- ) </nav> nl ;

: nav-item ( text$ href$ active? -- )
  if s" nav-item active" else s" nav-item" then
  <a 2swap href= s"  class='" raw raw s" '" raw a>
  text </a> nl ;

: nav-item-js ( text$ panel-id$ active? -- )
  \ Nav item that shows/hides panels via JS
  if s" nav-item active" else s" nav-item" then
  <a s"  class='" raw raw s" '" raw
     s\" onclick=\"" raw
     s" showPanel('" raw 2swap raw s\" ')\"" raw
  a> text </a> nl ;

\ ============================================================
\ Sidebar Layout
\ ============================================================

: sidebar-begin ( -- )
  s" sidebar" <aside.> ;

: sidebar-end ( -- )
  </aside> nl ;

: sidebar-section ( title$ -- )
  s" sidebar-section" <div.> text </div> nl ;

\ ============================================================
\ Tabs
\ ============================================================

variable current-tab

: tabs-begin ( -- ) s" tabs" <div.> nl ;
: tabs-end ( -- ) </div> nl ;

: tab ( text$ id$ active? -- )
  if s" tab active" else s" tab" then  \ ( text$ id$ class$ )
  <button s"  class='" raw raw s" '" raw  \ output class ( text$ id$ )
          s\" onclick=\"showPanel('" raw
          raw  \ output id$ ( text$ remains )
          s\" ')\">" raw
  text  \ output text$
  </button> nl ;

: panel-begin ( id$ active? -- )
  if s" panel active" else s" panel" then
  <div#.> nl ;

: panel-end ( -- ) </div> nl ;

\ ============================================================
\ Tables
\ ============================================================

: table-begin ( -- ) s" table" <table.> nl ;
: table-end ( -- ) </table> nl ;

: table-head-begin ( -- ) <thead> <tr> ;
: table-head-end ( -- ) </tr> </thead> nl ;

: table-body-begin ( -- ) <tbody> nl ;
: table-body-end ( -- ) </tbody> nl ;

: th-list ( n addr -- )
  \ Output n header cells from string array
  \ Usage: 3 headers th-list (where headers is array of string addrs)
  swap 0 ?do
    dup i cells + @ count th.
  loop drop ;

\ ============================================================
\ Grid Layout
\ ============================================================

: grid-begin ( -- ) s" grid" <div.> nl ;
: grid-end ( -- ) </div> nl ;

: grid-2 ( -- ) s" grid grid-2" <div.> nl ;
: grid-3 ( -- ) s" grid grid-3" <div.> nl ;
: grid-4 ( -- ) s" grid grid-4" <div.> nl ;

\ ============================================================
\ Dashboard Layout
\ ============================================================

: dashboard-begin ( -- )
  s" dashboard" <div.> nl ;

: dashboard-end ( -- )
  </div> nl ;

: dashboard-header ( title$ subtitle$ -- )
  s" dashboard-header" <header.> nl 
    <h1> 2swap text </h1>
    s" subtitle" <p.>  text </p> nl 
  </header> nl ;

: dashboard-main-begin ( -- )
  s" dashboard-main" <main.> nl ;

: dashboard-main-end ( -- )
  </main> nl ;

\ ============================================================
\ Common CSS for UI Components
\ ============================================================

: ui-css ( -- )
  <style>
  \ Layout
  s" .dashboard" s" display:flex;min-height:100vh" css-rule
  s" .dashboard-header" s" padding:1.5rem 2rem;border-bottom:1px solid rgba(255,255,255,0.1)" css-rule
  s" .dashboard-header h1" s" margin-bottom:0.25rem" css-rule
  s" .subtitle" s" color:#71717a;font-size:0.85rem" css-rule
  s" .dashboard-main" s" flex:1;padding:2rem;overflow-y:auto" css-rule

  \ Sidebar
  s" .sidebar" s" width:220px;background:#18181b;border-right:1px solid rgba(255,255,255,0.1);padding:1rem 0;flex-shrink:0" css-rule
  s" .sidebar-section" s" padding:0.75rem 1rem;color:#71717a;font-size:0.7rem;text-transform:uppercase;letter-spacing:0.05em" css-rule

  \ Nav
  s" .nav-item" s" display:block;padding:0.6rem 1rem;color:#a1a1aa;text-decoration:none;font-size:0.85rem;cursor:pointer;border-left:2px solid transparent" css-rule
  s" .nav-item:hover" s" background:rgba(255,255,255,0.05)" css-rule
  s" .nav-item.active" s" background:rgba(139,92,246,0.1);border-left-color:#8b5cf6;color:#c4b5fd" css-rule

  \ Tabs
  s" .tabs" s" display:flex;gap:0.5rem;margin-bottom:1.5rem;flex-wrap:wrap" css-rule
  s" .tab" s" padding:0.6rem 1.2rem;background:rgba(255,255,255,0.05);border:none;border-radius:0.5rem;color:#a1a1aa;cursor:pointer;font-size:0.85rem" css-rule
  s" .tab:hover" s" background:rgba(255,255,255,0.1)" css-rule
  s" .tab.active" s" background:linear-gradient(135deg,#6366f1,#8b5cf6);color:white" css-rule

  \ Panels
  s" .panel" s" display:none" css-rule
  s" .panel.active" s" display:block" css-rule

  \ Cards
  s" .card" s" background:#18181b;border:1px solid rgba(255,255,255,0.1);border-radius:0.75rem;padding:1.25rem;margin-bottom:1rem" css-rule
  s" .card h3" s" color:#c4b5fd;font-size:1rem;margin-bottom:0.5rem" css-rule
  s" .card p" s" color:#a1a1aa;font-size:0.875rem;line-height:1.5" css-rule
  s" .card-header" s" border-bottom:1px solid rgba(255,255,255,0.1);padding-bottom:0.75rem;margin-bottom:0.75rem" css-rule

  \ Stat cards
  s" .stat-card" s" background:linear-gradient(135deg,rgba(139,92,246,0.15),rgba(99,102,241,0.1));border:1px solid rgba(139,92,246,0.3);border-radius:0.75rem;padding:1.25rem;text-align:center" css-rule
  s" .stat-value" s" font-size:2rem;font-weight:700;color:#a78bfa" css-rule
  s" .stat-label" s" font-size:0.75rem;color:#71717a;margin-top:0.25rem" css-rule

  \ Badges
  s" .badge" s" display:inline-block;padding:0.2rem 0.6rem;border-radius:9999px;font-size:0.7rem;font-weight:500;margin-right:0.4rem" css-rule
  s" .bg-primary" s" background:#1e40af;color:#93c5fd" css-rule
  s" .bg-success" s" background:#065f46;color:#6ee7b7" css-rule
  s" .bg-warning" s" background:#92400e;color:#fcd34d" css-rule
  s" .bg-danger" s" background:#991b1b;color:#fca5a5" css-rule
  s" .bg-info" s" background:#0e7490;color:#67e8f9" css-rule

  \ Grid
  s" .grid" s" display:grid;gap:1rem" css-rule
  s" .grid-2" s" grid-template-columns:repeat(2,1fr)" css-rule
  s" .grid-3" s" grid-template-columns:repeat(3,1fr)" css-rule
  s" .grid-4" s" grid-template-columns:repeat(4,1fr)" css-rule
  s" @media(max-width:768px)" s" .grid-2,.grid-3,.grid-4{grid-template-columns:1fr}" css-rule

  \ Tables
  s" .table" s" width:100%;border-collapse:collapse;font-size:0.85rem" css-rule
  s" .table th" s" text-align:left;padding:0.75rem;color:#8b5cf6;border-bottom:1px solid rgba(255,255,255,0.15);font-weight:500" css-rule
  s" .table td" s" padding:0.75rem;border-bottom:1px solid rgba(255,255,255,0.05);color:#a1a1aa" css-rule
  s" .table code" s" background:rgba(139,92,246,0.15);padding:0.15rem 0.4rem;border-radius:0.25rem;font-size:0.8rem;color:#c4b5fd" css-rule

  \ Term styling
  s" .term" s" color:#c084fc;font-weight:600" css-rule

  \ Auto-fill grid
  s" .grid-auto" s" display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:1rem" css-rule
  </style> ;

\ ============================================================
\ JavaScript for Tabs/Panels
\ ============================================================

: ui-js ( -- )
  <script>
  s" function showPanel(id){" raw nl
  s"   document.querySelectorAll('.panel').forEach(p=>p.classList.remove('active'));" raw nl
  s"   document.querySelectorAll('.nav-item,.tab').forEach(t=>t.classList.remove('active'));" raw nl
  s"   document.getElementById(id).classList.add('active');" raw nl
  s"   event.target.classList.add('active');" raw nl
  s" }" raw nl
  </script> ;
