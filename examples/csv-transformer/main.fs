\ fifth/examples/csv-transformer/main.fs
\ CSV transformer - convert and enrich data

require ~/.fifth/lib/core.fs

\ Configuration
: delimiter ( -- c ) [char] , ;

\ --- CSV Parsing ---

: skip-to-delim ( addr u -- addr' u' )
  \ Skip to next delimiter or end
  begin
    dup 0> while
    over c@ delimiter = if exit then
    1 /string
  repeat ;

: csv-field ( addr u n -- field-addr field-u )
  \ Extract nth field (0-indexed) from CSV line
  \ Similar to sql-field but for comma-delimited
  >r
  r> 0 ?do
    skip-to-delim
    dup 0> if 1 /string then  \ skip delimiter
  loop
  2dup skip-to-delim nip - ;

: count-fields ( addr u -- n )
  \ Count number of fields in CSV line
  1 >r
  begin
    dup 0> while
    over c@ delimiter = if r> 1+ >r then
    1 /string
  repeat
  2drop r> ;

\ --- Field Transformations ---

: trim-spaces ( addr u -- addr' u' )
  \ Remove leading/trailing spaces
  \ TODO: Implement proper trimming
  ;

: to-uppercase ( addr u -- addr u )
  \ Convert string to uppercase in place
  2dup bounds ?do
    i c@ dup [char] a [char] z 1+ within if
      32 - i c!
    else drop then
  loop ;

\ --- Output Formats ---

: emit-csv-row ( addr u -- )
  \ Output row as CSV (pass-through for now)
  type cr ;

: emit-json-row ( addr u -- )
  \ Output row as JSON object
  s" {" type
  2dup count-fields 0 ?do
    i 0> if s" , " type then
    q s" field" type i . q s" : " type
    q 2dup i csv-field type q
  loop
  2drop
  s" }" type cr ;

: emit-html-row ( addr u -- )
  \ Output row as HTML table row
  s" <tr>" type
  2dup count-fields 0 ?do
    s" <td>" type
    2dup i csv-field type
    s" </td>" type
  loop
  2drop
  s" </tr>" type cr ;

\ --- Processing ---

256 constant max-line
create line-buf max-line allot
variable in-fid
variable out-fid
variable line-count

: process-csv ( in-addr in-u out-addr out-u -- )
  w/o create-file throw out-fid !
  r/o open-file throw in-fid !

  0 line-count !

  begin
    line-buf max-line in-fid @ read-line throw
  while
    1 line-count +!
    line-buf swap
    \ Apply transformations here
    emit-csv-row
  repeat
  drop

  in-fid @ close-file throw
  out-fid @ close-file throw

  s" Processed " type line-count @ . s"  rows" type cr ;

\ --- Main ---

: usage ( -- )
  s" Usage: ./fifth csv-transformer/main.fs <input.csv> <output.csv>" type cr
  s" " type cr
  s" Options:" type cr
  s"   --json    Output as JSON" type cr
  s"   --html    Output as HTML table" type cr ;

: main ( -- )
  s" CSV Transformer" type cr
  s" ---------------" type cr
  \ TODO: Parse command line arguments
  \ For now, just show usage
  usage ;

main
bye
