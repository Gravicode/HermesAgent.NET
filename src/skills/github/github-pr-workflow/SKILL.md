# github-pr-workflow

> Full pull request lifecycle — create branches, commit changes, open PRs, monitor CI status, auto-fix failures, and merge.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: github, git, pr, workflow -->

## Overview

Manage the full GitHub Pull Request lifecycle using the `gh` CLI or `git` + GitHub REST API as fallback.

## Prerequisites

- `gh` CLI installed and authenticated (`gh auth status`), OR
- `git` + `GITHUB_TOKEN` env var set

## Workflow

### 1. Create a feature branch

```bash
git checkout -b feature/<branch-name>
```

### 2. Make changes and commit

```bash
git add -A
git commit -m "feat: <description>"
```

### 3. Push and open PR

```bash
# With gh CLI
gh pr create --title "<title>" --body "<description>" --base main

# Fallback: push then open via API
git push -u origin HEAD
curl -s -X POST https://api.github.com/repos/<owner>/<repo>/pulls \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"<title>","body":"<body>","head":"<branch>","base":"main"}'
```

### 4. Monitor CI status

```bash
gh pr checks
# or
gh run list --branch $(git branch --show-current)
```

### 5. Auto-fix CI failures

If CI fails, read the logs and fix:
```bash
gh run view --log-failed
# Fix the issue, commit, push
git add -A && git commit -m "fix: resolve CI failure" && git push
```

### 6. Merge when approved

```bash
gh pr merge --squash --delete-branch
```

## Tips

- Always run tests locally before pushing: `npm test` / `pytest` / `dotnet test`
- Use conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`
- Request reviews: `gh pr edit --add-reviewer <username>`
- Check for conflicts: `git fetch origin && git merge origin/main`
