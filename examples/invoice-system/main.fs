\ fifth/examples/invoice-system/main.fs
\ Invoice generator - orders to PDF

require ~/.fifth/lib/core.fs

\ Configuration
: db-path     ( -- addr u ) s" orders.db" ;
: output-dir  ( -- addr u ) s" output/" ;
: company     ( -- addr u ) s" ACME Corp" ;

\ --- Invoice Styles (print-optimized) ---

: invoice-styles ( -- )
  <style>
  s" @page { size: letter; margin: 1in; }" raw nl
  s" body { font-family: 'Helvetica Neue', sans-serif; font-size: 12pt; }" raw nl
  s" .invoice-header { display: flex; justify-content: space-between; margin-bottom: 2rem; }" raw nl
  s" .company-name { font-size: 24pt; font-weight: bold; }" raw nl
  s" .invoice-number { color: #666; }" raw nl
  s" table { width: 100%; border-collapse: collapse; margin: 2rem 0; }" raw nl
  s" th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #ddd; }" raw nl
  s" th { background: #f5f5f5; }" raw nl
  s" .total-row { font-weight: bold; font-size: 14pt; }" raw nl
  s" .footer { margin-top: 3rem; color: #666; font-size: 10pt; }" raw nl
  </style> ;

\ --- Invoice Components ---

: invoice-header ( invoice-num-addr invoice-num-u date-addr date-u -- )
  <div.> s" invoice-header" raw q s" >" raw nl
    <div>
      <div.> s" company-name" raw q s" >" raw company text </div> nl
      s" <div>123 Business St</div>" raw nl
      s" <div>City, State 12345</div>" raw nl
    </div>
    <div>
      <div.> s" invoice-number" raw q s" >" raw
        s" Invoice #" text 2swap text </div> nl
      <div> s" Date: " text text </div> nl
    </div>
  </div> nl ;

: customer-info ( name-addr name-u addr-addr addr-u -- )
  <div.> s" customer" raw q s" >" raw nl
    <h3> s" Bill To:" text </h3>
    <div> 2swap text </div> nl
    <div> text </div> nl
  </div> nl ;

: line-item ( desc-addr desc-u qty price -- )
  \ Render single line item row
  <tr>
    <td> 2swap text </td>
    <td> . </td>
    <td> s" $" text . </td>
    \ TODO: Calculate line total
    <td> s" $0.00" text </td>
  </tr> nl ;

: items-table-start ( -- )
  <table>
    <thead> <tr>
      <th> s" Description" text </th>
      <th> s" Qty" text </th>
      <th> s" Unit Price" text </th>
      <th> s" Total" text </th>
    </tr> </thead> nl
    <tbody> ;

: items-table-end ( total -- )
  </tbody>
  <tfoot> <tr.> s" total-row" raw q s" >" raw
    <td> </td> <td> </td>
    <td> s" Total:" text </td>
    <td> s" $" text . </td>
  </tr> </tfoot>
  </table> nl ;

: invoice-footer ( -- )
  <div.> s" footer" raw q s" >" raw nl
    s" <p>Payment due within 30 days.</p>" raw nl
    s" <p>Thank you for your business!</p>" raw nl
  </div> nl ;

\ --- PDF Generation ---

: html>pdf ( html-path-addr html-path-u pdf-path-addr pdf-path-u -- )
  \ Convert HTML to PDF using wkhtmltopdf
  str-reset
  s" wkhtmltopdf -q " str+
  2swap str+
  s"  " str+
  str+
  str$ system drop ;

\ --- Main ---

: sample-invoice ( -- )
  \ Generate a sample invoice
  s" output/invoice-001.html" w/o create-file throw html>file

  s" Invoice" html-head
  invoice-styles
  html-body

  s" 001" s" 2024-01-15" invoice-header
  s" John Customer" s" 456 Client Ave, Town 67890" customer-info

  items-table-start
    s" Widget Pro" 2 50 line-item
    s" Service Fee" 1 100 line-item
  200 items-table-end

  invoice-footer
  html-end

  html-fid @ close-file throw
  s" Generated: output/invoice-001.html" type cr

  \ Convert to PDF
  \ s" output/invoice-001.html" s" output/invoice-001.pdf" html>pdf
  \ s" Converted to PDF" type cr
  ;

: ensure-output ( -- )
  s" mkdir -p output" system drop ;

: main ( -- )
  ensure-output
  sample-invoice ;

main
bye
