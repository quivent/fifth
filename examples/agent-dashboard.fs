\ examples/agent-dashboard.fs - Demo database viewer
\
\ Demonstrates querying the demo databases (projects.db, agents.db)
\ and rendering results as an HTML dashboard.
\
\ Usage: ./fifth examples/agent-dashboard.fs
\ Output: /tmp/agent-dashboard.html (opens in browser)

require ~/.fifth/lib/pkg.fs
use lib:core.fs
use lib:ui.fs

\ Database paths - use demo databases from data/
: demo-projects-db s" data/projects.db" ;
: demo-agents-db   s" data/agents.db" ;

\ Output file
s" /tmp/agent-dashboard.html" w/o create-file throw html>file

\ =============================================================================
\ HTML Document
\ =============================================================================

s" Agent & Project Dashboard" html-head

\ Custom styles
<style>
s"
.agent-card {
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
  border: 1px solid #0f3460;
  border-radius: 12px;
  padding: 1.5rem;
  margin: 0.5rem;
  min-width: 280px;
}
.agent-avatar { font-size: 2.5rem; margin-bottom: 0.5rem; }
.agent-name { font-size: 1.25rem; font-weight: bold; color: #e94560; }
.agent-role { color: #888; font-size: 0.9rem; margin-bottom: 0.5rem; }
.agent-desc { color: #ccc; font-size: 0.85rem; line-height: 1.4; }
.project-card {
  background: #0f3460;
  border-radius: 8px;
  padding: 1rem;
  margin: 0.5rem 0;
}
.project-name { color: #e94560; font-weight: bold; }
.project-domain {
  display: inline-block;
  background: #1a1a2e;
  padding: 0.2rem 0.6rem;
  border-radius: 4px;
  font-size: 0.75rem;
  color: #888;
}
.constraint-list { margin: 0.5rem 0; padding-left: 1.5rem; }
.constraint-item { color: #ff6b6b; font-size: 0.85rem; margin: 0.25rem 0; }
.nav-item { color: #4ecdc4; font-size: 0.85rem; }
" raw
</style>

ui-css
html-body

\ Page header
<div> s" class" s" container" attr>
  s" Demo Database Dashboard" h1.
  s" Viewing projects.db and agents.db from the data/ folder" p.
</div>

\ =============================================================================
\ Agents Section
\ =============================================================================

<div> s" class" s" container" attr>
  s" Functional Agents" h2.
  s" 12 generic software development agents" p.

  <div> s" class" s" grid-auto" attr>

  \ Query agents
  demo-agents-db s" SELECT avatar, name, role, description FROM agents ORDER BY priority DESC" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      <div> s" class" s" agent-card" attr>
        <div> s" class" s" agent-avatar" attr>
          2dup 0 sql-field raw   \ avatar emoji
        </div>
        <div> s" class" s" agent-name" attr>
          2dup 1 sql-field text   \ name
        </div>
        <div> s" class" s" agent-role" attr>
          2dup 2 sql-field text   \ role
        </div>
        <div> s" class" s" agent-desc" attr>
          2dup 3 sql-field text   \ description
        </div>
      </div>
      2drop
    else 2drop then
  repeat 2drop
  sql-close

  </div>
</div>

\ =============================================================================
\ Projects Section
\ =============================================================================

<div> s" class" s" container" attr>
  s" Encoded Projects" h2.

  \ Query projects
  demo-projects-db s" SELECT id, name, domain, description FROM projects" sql-exec
  sql-open
  begin sql-row? while
    dup 0> if
      <div> s" class" s" project-card" attr>
        <span> s" class" s" project-name" attr>
          2dup 1 sql-field text   \ name
        </span>
        s"  " raw
        <span> s" class" s" project-domain" attr>
          2dup 2 sql-field text   \ domain
        </span>
        <p>
          2dup 3 sql-field text   \ description
        </p>

        \ Get constraints for this project
        2dup 0 sql-field         \ project_id
        2>r                      \ save for later

        s" Constraints:" <strong> raw </strong>
        <ul> s" class" s" constraint-list" attr>

        \ Build constraint query
        str-reset
        s" data/projects.db" str+
        2r>                      \ restore project_id

        \ Note: We'd need to query constraints here, but for simplicity
        \ we'll show a static example since nested queries are complex

        </ul>
      </div>
      2drop
    else 2drop then
  repeat 2drop
  sql-close

</div>

\ =============================================================================
\ Stats Section
\ =============================================================================

<div> s" class" s" container" attr>
  s" Database Statistics" h2.

  grid-auto-begin

  \ Count agents
  demo-agents-db s" SELECT COUNT(*) FROM agents" sql-count
  s" Agents" stat-card-n

  \ Count projects
  demo-projects-db s" SELECT COUNT(*) FROM projects" sql-count
  s" Projects" stat-card-n

  \ Count constraints
  demo-projects-db s" SELECT COUNT(*) FROM constraints" sql-count
  s" Constraints" stat-card-n

  \ Count navigation entries
  demo-projects-db s" SELECT COUNT(*) FROM navigation" sql-count
  s" Navigation" stat-card-n

  grid-end
</div>

\ Footer
<div> s" class" s" container" attr>
  <hr>
  s" Generated by Fifth - data/projects.db, data/agents.db" p.
</div>

ui-js
html-end

\ Close file and open in browser
html-fid @ close-file throw
s" open /tmp/agent-dashboard.html" system

bye
