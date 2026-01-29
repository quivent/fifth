\ fifth/examples/cron-orchestrator/main.fs
\ Cron job orchestrator

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" jobs.db" ;

\ Job status
variable job-success

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS jobs (" str+
  s"   name TEXT PRIMARY KEY," str+
  s"   schedule TEXT," str+
  s"   last_run TEXT," str+
  s"   last_status TEXT," str+
  s"   run_count INTEGER DEFAULT 0" str+
  s" );" str+
  s" CREATE TABLE IF NOT EXISTS job_history (" str+
  s"   id INTEGER PRIMARY KEY," str+
  s"   job_name TEXT," str+
  s"   status TEXT," str+
  s"   duration_ms INTEGER," str+
  s"   output TEXT," str+
  s"   run_at TEXT DEFAULT CURRENT_TIMESTAMP" str+
  s" );\"" str+
  str$ system drop ;

: record-run ( job-addr job-u status-addr status-u -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" INSERT INTO job_history (job_name, status) VALUES ('" str+
  2swap str+
  s" ', '" str+
  str+
  s" ');" str+
  s" UPDATE jobs SET last_run=datetime('now'), last_status='" str+
  \ TODO: Add status again
  s" ', run_count=run_count+1 WHERE name='" str+
  \ TODO: Add job name again
  s" ';\"" str+
  str$ system drop ;

\ --- Job Definitions ---

: job-backup ( -- success )
  s" [backup] Creating database backup..." type cr
  s" cp jobs.db jobs.db.bak 2>/dev/null || true" system drop
  s" [backup] Done" type cr
  true ;

: job-cleanup ( -- success )
  s" [cleanup] Removing old temp files..." type cr
  s" find /tmp -name '*.tmp' -mtime +1 -delete 2>/dev/null || true" system drop
  s" [cleanup] Done" type cr
  true ;

: job-report ( -- success )
  s" [report] Generating status report..." type cr
  \ Generate HTML report
  s" mkdir -p output" system drop
  s" output/status.html" w/o create-file throw html>file
  s" Job Status" html-head html-body
  <h1> s" Cron Job Status" text </h1>
  <p> s" Report generated at: " text s" now" text </p>
  html-end
  html-fid @ close-file throw
  s" [report] Done" type cr
  true ;

: job-healthcheck ( -- success )
  s" [healthcheck] Checking system health..." type cr
  s" ping -c 1 localhost > /dev/null 2>&1" system
  0= dup if
    s" [healthcheck] System healthy" type cr
  else
    s" [healthcheck] WARNING: Health check failed!" type cr
  then ;

\ --- Job Registry ---

: run-job ( name-addr name-u -- success )
  2dup s" backup" compare 0= if 2drop job-backup exit then
  2dup s" cleanup" compare 0= if 2drop job-cleanup exit then
  2dup s" report" compare 0= if 2drop job-report exit then
  2dup s" healthcheck" compare 0= if 2drop job-healthcheck exit then
  s" Unknown job: " type type cr
  false ;

: execute-job ( name-addr name-u -- )
  s" ========================================" type cr
  s" Running job: " type 2dup type cr
  s" ========================================" type cr

  2dup run-job

  if
    s" success" record-run
    s" Job completed successfully" type cr
  else
    s" failed" record-run
    s" Job FAILED" type cr
  then
  cr ;

\ --- Run All Jobs ---

: run-all-jobs ( -- )
  s" Running all scheduled jobs..." type cr
  cr
  s" backup" execute-job
  s" cleanup" execute-job
  s" report" execute-job
  s" healthcheck" execute-job
  s" All jobs completed" type cr ;

\ --- Status & History ---

: show-status ( -- )
  s" Job Status:" type cr
  s" ===========" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT name, last_status, last_run, run_count FROM jobs;\"" str+
  str$ system drop ;

: show-history ( -- )
  s" Recent Job History:" type cr
  s" ===================" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT job_name, status, run_at FROM job_history ORDER BY id DESC LIMIT 20;\"" str+
  str$ system drop ;

: list-jobs ( -- )
  s" Available Jobs:" type cr
  s" ===============" type cr
  s"   backup      - Backup database" type cr
  s"   cleanup     - Clean temp files" type cr
  s"   report      - Generate status report" type cr
  s"   healthcheck - System health check" type cr ;

\ --- Main ---

: usage ( -- )
  s" Cron Job Orchestrator" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth cron-orchestrator/main.fs run [job]  - Run jobs (all or specific)" type cr
  s"   ./fifth cron-orchestrator/main.fs status     - Show job status" type cr
  s"   ./fifth cron-orchestrator/main.fs history    - Show run history" type cr
  s"   ./fifth cron-orchestrator/main.fs list       - List available jobs" type cr ;

: main ( -- )
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" run" compare 0= if
    2drop
    argc @ 3 < if
      run-all-jobs
    else
      2 argv execute-job
    then
    exit
  then
  2dup s" status" compare 0= if 2drop show-status exit then
  2dup s" history" compare 0= if 2drop show-history exit then
  2dup s" list" compare 0= if 2drop list-jobs exit then
  2drop usage ;

main
bye
