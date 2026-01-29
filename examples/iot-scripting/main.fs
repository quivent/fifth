\ fifth/examples/iot-scripting/main.fs
\ IoT scripting for resource-constrained devices

require ~/.fifth/lib/core.fs

\ Configuration
: db-file ( -- addr u ) s" data.db" ;
: log-interval ( -- ms ) 60000 ;  \ 1 minute

\ Sensor state
variable current-temp
variable current-humidity
variable fan-state
variable alert-state

\ --- Database Setup ---

: init-db ( -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"CREATE TABLE IF NOT EXISTS readings (id INTEGER PRIMARY KEY, sensor TEXT, value REAL, timestamp TEXT DEFAULT CURRENT_TIMESTAMP);\"" str+
  str$ system drop ;

: log-reading ( sensor-addr sensor-u value -- )
  str-reset
  s" sqlite3 " str+
  db-file str+
  s"  \"INSERT INTO readings (sensor, value) VALUES ('" str+
  2swap str+
  s" ', " str+
  0 <# #s #> str+
  s" );\"" str+
  str$ system drop ;

\ --- Sensor Reading ---

: read-temp ( -- temp )
  \ Read from thermal zone (Linux)
  \ cat /sys/class/thermal/thermal_zone0/temp returns millidegrees
  \ For demo, simulate
  25 random 10 mod + ;  \ 25-35 degrees

: read-humidity ( -- humidity )
  \ Read from humidity sensor
  \ For demo, simulate
  40 random 30 mod + ;  \ 40-70%

: read-gpio ( pin -- state )
  \ Read GPIO pin state
  str-reset
  s" cat /sys/class/gpio/gpio" str+
  0 <# #s #> str+
  s" /value 2>/dev/null || echo 0" str+
  str$ system drop
  0 ;  \ placeholder

: read-all-sensors ( -- )
  read-temp current-temp !
  read-humidity current-humidity !
  s" Temp: " type current-temp @ . s" C, Humidity: " type current-humidity @ . s" %" type cr ;

\ --- Actuator Control ---

: gpio-set ( pin value -- )
  str-reset
  s" echo " str+
  0 <# #s #> str+
  s"  > /sys/class/gpio/gpio" str+
  swap 0 <# #s #> str+
  s" /value 2>/dev/null" str+
  str$ system drop ;

: fan-on ( -- )
  s" Fan ON" type cr
  1 fan-state !
  17 1 gpio-set ;

: fan-off ( -- )
  s" Fan OFF" type cr
  0 fan-state !
  17 0 gpio-set ;

: alert-on ( -- )
  s" ALERT!" type cr
  1 alert-state !
  18 1 gpio-set ;

: alert-off ( -- )
  0 alert-state !
  18 0 gpio-set ;

\ --- Rules Engine ---

: temp-threshold ( -- n ) 30 ;
: humidity-threshold ( -- n ) 65 ;

: check-temp-rule ( -- )
  current-temp @ temp-threshold > if
    fan-state @ 0= if
      s" Temperature high, activating fan" type cr
      fan-on
    then
  else
    fan-state @ if
      s" Temperature normal, deactivating fan" type cr
      fan-off
    then
  then ;

: check-humidity-rule ( -- )
  current-humidity @ humidity-threshold > if
    alert-state @ 0= if
      s" Humidity alert!" type cr
      alert-on
    then
  else
    alert-off
  then ;

: apply-rules ( -- )
  check-temp-rule
  check-humidity-rule ;

\ --- Main Loop ---

: sensor-cycle ( -- )
  read-all-sensors
  s" temp" current-temp @ log-reading
  s" humidity" current-humidity @ log-reading
  apply-rules ;

: run-loop ( -- )
  s" Starting sensor loop (Ctrl-C to stop)" type cr
  s" ========================================" type cr
  begin
    sensor-cycle
    s" ---" type cr
    \ In real impl: ms delay
    \ For demo: just run once
    true  \ Set to false for continuous loop
  until ;

\ --- Status Report ---

: status ( -- )
  s" IoT Device Status" type cr
  s" =================" type cr
  s" Temperature: " type current-temp @ . s" C" type cr
  s" Humidity:    " type current-humidity @ . s" %" type cr
  s" Fan:         " type fan-state @ if s" ON" else s" OFF" then type cr
  s" Alert:       " type alert-state @ if s" ACTIVE" else s" inactive" then type cr ;

: history ( -- )
  s" Recent readings:" type cr
  str-reset
  s" sqlite3 -column -header " str+
  db-file str+
  s"  \"SELECT sensor, value, timestamp FROM readings ORDER BY id DESC LIMIT 10;\"" str+
  str$ system drop ;

\ --- Main ---

: usage ( -- )
  s" IoT Scripting" type cr
  s" " type cr
  s" Usage:" type cr
  s"   ./fifth iot-scripting/main.fs run      - Start sensor loop" type cr
  s"   ./fifth iot-scripting/main.fs read     - Single reading" type cr
  s"   ./fifth iot-scripting/main.fs status   - Show status" type cr
  s"   ./fifth iot-scripting/main.fs history  - Show history" type cr ;

: main ( -- )
  init-db
  0 fan-state !
  0 alert-state !

  argc @ 2 < if
    sensor-cycle
    status
    exit
  then

  1 argv
  2dup s" run" compare 0= if 2drop run-loop exit then
  2dup s" read" compare 0= if 2drop sensor-cycle exit then
  2dup s" status" compare 0= if 2drop read-all-sensors status exit then
  2dup s" history" compare 0= if 2drop history exit then
  2drop usage ;

main
bye
