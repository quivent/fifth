\ fifth/lib/html.fs - HTML Generation Library
\ Proper escaping, semantic tags, file output

require ~/fifth/lib/str.fs

\ ============================================================
\ Output Target
\ ============================================================

variable html-fid  \ File descriptor for HTML output

: html>file ( fid -- ) html-fid ! ;
: html>stdout ( -- ) stdout html-fid ! ;

: h>> ( addr u -- )
  \ Write to HTML output
  html-fid @ write-file throw ;

: h>>nl ( -- )
  \ Write newline
  10 html-fid @ emit-file throw ;

: h>>line ( addr u -- )
  \ Write line with newline
  h>> h>>nl ;

\ ============================================================
\ HTML Escaping
\ ============================================================

: html-escape ( addr u -- addr' u' )
  \ Escape HTML special characters: < > & ' "
  str2-reset
  0 ?do
    dup i + c@
    case
      [char] < of s" &lt;"   str2+ endof
      [char] > of s" &gt;"   str2+ endof
      [char] & of s" &amp;"  str2+ endof
      [char] ' of s" &#39;"  str2+ endof
      [char] " of s" &quot;" str2+ endof
      dup str2-char
    endcase
  loop
  drop str2$ ;

\ ============================================================
\ Core Output Words
\ ============================================================

: raw ( addr u -- ) h>> ;           \ Output raw HTML
: text ( addr u -- ) html-escape h>> ;  \ Output escaped text
: nl ( -- ) h>>nl ;                 \ Newline
: rawln ( addr u -- ) h>>line ;     \ Raw with newline

\ ============================================================
\ Tag Building
\ ============================================================

: </ ( -- ) s" </" h>> ;
: /> ( -- ) s" />" h>> ;
: >t ( -- ) s" >" h>> ;
: >tnl ( -- ) >t nl ;

\ Self-closing tag
: tag/ ( name$ -- ) s" <" h>> h>> /> ;

\ Open/close tags
: <tag> ( name$ -- ) s" <" h>> h>> >t ;
: <tag>nl ( name$ -- ) <tag> nl ;
: </tag> ( name$ -- ) </ h>> >t ;
: </tag>nl ( name$ -- ) </tag> nl ;

\ Tag with class attribute
: <tag.> ( class$ name$ -- )
  s" <" h>> h>>
  s"  class='" h>> h>> s" '" h>> >t ;
: <tag.>nl ( class$ name$ -- ) <tag.> nl ;

\ Tag with id attribute
: <tag#> ( id$ name$ -- )
  s" <" h>> h>>
  s"  id='" h>> h>> s" '" h>> >t ;

\ Tag with both class and id
: <tag#.> ( id$ class$ name$ -- )
  s" <" h>> h>>
  s"  id='" h>> 2swap h>> s" '" h>>
  s"  class='" h>> h>> s" '" h>> >t ;

\ ============================================================
\ Common HTML Elements
\ ============================================================

\ Document structure
: <!doctype> ( -- ) s" <!DOCTYPE html>" rawln ;
: <html> s" html" <tag>nl ;
: </html> s" html" </tag>nl ;
: <head> s" head" <tag>nl ;
: </head> s" head" </tag>nl ;
: <body> s" body" <tag>nl ;
: </body> s" body" </tag>nl ;
: <title> s" title" <tag> ;
: </title> s" title" </tag>nl ;
: <meta ( -- ) s" <meta " h>> ;
: meta> ( -- ) s" >" h>> ;
: <link ( -- ) s" <link " h>> ;

\ Headings
: <h1> s" h1" <tag> ;
: </h1> s" h1" </tag>nl ;
: <h2> s" h2" <tag> ;
: </h2> s" h2" </tag>nl ;
: <h3> s" h3" <tag> ;
: </h3> s" h3" </tag>nl ;
: <h4> s" h4" <tag> ;
: </h4> s" h4" </tag>nl ;

\ Containers
: <div> s" div" <tag> ;
: <div.> ( class$ -- ) s" div" <tag.> ;
: <div.>nl ( class$ -- ) s" div" <tag.>nl ;
: <div#> ( id$ -- ) s" div" <tag#> ;
: <div#.> ( id$ class$ -- ) s" div" <tag#.> ;
: </div> s" div" </tag> ;
: </div>nl s" div" </tag>nl ;

: <span> s" span" <tag> ;
: <span.> ( class$ -- ) s" span" <tag.> ;
: </span> s" span" </tag> ;

: <section> s" section" <tag>nl ;
: <section.> ( class$ -- ) s" section" <tag.>nl ;
: </section> s" section" </tag>nl ;

: <article> s" article" <tag>nl ;
: </article> s" article" </tag>nl ;

: <header> s" header" <tag>nl ;
: <header.> ( class$ -- ) s" header" <tag.>nl ;
: </header> s" header" </tag>nl ;

: <footer> s" footer" <tag>nl ;
: </footer> s" footer" </tag>nl ;

: <nav> s" nav" <tag>nl ;
: </nav> s" nav" </tag>nl ;

: <main> s" main" <tag>nl ;
: <main.> ( class$ -- ) s" main" <tag.>nl ;
: </main> s" main" </tag>nl ;

: <aside> s" aside" <tag>nl ;
: <aside.> ( class$ -- ) s" aside" <tag.>nl ;
: </aside> s" aside" </tag>nl ;

\ Text elements
: <p> s" p" <tag> ;
: <p.> ( class$ -- ) s" p" <tag.> ;
: </p> s" p" </tag> ;
: </p>nl s" p" </tag>nl ;

: <strong> s" strong" <tag> ;
: </strong> s" strong" </tag> ;

: <em> s" em" <tag> ;
: </em> s" em" </tag> ;

: <code> s" code" <tag> ;
: </code> s" code" </tag> ;

: <pre> s" pre" <tag> ;
: </pre> s" pre" </tag> ;

: <blockquote> s" blockquote" <tag>nl ;
: </blockquote> s" blockquote" </tag>nl ;

: <br/> s" br" tag/ ;

\ Lists
: <ul> s" ul" <tag>nl ;
: <ul.> ( class$ -- ) s" ul" <tag.>nl ;
: </ul> s" ul" </tag>nl ;

: <ol> s" ol" <tag>nl ;
: </ol> s" ol" </tag>nl ;

: <li> s" li" <tag> ;
: <li.> ( class$ -- ) s" li" <tag.> ;
: </li> s" li" </tag> ;
: </li>nl s" li" </tag>nl ;

\ Tables
: <table> s" table" <tag>nl ;
: <table.> ( class$ -- ) s" table" <tag.>nl ;
: </table> s" table" </tag>nl ;

: <thead> s" thead" <tag>nl ;
: </thead> s" thead" </tag>nl ;

: <tbody> s" tbody" <tag>nl ;
: </tbody> s" tbody" </tag>nl ;

: <tr> s" tr" <tag> ;
: <tr.> ( class$ -- ) s" tr" <tag.> ;
: </tr> s" tr" </tag>nl ;

: <th> s" th" <tag> ;
: </th> s" th" </tag> ;

: <td> s" td" <tag> ;
: <td.> ( class$ -- ) s" td" <tag.> ;
: </td> s" td" </tag> ;

\ Forms
: <form> s" form" <tag>nl ;
: </form> s" form" </tag>nl ;

: <input ( -- ) s" <input " h>> ;
: input> ( -- ) s" >" h>> ;

: <button ( -- ) s" <button" h>> ;  \ Open for attributes
: <button> s" button" <tag> ;
: <button.> ( class$ -- ) s" button" <tag.> ;
: </button> s" button" </tag> ;

: <label> s" label" <tag> ;
: </label> s" label" </tag> ;

: <textarea> s" textarea" <tag> ;
: </textarea> s" textarea" </tag> ;

: <select> s" select" <tag>nl ;
: </select> s" select" </tag>nl ;

: <option> s" option" <tag> ;
: </option> s" option" </tag>nl ;

\ Links and media
: <a ( -- ) s" <a " h>> ;
: a> ( -- ) s" >" h>> ;
: </a> s" a" </tag> ;

: <img ( -- ) s" <img " h>> ;
: img> ( -- ) s" >" h>> ;

\ Style and script
: <style> s" style" <tag>nl ;
: </style> s" style" </tag>nl ;

: <script> s" script" <tag> ;
: </script> s" script" </tag>nl ;

\ ============================================================
\ Attribute Helpers
\ ============================================================

: attr= ( name$ value$ -- )
  \ Output: name='value'
  2swap h>> s" ='" h>> h>> s" '" h>> ;

: attr-text= ( name$ value$ -- )
  \ Output: name='escaped-value'
  2swap h>> s" ='" h>> html-escape h>> s" '" h>> ;

: href= ( url$ -- ) s" href" 2swap attr= ;
: src= ( url$ -- ) s" src" 2swap attr= ;
: type= ( type$ -- ) s" type" 2swap attr= ;
: name= ( name$ -- ) s" name" 2swap attr= ;
: value= ( value$ -- ) s" value" 2swap attr-text= ;
: placeholder= ( text$ -- ) s" placeholder" 2swap attr-text= ;

\ ============================================================
\ Convenience Words
\ ============================================================

\ Quick elements with content
: h1. ( text$ -- ) <h1> text </h1> ;
: h2. ( text$ -- ) <h2> text </h2> ;
: h3. ( text$ -- ) <h3> text </h3> ;
: h4. ( text$ -- ) <h4> text </h4> ;
: p. ( text$ -- ) <p> text </p>nl ;
: li. ( text$ -- ) <li> text </li>nl ;

\ Table cells
: th. ( text$ -- ) <th> text </th> ;
: td. ( text$ -- ) <td> text </td> ;
: td-code. ( text$ -- ) <td> <code> text </code> </td> ;
: td-raw. ( html$ -- ) <td> raw </td> ;

\ Link
: a. ( text$ url$ -- )
  <a href= a> text </a> ;

\ Image
: img. ( alt$ src$ -- )
  <img src= s"  alt='" h>> html-escape h>> s" '" h>> img> ;

\ ============================================================
\ Common Patterns
\ ============================================================

: html-head ( title$ -- )
  \ Start HTML document with title, leave head open for styles
  <!doctype>
  <html>
  <head>
  <meta s" charset='UTF-8'" raw meta> nl
  <meta s" name='viewport' content='width=device-width,initial-scale=1'" raw meta> nl
  <title> text </title> ;

: html-body ( -- )
  \ Close head, open body
  </head>
  <body> ;

: html-begin ( title$ -- )
  \ Start HTML document (head closed, body open)
  html-head html-body ;

: html-end ( -- )
  \ End HTML document
  </body>
  </html> ;

\ ============================================================
\ CSS Helper
\ ============================================================

: css ( css-string$ -- )
  \ Output CSS inside style tags
  <style> raw nl </style> ;

: css-rule ( selector$ properties$ -- )
  \ Output a CSS rule: selector{properties}
  2swap h>> s" {" h>> h>> s" }" h>> nl ;
