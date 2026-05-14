# webhook-subscriptions

> Create and manage webhook subscriptions for event-driven agent activation. External services POST events to trigger agent runs.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: devops, webhooks, automation, event-driven -->

## Overview

Webhooks let external services (GitHub, Stripe, CI/CD, IoT) trigger Hermes automatically.
The gateway webhook adapter listens on `http://your-server:7788/webhook/`.

## Setup

1. Start the gateway: `hermes gateway`
2. Note your public URL (use ngrok for local dev)
3. Register the URL in your external service

## ngrok for local development

```bash
ngrok http 7788
# Copy the https://xxx.ngrok.io URL
```

## GitHub Webhooks

```bash
# In GitHub repo settings → Webhooks → Add webhook
# Payload URL: https://your-server/webhook/
# Content type: application/json
# Secret: your HERMES_Gateway__Webhook__SecretKey value
# Events: Push, Pull requests, Issues
```

## Payload Format

Hermes webhook adapter expects:
```json
{
  "message": "GitHub PR #42 was opened by alice: Fix login bug",
  "chat_id": "github-alerts",
  "user_id": "github"
}
```

## Adapter Script for GitHub

```python
#!/usr/bin/env python3
# Transform GitHub webhook → Hermes webhook
import json, hmac, hashlib, requests
from flask import Flask, request

app = Flask(__name__)
HERMES_URL = "http://localhost:7788/webhook/"
HERMES_SECRET = "your-secret"

@app.route('/github', methods=['POST'])
def github():
    event = request.headers.get('X-GitHub-Event', 'unknown')
    data = request.json
    
    if event == 'pull_request':
        action = data['action']
        pr = data['pull_request']
        msg = f"GitHub PR #{pr['number']} {action}: {pr['title']} by {pr['user']['login']}"
    elif event == 'push':
        commits = len(data.get('commits', []))
        msg = f"GitHub push to {data['ref']}: {commits} commits by {data['pusher']['name']}"
    else:
        return "ignored", 200
    
    requests.post(HERMES_URL,
        json={"message": msg, "chat_id": "github"},
        headers={"X-Hermes-Secret": HERMES_SECRET})
    return "ok", 200

app.run(port=5000)
```
