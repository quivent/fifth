# Kiosk Display System

Drive information displays with auto-refresh.

## Features

- Query data sources
- Generate full-screen HTML
- Auto-refresh on schedule
- Shell to browser in kiosk mode
- Multiple display layouts
- No runtime server needed

## Usage

```bash
# Generate display page
./fifth examples/kiosk-display/main.fs

# Launch in kiosk mode (Linux/X11)
./fifth examples/kiosk-display/main.fs launch
```

## Structure

```
kiosk-display/
├── main.fs          # Entry point
├── layouts/         # Display layouts
│   ├── dashboard.fs
│   ├── slideshow.fs
│   └── fullscreen.fs
├── data.db          # Local data cache
└── output/
    └── display.html
```

## Kiosk Setup

The display HTML includes:
- Auto-refresh meta tag
- Full-screen CSS
- Clock/time display
- Configurable content zones

## Browser Launch

```bash
# Chromium kiosk mode
chromium --kiosk --noerrdialogs file:///path/to/display.html

# Firefox kiosk
firefox --kiosk file:///path/to/display.html
```
