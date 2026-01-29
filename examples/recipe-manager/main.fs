\ fifth/examples/recipe-manager/main.fs
\ Recipe manager with scaling and inventory

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" recipes.db" ;

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"" str+
  s" CREATE TABLE IF NOT EXISTS recipes (id INTEGER PRIMARY KEY, name TEXT, servings INTEGER, instructions TEXT);" str+
  s" CREATE TABLE IF NOT EXISTS ingredients (id INTEGER PRIMARY KEY, recipe_id INTEGER, name TEXT, amount REAL, unit TEXT);" str+
  s" CREATE TABLE IF NOT EXISTS inventory (id INTEGER PRIMARY KEY, name TEXT, quantity REAL, unit TEXT);" str+
  s" \"" str+
  str$ system drop ;

\ --- Recipe Operations ---

: add-recipe ( name-addr name-u servings -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO recipes (name, servings) VALUES ('" str+
  2swap str+
  s" ', " str+
  0 <# #s #> str+
  s" );\"" str+
  str$ system drop
  s" Recipe added" type cr ;

: list-recipes ( -- )
  s" Recipes:" type cr
  s" --------" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT id, name, servings FROM recipes;\"" str+
  str$ system drop ;

: view-recipe ( name-addr name-u -- )
  s" Recipe: " type 2dup type cr
  s" ========" type cr
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"SELECT * FROM recipes WHERE name='" str+
  str+
  s" ';\"" str+
  str$ system drop ;

\ --- Ingredient Scaling ---

: scale-amount ( amount scale-factor -- scaled )
  * ;

: print-ingredient ( name-addr name-u amount unit-addr unit-u -- )
  s" - " type
  2swap type s"  " type
  . s" " type
  type cr ;

\ --- Shopping List ---

: shopping-styles ( -- )
  <style>
  s" body { font-family: system-ui; max-width: 600px; margin: 0 auto; padding: 2rem; }" raw nl
  s" h1 { border-bottom: 2px solid #333; padding-bottom: 0.5rem; }" raw nl
  s" .item { padding: 0.5rem 0; border-bottom: 1px solid #eee; display: flex; }" raw nl
  s" .item input { margin-right: 1rem; }" raw nl
  s" .amount { color: #666; margin-left: auto; }" raw nl
  s" @media print { .no-print { display: none; } }" raw nl
  </style> ;

: shopping-item ( name-addr name-u amount unit-addr unit-u -- )
  <div.> s" item" raw q s" >" raw
  s" <input type=" raw q s" checkbox" raw q s" >" raw
  <span> 2>r 2>r text </span>
  <span.> s" amount" raw q s" >" raw
  2r> . 2r> text
  </span>
  </div> nl ;

: generate-shopping-list ( -- )
  s" output/shopping.html" w/o create-file throw html>file

  s" Shopping List" html-head
  shopping-styles
  html-body

  <h1> s" Shopping List" text </h1>

  <div.> s" no-print" raw q s" >" raw
    s" <button onclick=" raw q s" window.print()" raw q s" >" raw
    s" Print" text
    s" </button>" raw
  </div> nl

  <div.> s" list" raw q s" >" raw nl
    \ Sample items
    s" Flour" 2 s" cups" shopping-item
    s" Sugar" 1 s" cup" shopping-item
    s" Eggs" 3 s" " shopping-item
    s" Butter" 200 s" g" shopping-item
    s" Milk" 1 s" cup" shopping-item
  </div> nl

  html-end
  html-fid @ close-file throw

  s" Shopping list: output/shopping.html" type cr ;

\ --- Inventory ---

: update-inventory ( name-addr name-u amount unit-addr unit-u -- )
  s" Updating inventory..." type cr
  \ TODO: Implement with SQL
  2drop drop 2drop ;

: check-inventory ( name-addr name-u -- amount )
  \ TODO: Query inventory
  2drop 0 ;

\ --- Main ---

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: usage ( -- )
  s" Recipe Manager" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth recipe-manager/main.fs list           - List all recipes" type cr
  s"   ./fifth recipe-manager/main.fs shop           - Generate shopping list" type cr
  s"   ./fifth recipe-manager/main.fs add <name> <servings>" type cr ;

: main ( -- )
  ensure-output
  init-db

  argc @ 2 < if
    usage exit
  then

  1 argv
  2dup s" list" compare 0= if 2drop list-recipes exit then
  2dup s" shop" compare 0= if 2drop generate-shopping-list exit then
  2dup s" add" compare 0= if
    2drop
    argc @ 4 < if
      s" Usage: add <name> <servings>" type cr exit
    then
    2 argv 3 argv drop c@ [char] 0 - add-recipe
    exit
  then
  2drop usage ;

main
bye
