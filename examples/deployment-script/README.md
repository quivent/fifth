# Deployment Script

Orchestrate multi-step deployments with rollback support.

## Features

- Check prerequisites before deploy
- Run build commands
- Copy files to targets
- Run health checks
- Automatic rollback on failure
- Deployment logging

## Usage

```bash
# Deploy to staging
./fifth examples/deployment-script/main.fs staging

# Deploy to production (requires confirmation)
./fifth examples/deployment-script/main.fs production

# Rollback last deployment
./fifth examples/deployment-script/main.fs rollback
```

## Structure

```
deployment-script/
├── main.fs          # Entry point
├── targets.fs       # Target configurations
├── checks.fs        # Health check definitions
├── deploy.log       # Deployment history
└── envs/
    ├── staging.fs
    └── production.fs
```

## Deployment Steps

1. Pre-flight checks (git status, tests)
2. Build artifacts
3. Backup current version
4. Deploy new version
5. Run health checks
6. Rollback if checks fail
