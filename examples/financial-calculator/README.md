# Financial Calculator

RPN calculator and financial modeling tools.

## Features

- Compound interest calculations
- Amortization schedules
- Loan comparisons
- Investment projections
- HTML report generation

## Usage

```bash
# Interactive calculator
./fifth examples/financial-calculator/main.fs

# Generate amortization schedule
./fifth examples/financial-calculator/main.fs amortize 250000 30 6.5
```

## Structure

```
financial-calculator/
├── main.fs          # Entry point
├── compound.fs      # Compound interest
├── amortize.fs      # Loan amortization
├── invest.fs        # Investment calcs
└── output/          # Generated reports
```

## Stack-Based Calculation

The stack naturally holds intermediate values:
```forth
\ Calculate monthly payment
250000 360 6.5 monthly-payment .
```

## Note on Precision

All calculations use integer cents (multiply by 100) to avoid floating-point errors. Final display divides back to dollars.
