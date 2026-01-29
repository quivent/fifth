\ fifth/examples/quiz-system/main.fs
\ Quiz system - generate and score assessments

require ~/.fifth/lib/core.fs

\ Configuration
: output-file ( -- addr u ) s" output/quiz.html" ;
: db-file     ( -- addr u ) s" results.db" ;

\ --- Question Components ---

: question-header ( num text-addr text-u -- )
  <div.> s" question" raw q s" >" raw nl
  <h3>
    s" Question " text
    rot .
    s" : " text
    text
  </h3> nl ;

: question-end ( -- )
  </div> nl ;

: option-radio ( name-addr name-u value text-addr text-u -- )
  <label.> s" option" raw q s" >" raw nl
  s"   <input type=" raw q s" radio" raw q
  s"  name=" raw q 2>r 2r@ raw q
  s"  value=" raw q 2swap . q
  s" >" raw
  text
  2r> 2drop
  </label> nl ;

: option-checkbox ( name-addr name-u value text-addr text-u -- )
  <label.> s" option" raw q s" >" raw nl
  s"   <input type=" raw q s" checkbox" raw q
  s"  name=" raw q 2>r 2r@ raw q
  s"  value=" raw q 2swap . q
  s" >" raw
  text
  2r> 2drop
  </label> nl ;

: true-false-question ( num text-addr text-u -- )
  question-header
  s" q" 1 s" True" option-radio
  s" q" 0 s" False" option-radio
  question-end ;

: multiple-choice ( num text-addr text-u -- )
  question-header
  \ Options would come from data
  s" q" 0 s" Option A" option-radio
  s" q" 1 s" Option B" option-radio
  s" q" 2 s" Option C" option-radio
  s" q" 3 s" Option D" option-radio
  question-end ;

\ --- Quiz Generation ---

: quiz-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 700px; margin: 0 auto; padding: 2rem; }" raw nl
  s" .question { margin-bottom: 2rem; padding: 1rem; background: #f9f9f9; border-radius: 8px; }" raw nl
  s" .option { display: block; padding: 0.5rem; margin: 0.25rem 0; cursor: pointer; }" raw nl
  s" .option:hover { background: #e3f2fd; }" raw nl
  s" input[type=radio], input[type=checkbox] { margin-right: 0.5rem; }" raw nl
  s" .submit-btn { background: #1976d2; color: white; padding: 1rem 2rem; border: none; border-radius: 4px; cursor: pointer; font-size: 1rem; }" raw nl
  s" .submit-btn:hover { background: #1565c0; }" raw nl
  </style> ;

: generate-quiz ( title-addr title-u -- )
  s" output/quiz.html" w/o create-file throw html>file

  2dup html-head
  quiz-styles
  html-body

  <h1> text </h1>

  s" <form action=" raw q s" submit.html" raw q s"  method=" raw q s" POST" raw q s" >" raw nl

  \ Sample questions
  1 s" The Earth is flat." true-false-question
  2 s" What is the capital of France?" multiple-choice
  3 s" Water freezes at 0 degrees Celsius." true-false-question

  <div>
    s" <button type=" raw q s" submit" raw q s"  class=" raw q s" submit-btn" raw q s" >" raw
    s" Submit Quiz" text
    s" </button>" raw
  </div> nl

  s" </form>" raw nl

  html-end
  html-fid @ close-file throw ;

\ --- Scoring ---

variable correct-count
variable total-count

: init-scoring ( -- )
  0 correct-count !
  0 total-count ! ;

: check-answer ( submitted correct -- )
  1 total-count +!
  = if 1 correct-count +! then ;

: score-result ( -- percent )
  correct-count @ 100 * total-count @ / ;

: display-score ( -- )
  s" Score: " type
  correct-count @ . s" / " type total-count @ .
  s"  (" type score-result . s" %)" type cr ;

\ --- Results Storage ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"CREATE TABLE IF NOT EXISTS results (id INTEGER PRIMARY KEY, quiz TEXT, score INTEGER, date TEXT);\"" str+
  str$ system drop ;

: save-result ( quiz-addr quiz-u score -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO results (quiz, score, date) VALUES ('" str+
  2swap str+
  s" ', " str+
  0 <# #s #> str+
  s" , datetime('now'));\"" str+
  str$ system drop ;

\ --- Reports ---

: generate-report ( -- )
  s" Quiz Results Report" type cr
  s" ===================" type cr
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"SELECT quiz, score, date FROM results ORDER BY date DESC LIMIT 10;\"" str+
  str$ system drop ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: usage ( -- )
  s" Quiz System" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth quiz-system/main.fs generate        - Generate quiz HTML" type cr
  s"   ./fifth quiz-system/main.fs report          - View results" type cr ;

: main ( -- )
  ensure-output
  init-db

  argc @ 2 < if
    s" Sample Quiz" generate-quiz
    s" Generated: output/quiz.html" type cr
    exit
  then

  1 argv
  2dup s" generate" compare 0= if
    2drop s" Sample Quiz" generate-quiz
    s" Generated: output/quiz.html" type cr
    exit
  then
  2dup s" report" compare 0= if
    2drop generate-report
    exit
  then
  2drop usage ;

main
bye
