\ fifth/examples/agentic-coder/planner.fs
\ Task planning and execution for agentic coder

\ --- Task Structure ---

\ Tasks stored in SQLite with:
\ - id, description, status, parent_id, result, priority, created_at

: tasks-db ( -- addr u ) s" memory.db" ;

\ Task status constants
0 constant TASK_PENDING
1 constant TASK_RUNNING
2 constant TASK_DONE
3 constant TASK_FAILED
4 constant TASK_BLOCKED

\ --- Task Creation ---

variable current-task-id

: create-task ( desc-addr desc-u parent-id priority -- task-id )
  str-reset
  s" sqlite3 " str+
  tasks-db str+
  s"  \"INSERT INTO tasks (description, status, parent_id, priority) VALUES ('" str+
  2>r 2>r
  str+  \ description
  s" ', 'pending', " str+
  2r> 0 <# #s #> str+  \ parent_id
  s" , " str+
  2r> 0 <# #s #> str+  \ priority
  s" ); SELECT last_insert_rowid();\"" str+
  str$ system drop
  current-task-id @ dup 1+ current-task-id ! ;

: create-root-task ( desc-addr desc-u -- task-id )
  0 1 create-task ;

: create-subtask ( desc-addr desc-u parent-id -- task-id )
  1 create-task ;

\ --- Task Updates ---

: set-task-status ( task-id status-addr status-u -- )
  str-reset
  s" sqlite3 " str+
  tasks-db str+
  s"  \"UPDATE tasks SET status='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

: start-task ( task-id -- )
  s" running" set-task-status ;

: complete-task ( task-id result-addr result-u -- )
  str-reset
  s" sqlite3 " str+
  tasks-db str+
  s"  \"UPDATE tasks SET status='done', result='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

: fail-task ( task-id error-addr error-u -- )
  str-reset
  s" sqlite3 " str+
  tasks-db str+
  s"  \"UPDATE tasks SET status='failed', result='" str+
  str+
  s" ' WHERE id=" str+
  swap 0 <# #s #> str+
  s" ;\"" str+
  str$ system drop ;

\ --- Task Queries ---

: get-pending-tasks ( -- )
  s" Pending tasks:" type cr
  str-reset
  s" sqlite3 -column " str+
  tasks-db str+
  s"  \"SELECT id, description FROM tasks WHERE status='pending' ORDER BY priority DESC, id;\"" str+
  str$ system drop ;

: get-task-tree ( root-id -- )
  s" Task tree:" type cr
  str-reset
  s" sqlite3 -column " str+
  tasks-db str+
  s"  \"WITH RECURSIVE tree AS (" str+
  s"    SELECT id, description, status, 0 as depth FROM tasks WHERE id=" str+
  swap 0 <# #s #> str+
  s"    UNION ALL" str+
  s"    SELECT t.id, t.description, t.status, tree.depth+1 FROM tasks t JOIN tree ON t.parent_id=tree.id" str+
  s"  ) SELECT printf('%*s', depth*2, '') || description, status FROM tree;\"" str+
  str$ system drop ;

: get-next-task ( -- task-id )
  \ Get highest priority pending task
  \ TODO: Query and parse
  0 ;

\ --- Task Decomposition ---

: decompose-prompt ( task-addr task-u -- prompt-addr prompt-u )
  str-reset
  s" Break down this task into smaller subtasks (max 5):\n\n" str+
  str+
  s" \n\nRespond with a JSON array of subtask descriptions:\n" str+
  s" [{\"task\": \"...\", \"priority\": 1-5}, ...]" str+
  str$ ;

: parse-subtasks ( response-addr response-u parent-id -- )
  \ Parse LLM response and create subtasks
  \ TODO: Parse JSON array with jq
  drop 2drop ;

: decompose-task ( task-id -- )
  \ Use LLM to break task into subtasks
  s" [Decomposing task " type dup . s" ...]" type cr

  \ Get task description
  \ Call LLM with decompose prompt
  \ Parse response and create subtasks

  drop ;

\ --- Execution Planning ---

: estimate-complexity ( task-addr task-u -- complexity )
  \ Estimate task complexity (1-5)
  \ TODO: Use LLM or heuristics
  2drop 3 ;

: should-decompose? ( task-id -- flag )
  \ Check if task should be broken down further
  \ TODO: Check complexity and depth
  drop false ;

: plan-execution ( task-addr task-u -- )
  s" Planning execution for: " type 2dup type cr

  \ Create root task
  2dup create-root-task >r

  \ Check if decomposition needed
  estimate-complexity 3 > if
    r@ decompose-task
  then

  r> drop ;

\ --- Execution Loop ---

variable max-iterations
10 max-iterations !

: execute-single-task ( task-id -- success? )
  \ Execute one task
  dup start-task

  \ TODO: Get task description
  \ TODO: Determine action (tool call, LLM query, etc.)
  \ TODO: Execute and capture result

  s" [Task completed]" complete-task
  true ;

: execute-plan ( root-task-id -- )
  \ Execute all tasks in plan
  s" Executing plan..." type cr

  max-iterations @ 0 ?do
    get-next-task dup 0= if
      drop s" All tasks complete!" type cr
      leave
    then

    dup execute-single-task if
      drop
    else
      s" Task failed, stopping" type cr
      fail-task
      leave
    then
  loop ;

\ --- Plan Visualization ---

: show-plan ( task-id -- )
  s" ┌─────────────────────────────────────────┐" type cr
  s" │              EXECUTION PLAN             │" type cr
  s" ├─────────────────────────────────────────┤" type cr
  get-task-tree
  s" └─────────────────────────────────────────┘" type cr ;

\ --- Interactive Planning ---

: interactive-plan ( task-addr task-u -- )
  s" I'll help you with: " type 2dup type cr
  cr

  plan-execution

  s" Here's my plan:" type cr
  0 show-plan  \ TODO: Use actual root task ID

  s" Proceed with execution? (yes/no) " type
  \ TODO: Read user confirmation
  ;
