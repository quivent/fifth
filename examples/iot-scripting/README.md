# IoT Device Scripting

Lightweight automation for resource-constrained devices.

## Features

- Read sensors via shell commands
- Apply thresholds and rules
- Trigger actuators
- Log to local SQLite
- Small memory footprint
- No dynamic allocation

## Usage

```bash
# Run sensor loop
./fifth examples/iot-scripting/main.fs

# Single reading
./fifth examples/iot-scripting/main.fs read

# Check thresholds
./fifth examples/iot-scripting/main.fs check
```

## Structure

```
iot-scripting/
├── main.fs          # Entry point
├── sensors.fs       # Sensor reading
├── rules.fs         # Threshold rules
├── actions.fs       # Actuator control
└── data.db          # Local storage
```

## Sensor Interface

Sensors are read via shell commands that return values:
```bash
# Temperature sensor (returns degrees C)
cat /sys/class/thermal/thermal_zone0/temp

# GPIO state
cat /sys/class/gpio/gpio17/value
```

## Rules

Define simple rules in Forth:
```forth
: check-temp ( temp -- )
  30 > if fan-on else fan-off then ;
```
