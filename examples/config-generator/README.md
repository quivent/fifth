# Configuration Generator

Generate nginx, Apache, systemd, and other config files from templates.

## Features

- Define config blocks as Forth words
- Compose configurations from components
- Environment-specific variations
- Validation before output
- Dry-run mode

## Usage

```bash
# Generate nginx config
./fifth examples/config-generator/main.fs nginx

# Generate systemd unit
./fifth examples/config-generator/main.fs systemd myapp
```

## Structure

```
config-generator/
├── main.fs          # Entry point
├── nginx.fs         # Nginx templates
├── systemd.fs       # Systemd templates
├── envs/            # Environment configs
│   ├── dev.fs
│   └── prod.fs
└── output/          # Generated configs
```

## Template Pattern

```forth
: server-block ( port -- )
  <server>
    s" listen " emit . emit-nl
    s" server_name localhost;" emit-nl
  </server> ;
```
