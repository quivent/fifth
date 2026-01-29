\ fifth/examples/financial-calculator/main.fs
\ Financial calculator - RPN style

require ~/.fifth/lib/core.fs

\ Note: All monetary values in cents to avoid floating point
\ 100 = $1.00

\ --- Utility ---

: cents>dollars ( cents -- )
  \ Print cents as dollars
  dup abs 100 /mod
  swap rot 0< if [char] - emit then
  [char] $ emit
  . [char] . emit
  dup 10 < if [char] 0 emit then . ;

: percent>decimal ( percent*100 -- factor*10000 )
  \ Convert 650 (6.50%) to factor for multiplication
  100 + ;

\ --- Simple Interest ---

: simple-interest ( principal rate-percent months -- interest )
  \ I = P * r * t / 12
  >r >r                 \ save months, rate
  r> 100 */ r>          \ principal * rate / 100, get months
  12 */ ;               \ * months / 12

\ --- Compound Interest ---

: compound-monthly ( principal rate-percent months -- final )
  \ A = P(1 + r/1200)^n
  \ Simplified approximation using integer math
  >r >r dup             \ P P | months rate
  r> 1200 */            \ P P*r/1200 | months
  r> 0 ?do
    2dup + -rot drop    \ accumulate
  loop
  drop ;

\ --- Loan Amortization ---

: monthly-rate ( annual-rate-percent -- monthly-rate*10000 )
  \ Convert 6.5% annual to monthly rate factor
  100 * 1200 / ;

: monthly-payment ( principal term-months rate-percent -- payment )
  \ Calculate fixed monthly payment
  \ Simplified: P * r / (1 - (1+r)^-n)
  \ Using approximation for integer math
  monthly-rate >r       \ principal months | rate
  over r> * 10000 /     \ P months P*r/10000
  -rot                  \ P*r/10000 P months
  * 10000 / + ;         \ Simplified approximation

: amortization-table ( principal term rate -- )
  \ Generate amortization schedule
  s" Amortization Schedule" type cr
  s" =====================" type cr
  cr
  s" Principal: " type -rot dup cents>dollars cr
  s" Term: " type over . s"  months" type cr
  s" Rate: " type dup . s" %" type cr
  cr

  monthly-payment
  s" Monthly Payment: " type dup cents>dollars cr
  cr

  s" Month | Payment | Principal | Interest | Balance" type cr
  s" ------|---------|-----------|----------|--------" type cr

  \ TODO: Generate full table
  ;

\ --- Investment Growth ---

: future-value ( monthly-contrib rate-percent months -- total )
  \ FV of regular contributions
  \ Simplified accumulation
  0 swap 0 ?do          \ total contrib | rate
    over +              \ add contribution
    dup 2 pick 1200 */ + \ add interest
  loop
  nip nip ;

\ --- Report Generation ---

: report-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 800px; margin: 0 auto; padding: 2rem; }" raw nl
  s" table { width: 100%; border-collapse: collapse; }" raw nl
  s" th, td { padding: 0.5rem; text-align: right; border-bottom: 1px solid #ddd; }" raw nl
  s" th { background: #f5f5f5; }" raw nl
  s" .summary { background: #e3f2fd; padding: 1rem; margin-bottom: 2rem; border-radius: 4px; }" raw nl
  </style> ;

: generate-report ( principal term rate -- )
  s" /tmp/financial-report.html" w/o create-file throw html>file

  s" Financial Report" html-head
  report-styles
  html-body

  <h1> s" Loan Amortization Report" text </h1>

  <div.> s" summary" raw q s" >" raw nl
    <p> s" Principal: $" text -rot dup . </p> nl
    <p> s" Term: " text over . s"  months" text </p> nl
    <p> s" Annual Rate: " text dup . s" %" text </p> nl
  </div> nl

  monthly-payment
  <p> s" Monthly Payment: $" text . </p> nl

  html-end
  html-fid @ close-file throw

  s" Report generated: /tmp/financial-report.html" type cr ;

\ --- Interactive Mode ---

: help ( -- )
  s" Financial Calculator Commands:" type cr
  s"   <principal> <months> <rate> amortize  - Show amortization" type cr
  s"   <monthly> <rate> <months> invest      - Investment growth" type cr
  s"   <principal> <rate> <months> compound  - Compound interest" type cr
  s"   help                                  - Show this help" type cr ;

\ --- Main ---

: usage ( -- )
  s" Financial Calculator" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth financial-calculator/main.fs [command] [args]" type cr
  s" " type cr
  s" Commands:" type cr
  s"   amortize <principal> <term> <rate>  - Loan amortization" type cr
  s"   invest <monthly> <rate> <months>    - Investment growth" type cr
  s"   help                                - Show help" type cr
  s" " type cr
  s" Example:" type cr
  s"   ./fifth financial-calculator/main.fs amortize 25000000 360 650" type cr
  s"   (Principal $250,000, 30 years, 6.5%)" type cr ;

: main ( -- )
  argc @ 2 < if
    usage exit
  then
  1 argv
  2dup s" help" compare 0= if 2drop help exit then
  2dup s" amortize" compare 0= if
    2drop
    \ Parse args: principal term rate
    s" Amortization mode (args parsing TODO)" type cr
    25000000 360 650 amortization-table
    exit
  then
  2drop usage ;

main
bye
