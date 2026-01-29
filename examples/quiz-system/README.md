# Quiz System

Generate and score assessments with result tracking.

## Features

- Define questions as data
- Multiple question types (multiple choice, true/false)
- Render HTML forms
- Score submissions
- Store results in SQLite
- Generate reports and analytics

## Usage

```bash
# Generate quiz HTML
./fifth examples/quiz-system/main.fs generate quiz.json

# Score submission
./fifth examples/quiz-system/main.fs score quiz.json answers.json

# View results
./fifth examples/quiz-system/main.fs report
```

## Structure

```
quiz-system/
├── main.fs          # Entry point
├── questions.fs     # Question rendering
├── scoring.fs       # Answer evaluation
├── quiz.json        # Sample quiz
├── results.db       # Stored results
└── output/
    └── quiz.html    # Generated quiz
```

## Quiz Format

```json
{
  "title": "Sample Quiz",
  "questions": [
    {
      "id": 1,
      "type": "multiple",
      "text": "What is 2+2?",
      "options": ["3", "4", "5"],
      "correct": 1
    }
  ]
}
```
