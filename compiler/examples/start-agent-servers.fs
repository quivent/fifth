\ start_agent_servers.fs - Start Multiple Fast Forth Agent Servers
\
\ This script starts N Fast Forth verification servers on sequential ports.
\ Each server acts as an independent agent worker.
\
\ Usage:
\   fifth start_agent_servers.fs [num_agents]
\
\ Example:
\   fifth start_agent_servers.fs

variable num-agents
variable base-port

10 num-agents !
8080 base-port !

: header ( -- )
  ." Starting " num-agents @ . ." Fast Forth agent servers..." cr
  ." Base port: " base-port @ . cr cr ;

: start-servers ( -- )
  \ Start agent servers using shell
  str-reset
  s" PIDS=(); trap 'echo \"\"; echo \"Shutting down agent servers...\"; for pid in \"${PIDS[@]}\"; do kill $pid 2>/dev/null; done; echo \"All agents stopped.\"; exit 0' SIGINT SIGTERM; for i in $(seq 0 $((" str+
  num-agents @ s>d <# #s #> str+
  s"  - 1))); do PORT=$((" str+
  base-port @ s>d <# #s #> str+
  s"  + i)); fastforth-server --port $PORT & PID=$!; PIDS+=($PID); echo \"  Agent $i: http://localhost:$PORT (PID: $PID)\"; sleep 0.1; done; echo ''; echo 'All " str+
  num-agents @ s>d <# #s #> str+
  s"  agents started!'; echo 'Press Ctrl+C to stop all servers'; echo ''; wait" str+
  str$ system ;

: footer ( -- )
  cr ." Agent servers stopped." cr ;

: main ( -- )
  header
  start-servers
  footer ;

main
bye
