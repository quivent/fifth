\ fifth/examples/agentic-coder/tools/git.fs
\ Git operations for agentic coder

\ --- Status ---

: git-status ( -- )
  s" git status --short" system drop ;

: tool-git-status ( -- json-addr json-u )
  \ Get git status as JSON
  s" {\"status\": \"success\", \"clean\": false, \"changes\": []}"
  \ TODO: Parse git status output
  ;

\ --- Diff ---

: git-diff ( -- )
  s" git diff" system drop ;

: git-diff-staged ( -- )
  s" git diff --staged" system drop ;

: git-diff-file ( path-addr path-u -- )
  str-reset
  s" git diff " str+
  str+
  str$ system drop ;

: tool-git-diff ( path-addr path-u -- json-addr json-u )
  s" {\"status\": \"success\", \"diff\": \"...\"}"
  \ TODO: Capture diff output
  2drop ;

\ --- Log ---

: git-log ( n -- )
  str-reset
  s" git log --oneline -" str+
  0 <# #s #> str+
  str$ system drop ;

: git-log-file ( path-addr path-u n -- )
  str-reset
  s" git log --oneline -" str+
  swap 0 <# #s #> str+
  s"  -- " str+
  str+
  str$ system drop ;

: tool-git-log ( n -- json-addr json-u )
  drop s" {\"status\": \"success\", \"commits\": []}"
  \ TODO: Parse git log output
  ;

\ --- Branch Operations ---

: git-branch ( -- )
  s" git branch -a" system drop ;

: git-current-branch ( -- )
  s" git branch --show-current" system drop ;

: git-checkout ( branch-addr branch-u -- )
  str-reset
  s" git checkout " str+
  str+
  str$ system drop ;

: git-create-branch ( name-addr name-u -- )
  str-reset
  s" git checkout -b " str+
  str+
  str$ system drop ;

: tool-git-branch ( -- json-addr json-u )
  s" {\"status\": \"success\", \"current\": \"\", \"branches\": []}" ;

\ --- Commit Operations ---

: git-add ( path-addr path-u -- )
  str-reset
  s" git add " str+
  str+
  str$ system drop ;

: git-add-all ( -- )
  s" git add -A" system drop ;

: git-commit ( message-addr message-u -- )
  str-reset
  s" git commit -m '" str+
  str+
  s" '" str+
  str$ system drop ;

: git-commit-amend ( -- )
  s" git commit --amend --no-edit" system drop ;

: tool-git-commit ( message-addr message-u files -- json-addr json-u )
  \ Stage files and commit
  \ files would be array of paths
  drop
  git-commit
  s" {\"status\": \"success\", \"message\": \"Committed\"}" ;

\ --- Stash ---

: git-stash ( -- )
  s" git stash" system drop ;

: git-stash-pop ( -- )
  s" git stash pop" system drop ;

: git-stash-list ( -- )
  s" git stash list" system drop ;

\ --- Remote Operations ---

: git-fetch ( -- )
  s" git fetch" system drop ;

: git-pull ( -- )
  s" git pull" system drop ;

: git-push ( -- )
  s" git push" system drop ;

\ Note: Push should require explicit user confirmation in agent context

\ --- Blame ---

: git-blame ( path-addr path-u -- )
  str-reset
  s" git blame " str+
  str+
  str$ system drop ;

: git-blame-line ( path-addr path-u line -- )
  str-reset
  s" git blame -L " str+
  dup 0 <# #s #> str+
  s" ," str+
  0 <# #s #> str+
  s"  " str+
  str+
  str$ system drop ;

\ --- Tool Dispatcher ---

: tool-git-dispatch ( cmd-addr cmd-u args-addr args-u -- json-addr json-u )
  2>r  \ save args
  2dup s" status" compare 0= if 2drop 2r> 2drop tool-git-status exit then
  2dup s" diff" compare 0= if 2drop 2r> tool-git-diff exit then
  2dup s" log" compare 0= if 2drop 2r> drop c@ [char] 0 - tool-git-log exit then
  2dup s" branch" compare 0= if 2drop 2r> 2drop tool-git-branch exit then
  2dup s" commit" compare 0= if 2drop 2r> 0 tool-git-commit exit then
  2drop 2r> 2drop
  s" {\"status\": \"error\", \"error\": \"Unknown git command\"}" ;

\ --- Safety Checks ---

: git-is-clean? ( -- flag )
  \ Check if working directory is clean
  \ Would parse git status output
  false ;

: git-on-main? ( -- flag )
  \ Check if on main/master branch
  false ;

: git-safe-for-push? ( -- flag )
  \ Check if safe to push (not on main, etc.)
  git-on-main? 0= ;
