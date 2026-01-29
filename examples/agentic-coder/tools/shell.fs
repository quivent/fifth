\ fifth/examples/agentic-coder/tools/shell.fs
\ Shell command execution for agentic coder

\ --- Configuration ---

variable shell-timeout
30000 shell-timeout !  \ 30 second default timeout

\ Output buffer
4096 constant shell-buf-size
create shell-output-buf shell-buf-size allot
variable shell-output-len

\ --- Command Execution ---

: shell-exec ( cmd-addr cmd-u -- exit-code )
  \ Execute command and return exit code
  system ;

: shell-exec-capture ( cmd-addr cmd-u -- output-addr output-u exit-code )
  \ Execute and capture output
  \ Fifth's system doesn't capture output directly,
  \ so we redirect to a temp file
  str-reset
  str+
  s"  > /tmp/agent-shell-output.txt 2>&1" str+
  str$ system >r

  \ Read output file
  s" /tmp/agent-shell-output.txt" r/o open-file if
    drop s" " 0 r> exit
  then
  >r
  shell-output-buf shell-buf-size r@ read-file if
    r> close-file drop
    s" " 0 r> exit
  then
  shell-output-len !
  r> close-file drop

  shell-output-buf shell-output-len @ r> ;

: tool-shell-exec ( cmd-addr cmd-u -- json-addr json-u )
  \ Execute command and return JSON result
  shell-exec-capture >r

  str-reset
  s" {\"status\": " str+
  r@ 0= if s" \"success\"" else s" \"error\"" then str+
  s" , \"exit_code\": " str+
  r> 0 <# #s #> str+
  s" , \"output\": \"" str+
  \ TODO: JSON escape output
  str+
  s" \"}" str+
  str$ ;

\ --- Safe Commands (allowlist) ---

create safe-commands 1024 allot
variable safe-commands-len

: init-safe-commands ( -- )
  \ Initialize list of safe commands
  s" ls cat head tail wc grep find file stat pwd which" safe-commands swap move
  safe-commands-len ! ;

: is-safe-command? ( cmd-addr cmd-u -- flag )
  \ Check if command starts with a safe prefix
  \ Extract first word and check against allowlist
  \ TODO: Implement proper parsing
  2drop true ;  \ DANGER: Always returns true for demo

\ --- Sandboxed Execution ---

: sandbox-check ( cmd-addr cmd-u -- safe? )
  \ Check command for dangerous patterns
  2dup s" rm " -1 search nip nip if 2drop false exit then
  2dup s" sudo" -1 search nip nip if 2drop false exit then
  2dup s" chmod" -1 search nip nip if 2drop false exit then
  2dup s" chown" -1 search nip nip if 2drop false exit then
  2dup s" dd " -1 search nip nip if 2drop false exit then
  2dup s" mkfs" -1 search nip nip if 2drop false exit then
  2dup s" >" -1 search nip nip if 2drop false exit then  \ redirect
  2drop true ;

: tool-shell-safe ( cmd-addr cmd-u -- json-addr json-u )
  \ Execute only if command passes safety checks
  2dup sandbox-check if
    tool-shell-exec
  else
    2drop
    s" {\"status\": \"error\", \"error\": \"Command blocked by safety check\"}"
  then ;

\ --- Background Execution ---

: shell-exec-bg ( cmd-addr cmd-u -- pid )
  \ Execute command in background
  str-reset
  str+
  s"  &" str+
  s"  echo $!" str+
  str$ system drop
  0 ;  \ TODO: Capture actual PID

: shell-kill ( pid -- )
  str-reset
  s" kill " str+
  0 <# #s #> str+
  str$ system drop ;

\ --- Working Directory ---

: shell-pwd ( -- )
  s" pwd" system drop ;

: shell-cd ( path-addr path-u -- )
  \ Note: This won't persist in Forth's system calls
  \ Each system call runs in a new shell
  s" [cd doesn't persist between commands]" type cr
  2drop ;

\ --- Environment ---

: shell-env ( var-addr var-u -- value-addr value-u )
  \ Get environment variable
  str-reset
  s" echo $" str+
  str+
  str$ shell-exec-capture drop ;

: shell-set-env ( var-addr var-u value-addr value-u -- )
  \ Set environment variable (only for current command)
  s" [Environment variables don't persist]" type cr
  2drop 2drop ;

\ --- Tool Interface ---

: tool-shell-dispatch ( mode-addr mode-u cmd-addr cmd-u -- json-addr json-u )
  2>r  \ save command
  2dup s" exec" compare 0= if 2drop 2r> tool-shell-exec exit then
  2dup s" safe" compare 0= if 2drop 2r> tool-shell-safe exit then
  2dup s" capture" compare 0= if 2drop 2r> tool-shell-exec exit then
  2drop 2r> 2drop
  s" {\"status\": \"error\", \"error\": \"Unknown shell mode\"}" ;
