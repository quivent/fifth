# App from Spec - Multi-Stage AI Code Generation

Generate complete, runnable applications from YAML specifications using staged LLM code synthesis.

## Overview

This example demonstrates Fifth as a sophisticated code generation orchestrator. Given a simple specification file, it generates:

1. Architecture document (components, data flow, interfaces)
2. Database schema (tables, relationships, migrations)
3. API routes and handlers (REST endpoints)
4. Frontend components (React/HTML)
5. Test suites (unit, integration, e2e)

Each stage uses tailored prompts and validates output before proceeding.

## The Pipeline

```
spec.yaml
    |
    v
[Stage 1: Architecture]  -->  architecture.md
    |
    v
[Stage 2: Schema]        -->  schema.sql, migrations/
    |
    v
[Stage 3: API]           -->  routes/, handlers/, middleware/
    |
    v
[Stage 4: Frontend]      -->  components/, pages/, styles/
    |
    v
[Stage 5: Tests]         -->  tests/unit/, tests/integration/
    |
    v
[Validation]             -->  Report: ready / issues found
```

## Quick Start

```bash
# Create a spec file
cat > myapp.yaml << 'EOF'
name: TaskTracker
description: Simple task management application
entities:
  - name: Task
    fields:
      - name: title
        type: string
        required: true
      - name: description
        type: text
      - name: status
        type: enum
        values: [todo, in_progress, done]
      - name: due_date
        type: date
  - name: User
    fields:
      - name: email
        type: string
        required: true
        unique: true
      - name: name
        type: string
relations:
  - type: belongs_to
    from: Task
    to: User
    as: assignee
features:
  - authentication
  - crud
  - filtering
stack:
  backend: express
  database: sqlite
  frontend: react
EOF

# Generate the full application
./fifth examples/app-from-spec/main.fs generate myapp.yaml output/

# Or run individual stages
./fifth examples/app-from-spec/main.fs stage architecture myapp.yaml
./fifth examples/app-from-spec/main.fs stage schema myapp.yaml
./fifth examples/app-from-spec/main.fs stage api myapp.yaml
./fifth examples/app-from-spec/main.fs stage frontend myapp.yaml
./fifth examples/app-from-spec/main.fs stage tests myapp.yaml

# Regenerate a specific stage (preserves others)
./fifth examples/app-from-spec/main.fs regen api myapp.yaml output/
```

## How LLMs Are Used at Each Stage

### Stage 1: Architecture

**Input**: Raw spec YAML
**Prompt Strategy**: High-level system design

```
Given this application specification:
{spec_yaml}

Generate an architecture document that includes:
1. System components and their responsibilities
2. Data flow diagrams (as ASCII art)
3. Interface contracts between components
4. Security considerations
5. Scalability notes

Output format: Markdown with clear sections.
```

**Validation**:
- Document must contain required sections
- Component names must match entities in spec
- Data flow must be coherent

### Stage 2: Database Schema

**Input**: Spec YAML + Architecture document
**Prompt Strategy**: Precise SQL generation

```
Based on this architecture:
{architecture_md}

And these entity definitions:
{entities_yaml}

Generate:
1. Complete SQL schema (CREATE TABLE statements)
2. Foreign key relationships
3. Indexes for common query patterns
4. Migration file structure

Database: {stack.database}
Output: Valid SQL that can be executed directly.
```

**Validation**:
- SQL syntax check via `sqlite3 :memory:`
- All entities from spec must have tables
- Foreign keys must reference valid tables
- Required fields must be NOT NULL

### Stage 3: API Routes

**Input**: Spec + Architecture + Schema
**Prompt Strategy**: RESTful endpoint generation

```
Given this schema:
{schema_sql}

And these features:
{features}

Generate REST API routes for {stack.backend}:
1. CRUD endpoints for each entity
2. Authentication middleware (if feature enabled)
3. Validation middleware
4. Error handling
5. OpenAPI/Swagger annotations

Include proper HTTP methods, status codes, and response formats.
```

**Validation**:
- Each entity has CRUD routes
- Route handlers reference valid schema fields
- Middleware chain is complete
- Syntax validation for target language

### Stage 4: Frontend Components

**Input**: Spec + Architecture + Schema + API routes
**Prompt Strategy**: Component-driven UI

```
Given these API endpoints:
{api_routes}

And this entity structure:
{entities}

Generate {stack.frontend} components:
1. List view for each entity
2. Detail/edit forms
3. Navigation structure
4. API client hooks/services
5. Basic styling

Components should use modern patterns (hooks, composition).
```

**Validation**:
- Components exist for each entity
- API calls match route definitions
- Form fields match entity schema
- No hardcoded URLs

### Stage 5: Test Generation

**Input**: All previous artifacts
**Prompt Strategy**: Comprehensive test coverage

```
Given this application:
- Schema: {schema}
- API: {routes}
- Components: {components}

Generate tests:
1. Unit tests for each API handler
2. Integration tests for CRUD flows
3. Component tests for forms
4. E2E test scenarios

Use {test_framework}. Include edge cases and error handling.
```

**Validation**:
- Test files exist for each source file
- Tests cover happy path and error cases
- No tests reference undefined entities
- Tests are syntactically valid

## Validation and Iteration

Each stage includes validation that can trigger regeneration:

```forth
: validate-stage ( stage-id -- valid? )
  case
    STAGE-ARCH     of validate-architecture endof
    STAGE-SCHEMA   of validate-schema       endof
    STAGE-API      of validate-api          endof
    STAGE-FRONTEND of validate-frontend     endof
    STAGE-TESTS    of validate-tests        endof
  endcase ;

: run-stage-with-retry ( stage-id max-retries -- success? )
  0 do
    dup run-stage
    dup validate-stage if
      drop true unloop exit
    then
    i 1+ . ." retry..." cr
    regenerate-with-feedback
  loop
  drop false ;
```

Validation feedback is included in regeneration prompts:

```
Previous generation had these issues:
- Missing foreign key for Task.user_id
- Invalid SQL syntax on line 23

Please regenerate fixing these specific issues.
```

## File Generation

The generator creates a complete project structure:

```
output/
  README.md                 # Generated project docs
  package.json              # Dependencies

  src/
    db/
      schema.sql            # Stage 2 output
      migrations/
        001_initial.sql

    api/
      routes/
        tasks.js            # Stage 3 output
        users.js
      middleware/
        auth.js
        validate.js
      handlers/
        tasks.js
        users.js

    frontend/
      components/           # Stage 4 output
        TaskList.jsx
        TaskForm.jsx
        UserList.jsx
      pages/
        Dashboard.jsx
      services/
        api.js

  tests/
    unit/                   # Stage 5 output
      tasks.test.js
      users.test.js
    integration/
      crud.test.js
    e2e/
      flows.test.js

  docs/
    architecture.md         # Stage 1 output
    api.md
```

## Configuration

Environment variables:

```bash
export ANTHROPIC_API_KEY="sk-..."     # Required
export APP_GEN_MODEL="claude-sonnet-4-20250514"  # Optional, default shown
export APP_GEN_MAX_TOKENS="8192"      # Optional
export APP_GEN_TEMP="0.2"             # Optional, low for code
```

## Supported Stacks

**Backend**:
- `express` - Node.js/Express
- `fastapi` - Python/FastAPI
- `go` - Go/Chi router

**Database**:
- `sqlite` - SQLite
- `postgres` - PostgreSQL

**Frontend**:
- `react` - React with hooks
- `html` - Static HTML/CSS/JS

## Advanced: Custom Templates

Override default prompts with custom templates:

```yaml
# In spec.yaml
templates:
  schema_prompt: |
    Generate schema following our conventions:
    - All tables prefixed with app_
    - Use BIGINT for IDs
    - Include audit columns (created_at, updated_at)
    {entities}

  api_style: functional  # or class-based
```

## How Fifth Orchestrates Generation

Fifth excels at orchestration because:

1. **Shell-out pattern**: Uses `curl` for LLM APIs, `jq`/`yq` for parsing, standard tools for file ops
2. **Buffer system**: Builds complex prompts without allocation issues
3. **Linear execution**: Clear stage-by-stage flow with checkpoints
4. **Validation hooks**: Each stage can validate and retry

```forth
\ Core orchestration loop
: generate-app ( spec-file output-dir -- )
  parse-spec                    \ Load YAML via yq

  STAGE-ARCH run-stage-with-retry 0= if
    ." Architecture generation failed" cr exit
  then

  STAGE-SCHEMA run-stage-with-retry 0= if
    ." Schema generation failed" cr exit
  then

  \ ... remaining stages ...

  ." Application generated successfully" cr
  show-summary ;
```

## Limitations

- LLM output requires review before production use
- Complex business logic may need manual refinement
- Generated tests provide structure but need assertion refinement
- Large specs may hit token limits (split into modules)

## Example Output

Running against the TaskTracker spec produces:

```
$ ./fifth examples/app-from-spec/main.fs generate tasktracker.yaml output/

App from Spec - Multi-Stage Code Generation
============================================

Parsing specification: tasktracker.yaml
  Name: TaskTracker
  Entities: Task, User
  Features: authentication, crud, filtering
  Stack: express / sqlite / react

Stage 1: Architecture
  Generating architecture document...
  Validating... OK
  Written: output/docs/architecture.md

Stage 2: Database Schema
  Generating schema from architecture...
  Validating SQL syntax... OK
  Validating entity coverage... OK
  Written: output/src/db/schema.sql

Stage 3: API Routes
  Generating REST endpoints...
  Validating route coverage... OK
  Written: output/src/api/routes/tasks.js
  Written: output/src/api/routes/users.js
  Written: output/src/api/middleware/auth.js

Stage 4: Frontend Components
  Generating React components...
  Validating component coverage... OK
  Written: output/src/frontend/components/TaskList.jsx
  Written: output/src/frontend/components/TaskForm.jsx
  Written: output/src/frontend/components/UserList.jsx

Stage 5: Test Suite
  Generating test files...
  Written: output/tests/unit/tasks.test.js
  Written: output/tests/unit/users.test.js
  Written: output/tests/integration/crud.test.js

Summary
-------
Files generated: 14
Lines of code: ~1,200
Time elapsed: 45s

Next steps:
  cd output/
  npm install
  npm run migrate
  npm run dev
```

## See Also

- `examples/code-generator/` - Simpler single-entity generation
- `examples/agentic-coder/` - Interactive AI coding assistant
- `examples/project-scaffolder/` - Template-based scaffolding
