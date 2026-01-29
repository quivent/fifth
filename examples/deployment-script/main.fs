\ fifth/examples/deployment-script/main.fs
\ Deployment orchestration script

require ~/.fifth/lib/core.fs

\ Configuration
: staging-host    ( -- addr u ) s" staging.example.com" ;
: production-host ( -- addr u ) s" prod.example.com" ;
: deploy-user     ( -- addr u ) s" deploy" ;
: app-dir         ( -- addr u ) s" /var/www/app" ;

\ State
variable deploy-ok
variable current-step

: step ( n addr u -- )
  current-step !
  s" [" type swap . s" ] " type type cr ;

: fail ( addr u -- )
  s" FAILED: " type type cr
  0 deploy-ok ! ;

: success ( addr u -- )
  s" OK: " type type cr ;

\ --- Pre-flight Checks ---

: check-git-clean ( -- flag )
  \ Ensure no uncommitted changes
  s" Checking git status..." type cr
  \ TODO: Actually run git status
  true ;

: check-tests ( -- flag )
  \ Run test suite
  s" Running tests..." type cr
  \ TODO: Actually run tests
  true ;

: preflight ( -- flag )
  1 s" Pre-flight checks" step
  check-git-clean 0= if s" Uncommitted changes" fail false exit then
  check-tests 0= if s" Tests failed" fail false exit then
  s" All pre-flight checks passed" success
  true ;

\ --- Build ---

: build-app ( -- flag )
  2 s" Building application" step
  s" npm run build 2>&1" system drop
  \ TODO: Check exit code
  s" Build complete" success
  true ;

\ --- Backup ---

: backup-current ( host-addr host-u -- flag )
  3 s" Backing up current version" step
  s" Creating backup..." type cr
  str-reset
  s" ssh " str+
  deploy-user str+ s" @" str+
  str+  \ host
  s"  'cp -r " str+ app-dir str+ s"  " str+ app-dir str+ s" .bak'" str+
  str$ system drop
  s" Backup created" success
  true ;

\ --- Deploy ---

: deploy-files ( host-addr host-u -- flag )
  4 s" Deploying files" step
  s" Copying files..." type cr
  str-reset
  s" rsync -avz --delete dist/ " str+
  deploy-user str+ s" @" str+
  str+  \ host
  s" :" str+ app-dir str+ s" /" str+
  str$ system drop
  s" Files deployed" success
  true ;

\ --- Health Check ---

: health-check ( host-addr host-u -- flag )
  5 s" Running health checks" step
  s" Checking application health..." type cr
  str-reset
  s" curl -sf http://" str+
  str+  \ host
  s" /health > /dev/null" str+
  str$ system
  0= if
    s" Health check passed" success
    true
  else
    s" Health check failed" fail
    false
  then ;

\ --- Rollback ---

: rollback ( host-addr host-u -- )
  s" Rolling back..." type cr
  str-reset
  s" ssh " str+
  deploy-user str+ s" @" str+
  str+  \ host
  s"  'rm -rf " str+ app-dir str+ s"  && mv " str+ app-dir str+ s" .bak " str+ app-dir str+ s" '" str+
  str$ system drop
  s" Rollback complete" type cr ;

\ --- Main Deploy Flow ---

: deploy-to ( host-addr host-u -- )
  true deploy-ok !

  s" Deploying to: " type 2dup type cr
  s" ================================" type cr

  preflight 0= if exit then
  build-app 0= if exit then
  2dup backup-current 0= if 2drop exit then
  2dup deploy-files 0= if 2drop exit then
  2dup health-check 0= if
    s" Deployment failed, initiating rollback" type cr
    rollback
    exit
  then
  2drop

  s" ================================" type cr
  s" Deployment successful!" type cr ;

: confirm-production ( -- flag )
  s" WARNING: You are about to deploy to PRODUCTION" type cr
  s" Type 'yes' to continue: " type
  \ TODO: Read user input
  \ For safety, default to no
  false ;

\ --- Commands ---

: cmd-staging ( -- )
  staging-host deploy-to ;

: cmd-production ( -- )
  confirm-production if
    production-host deploy-to
  else
    s" Deployment cancelled" type cr
  then ;

: cmd-rollback ( -- )
  s" Which environment? (staging/production): " type
  \ TODO: Read input
  staging-host rollback ;

: usage ( -- )
  s" Deployment Script" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth deployment-script/main.fs staging" type cr
  s"   ./fifth deployment-script/main.fs production" type cr
  s"   ./fifth deployment-script/main.fs rollback" type cr ;

: main ( -- )
  argc @ 2 < if
    usage exit
  then
  1 argv
  2dup s" staging" compare 0= if 2drop cmd-staging exit then
  2dup s" production" compare 0= if 2drop cmd-production exit then
  2dup s" rollback" compare 0= if 2drop cmd-rollback exit then
  2drop usage ;

main
bye
