\ install.fs - Fifth self-installer
\
\ Run after building the interpreter:
\   cd engine && make && cd ..
\   ./fifth install.fs
\
\ This script sets up ~/.fifth/ with libraries and packages.

\ Get FIFTH_HOME or default to ~/.fifth
: fifth-home ( -- addr u )
  s" FIFTH_HOME" getenv
  dup 0= if
    2drop s" ~/.fifth"
  then ;

\ Create directory (ignore errors if exists)
: mkdir ( addr u -- )
  str-reset
  s" mkdir -p " str+
  str+
  s"  2>/dev/null" str+
  str$ system ;

\ Copy file
: cp ( src-addr src-u dest-addr dest-u -- )
  2>r
  str-reset
  s" cp -n " str+    \ -n = don't overwrite existing
  str+
  s"  " str+
  2r> str+
  s"  2>/dev/null" str+
  str$ system ;

\ Copy directory contents
: cp-r ( src-addr src-u dest-addr dest-u -- )
  2>r
  str-reset
  s" cp -rn " str+   \ -rn = recursive, don't overwrite
  str+
  s" /* " str+
  2r> str+
  s"  2>/dev/null" str+
  str$ system ;

\ =============================================================================
\ Installation
\ =============================================================================

cr
." ███████╗██╗███████╗████████╗██╗  ██╗" cr
." ██╔════╝██║██╔════╝╚══██╔══╝██║  ██║" cr
." █████╗  ██║█████╗     ██║   ███████║" cr
." ██╔══╝  ██║██╔══╝     ██║   ██╔══██║" cr
." ██║     ██║██║        ██║   ██║  ██║" cr
." ╚═╝     ╚═╝╚═╝        ╚═╝   ╚═╝  ╚═╝" cr
."      Fifth Installer" cr
cr

." Installing to: " fifth-home type cr
cr

\ Create directories
." Creating directories..." cr
fifth-home mkdir
str-reset fifth-home str+ s" /lib" str+ str$ mkdir
str-reset fifth-home str+ s" /packages" str+ str$ mkdir

\ Copy libraries
." Copying libraries..." cr
str-reset fifth-home str+ s" /lib" str+ str$
s" lib" 2swap cp-r

\ Verify
cr
." Verifying installation..." cr
." 2 + 3 = " 2 3 + . cr

cr
." ✓ Installation complete!" cr
cr
." Try:" cr
."   ./fifth                          # Interactive REPL" cr
."   ./fifth -e \"2 3 + . cr\"          # One-liner" cr
."   ./fifth examples/agent-dashboard.fs  # Demo" cr
cr

bye
