# claude-code

> Delegate coding tasks to Claude Code (Anthropic's CLI agent). Use for building features, refactoring, PR reviews, and iterative coding.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: coding, delegation, claude, ai-agents -->

## Overview

Claude Code is Anthropic's CLI coding agent. Use it to delegate complex coding tasks that require many file edits, refactoring across multiple files, or building features end-to-end.

## Prerequisites

```bash
# Install
npm install -g @anthropic-ai/claude-code

# Authenticate
claude-code auth
```

## Usage Patterns

### One-shot task

```bash
claude-code -q "Add input validation to the UserController.cs registration endpoint"
```

### Interactive session

```bash
claude-code  # Opens interactive REPL
```

### Build a feature

```bash
claude-code -q "Implement a rate-limiting middleware for the ASP.NET API. 
Use sliding window algorithm, 100 req/min per IP, return 429 with Retry-After header.
Add unit tests."
```

### Refactor

```bash
claude-code -q "Refactor all synchronous database calls in /src to use async/await.
Ensure no sync-over-async patterns remain."
```

### Code review

```bash
git diff main...HEAD | claude-code -q "Review this diff for bugs, security issues, and performance problems."
```

## Tips

- Use `-q` (quiet/non-interactive) for automation
- Provide context: file paths, requirements, constraints
- Ask for tests alongside implementation
- Review output before committing
- Use `--no-auto-commit` if you want to review changes first

## When to Use Claude Code vs Hermes directly

- Use Claude Code for: large codebases, multi-file refactoring, test writing
- Use Hermes directly for: quick edits, file operations, system tasks, research
