# Recipe Manager

Store, scale, and manage recipes with inventory tracking.

## Features

- Store recipes with ingredients
- Scale recipes by servings
- Generate shopping lists
- Track inventory in SQLite
- Nutritional calculations
- HTML export for printing

## Usage

```bash
# List recipes
./fifth examples/recipe-manager/main.fs list

# View recipe
./fifth examples/recipe-manager/main.fs view "Chocolate Cake"

# Scale recipe
./fifth examples/recipe-manager/main.fs scale "Chocolate Cake" 2

# Generate shopping list
./fifth examples/recipe-manager/main.fs shop
```

## Structure

```
recipe-manager/
├── main.fs          # Entry point
├── recipes.fs       # Recipe operations
├── scaling.fs       # Quantity scaling
├── recipes.db       # Recipe database
└── output/
    └── shopping.html
```

## Database Schema

```sql
CREATE TABLE recipes (id, name, servings, instructions);
CREATE TABLE ingredients (id, recipe_id, name, amount, unit);
CREATE TABLE inventory (id, name, quantity, unit);
```
