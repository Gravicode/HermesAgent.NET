# github-code-review

> Review code changes by analyzing git diffs, leaving inline comments on PRs, and performing pre-push review.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: github, code-review, quality -->

## Overview

Systematic code review workflow using `gh` CLI or GitHub REST API.

## Review Checklist

When reviewing code, always check:

1. **Correctness** — Does the code do what it claims?
2. **Security** — SQL injection, XSS, auth bypasses, secrets in code?
3. **Performance** — N+1 queries, missing indices, unbounded loops?
4. **Error handling** — Are exceptions caught and handled properly?
5. **Tests** — Are new paths covered by tests?
6. **Naming** — Are variables, functions, classes named clearly?
7. **SOLID principles** — Single responsibility, open/closed, etc.

## Workflow

### Get the diff

```bash
# Current branch vs main
git diff main...HEAD

# Specific PR
gh pr diff <PR_NUMBER>
```

### Leave inline comments

```bash
# Via gh CLI
gh pr review <PR_NUMBER> --comment --body "Great work, one suggestion..."

# Leave inline comment on specific line
gh api repos/<owner>/<repo>/pulls/<PR_NUMBER>/comments \
  -f body="<comment>" \
  -f commit_id="<sha>" \
  -f path="<file>" \
  -F position=<line_number>
```

### Approve or request changes

```bash
gh pr review <PR_NUMBER> --approve
gh pr review <PR_NUMBER> --request-changes --body "Please address the security issues."
```

## Security Review Patterns

```bash
# Check for hardcoded secrets
grep -r "password\|secret\|api_key\|token" --include="*.py" --include="*.ts" --include="*.cs" .

# Check for SQL injection risks
grep -r "execute\|query\|raw" --include="*.py" . | grep -v "prepared\|parameterized"
```

## Pre-push Self-Review

Before pushing, always run:
```bash
git diff main...HEAD | head -500  # Check your own diff
# Run linter
# Run tests
# Check for debug code / TODOs
```
