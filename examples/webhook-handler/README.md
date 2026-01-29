# Webhook Handler

Process incoming webhooks with validation and actions.

## Features

- Parse JSON payloads via jq
- Validate signatures
- Store events in SQLite
- Trigger downstream actions
- Logging and replay

## Usage

```bash
# Process webhook from file
./fifth examples/webhook-handler/main.fs process payload.json

# Replay failed webhooks
./fifth examples/webhook-handler/main.fs replay

# View webhook history
./fifth examples/webhook-handler/main.fs history
```

## Structure

```
webhook-handler/
├── main.fs          # Entry point
├── validators.fs    # Signature validation
├── handlers.fs      # Event handlers
├── events.db        # Event storage
└── payloads/        # Sample payloads
```

## Integration

Typically called from a simple HTTP server:
```bash
# Using socat or netcat
while true; do
  nc -l 8080 | ./fifth webhook-handler/main.fs process -
done
```

## Payload Processing

```forth
: handle-github-push ( payload -- )
  s" .commits | length" jq-query
  s" Received " type . s"  commits" type cr ;
```
