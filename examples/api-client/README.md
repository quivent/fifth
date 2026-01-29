# API Client

Wrap REST APIs in Forth words with caching.

## Features

- Shell to curl for HTTP requests
- Parse JSON responses with jq
- High-level Forth interface
- Response caching in SQLite
- Rate limiting support
- Error handling

## Usage

```bash
# Fetch weather
./fifth examples/api-client/main.fs weather "New York"

# Get GitHub user
./fifth examples/api-client/main.fs github-user octocat

# List cached responses
./fifth examples/api-client/main.fs cache
```

## Structure

```
api-client/
├── main.fs          # Entry point
├── http.fs          # HTTP primitives
├── apis/            # API wrappers
│   ├── github.fs
│   ├── weather.fs
│   └── json-placeholder.fs
└── cache.db         # Response cache
```

## HTTP Pattern

```forth
: http-get ( url-addr url-u -- response-addr response-u )
  str-reset
  s" curl -s " str+
  str+
  str$ system
  \ Parse response... ;

: github-user ( username-addr username-u -- )
  str-reset
  s" https://api.github.com/users/" str+
  str+
  str$ http-get
  \ Parse and display... ;
```

## Caching

Responses are cached with TTL:
```forth
: cached-get ( url-addr url-u ttl-seconds -- response-addr response-u )
  \ Check cache first, fetch if stale
```
