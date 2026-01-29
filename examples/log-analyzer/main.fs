\ fifth/examples/log-analyzer/main.fs
\ Log analyzer - parse and summarize logs

require ~/.fifth/lib/core.fs

\ Configuration
variable log-fid
variable total-lines
variable error-count
variable warn-count
variable info-count

\ --- Log Level Detection ---

: contains? ( addr u pattern-addr pattern-u -- flag )
  \ Check if string contains pattern
  \ Simple implementation - check each position
  2>r 2dup + 2r@ nip - 1+ 0 ?do
    2dup i + 2r@ compare 0= if
      2drop 2r> 2drop true unloop exit
    then
  loop
  2drop 2r> 2drop false ;

: error-line? ( addr u -- flag )
  s" [ERROR]" contains? ;

: warn-line? ( addr u -- flag )
  s" [WARN]" contains? ;

: info-line? ( addr u -- flag )
  s" [INFO]" contains? ;

\ --- Line Processing ---

: process-line ( addr u -- )
  1 total-lines +!
  2dup error-line? if 1 error-count +! then
  2dup warn-line?  if 1 warn-count +!  then
  2dup info-line?  if 1 info-count +!  then
  2drop ;

\ --- File Reading ---

256 constant max-line
create line-buf max-line allot

: read-log-file ( filename-addr filename-u -- )
  r/o open-file throw log-fid !
  begin
    line-buf max-line log-fid @ read-line throw
  while
    line-buf swap process-line
  repeat
  drop
  log-fid @ close-file throw ;

\ --- Report Generation ---

: report-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 900px; margin: 0 auto; padding: 2rem; }" raw nl
  s" .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin: 2rem 0; }" raw nl
  s" .stat { padding: 1rem; border-radius: 8px; text-align: center; }" raw nl
  s" .stat-value { font-size: 2rem; font-weight: bold; }" raw nl
  s" .stat-label { color: #666; }" raw nl
  s" .total { background: #e3f2fd; }" raw nl
  s" .errors { background: #ffebee; }" raw nl
  s" .warns { background: #fff3e0; }" raw nl
  s" .infos { background: #e8f5e9; }" raw nl
  </style> ;

: stat-box ( value label-addr label-u class-addr class-u -- )
  s" <div class=" raw q s" stat " str-reset str+ str$ raw q s" >" raw nl
  s" <div class=" raw q s" stat-value" raw q s" >" raw . s" </div>" raw nl
  s" <div class=" raw q s" stat-label" raw q s" >" raw text s" </div>" raw nl
  s" </div>" raw nl ;

: generate-report ( -- )
  s" /tmp/log-report.html" w/o create-file throw html>file

  s" Log Analysis Report" html-head
  report-styles
  html-body

  <h1> s" Log Analysis Report" text </h1>

  <div.> s" stats" raw q s" >" raw nl
    total-lines @ s" Total Lines" s" total" stat-box
    error-count @ s" Errors" s" errors" stat-box
    warn-count @  s" Warnings" s" warns" stat-box
    info-count @  s" Info" s" infos" stat-box
  </div> nl

  <h2> s" Summary" text </h2>
  <p>
    s" Processed " text total-lines @ . s"  log entries." text
  </p>

  html-end
  html-fid @ close-file throw

  s" Report generated: /tmp/log-report.html" type cr ;

\ --- Main ---

: init-counters ( -- )
  0 total-lines !
  0 error-count !
  0 warn-count !
  0 info-count ! ;

: analyze ( filename-addr filename-u -- )
  init-counters
  s" Analyzing: " type 2dup type cr
  read-log-file
  generate-report ;

: usage ( -- )
  s" Usage: ./fifth log-analyzer/main.fs <logfile>" type cr ;

: main ( -- )
  argc @ 2 < if
    \ No file provided, use sample
    s" sample.log" analyze
  else
    \ Use provided filename
    1 argv analyze
  then ;

main
bye
