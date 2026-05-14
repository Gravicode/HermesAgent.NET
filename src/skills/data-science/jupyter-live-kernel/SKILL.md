# jupyter-live-kernel

> Use a live Jupyter kernel for stateful, iterative Python execution. Load this skill when the task involves exploration, iteration, or inspecting intermediate results.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: data-science, python, jupyter, interactive -->

## Overview

Use `hamelnb` or `jupyter nbconvert` for stateful, iterative Python execution with a live kernel.

## Setup

```bash
pip install jupyter hamelnb
jupyter kernel &   # start a kernel
```

## Usage with hamelnb

```bash
# Execute a cell and get output
hamelnb exec "import pandas as pd; df = pd.read_csv('data.csv'); df.describe()"

# Inspect variable
hamelnb exec "df.head(10)"

# Plot (saves to file)
hamelnb exec "df['revenue'].plot(); plt.savefig('revenue.png')"
```

## Usage with nbconvert

```bash
# Run a notebook
jupyter nbconvert --to notebook --execute analysis.ipynb --output analysis_output.ipynb

# Convert to HTML for viewing
jupyter nbconvert --to html analysis_output.ipynb
```

## Workflow for Data Exploration

1. Load data, inspect shape and types
2. Check for missing values and distributions
3. Clean and transform
4. Explore correlations
5. Visualize key patterns
6. Build models or aggregations
7. Export results

## Common Patterns

```python
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np

# Load and inspect
df = pd.read_csv('data.csv')
print(df.shape, df.dtypes, df.isnull().sum())

# Quick summary
df.describe()

# Value counts for categoricals
df['category'].value_counts().head(10)

# Group aggregation
df.groupby('region')['revenue'].agg(['sum','mean','count'])

# Correlation matrix
df.select_dtypes(include=np.number).corr()
```
