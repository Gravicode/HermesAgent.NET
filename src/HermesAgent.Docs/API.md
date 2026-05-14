# Hermes Agent Web API 🌐

The `HermesAgent.Web` project provides a RESTful interface to interact with the agent. By default, it runs on `http://localhost:5000`.

## Endpoints

### 1. Chat
`POST /chat`

Send a message to the agent. Supports stateful conversations and streaming.

**Request Body:**
```json
{
  "message": "Hello Hermes!",
  "session_id": "optional-uuid",
  "stream": true
}
```

**Response (Non-streaming):**
```json
{
  "response": "Hello! How can I assist you today?",
  "session_id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "turns_used": 1,
  "duration_ms": 1250,
  "tool_calls": 0
}
```

**SSE Streaming (`stream: true`):**
Responds with `text/event-stream`. Data packets are JSON:
- `{"type": "delta", "text": "..."}`
- `{"type": "tool_start", "tool": "read_file"}`
- `{"type": "tool_done", "tool": "read_file", "error": false}`
- `{"type": "done", "turns": 1, ...}`

---

### 2. Session Management
- `GET /sessions`: List recent sessions with summaries.
- `GET /sessions/{id}`: Retrieve all messages for a specific session.
- `DELETE /sessions/{id}`: Delete a session from the store.
- `GET /sessions/{id}/summary`: Force an LLM-generated summary of the session.

### 3. Skills (agentskills.io)
- `GET /skills`: List all known skills.
- `GET /skills/{name}`: Get full Markdown content of a skill.
- `POST /skills`: Manually register a new skill.
- `PATCH /skills/{name}`: Append an "Improvement" block to a skill.
- `DELETE /skills/{name}`: Remove a skill.

### 4. Memory
- `GET /memory`: Read the primary `MEMORY.md` file.
- `POST /memory/search`: Search across all sessions and explicit memories using FTS5.
  ```json
  { "query": "project requirements", "max_results": 5 }
  ```

### 5. Direct Tool Access
`POST /tools/run`

Execute any registered tool directly, bypassing the LLM loop.
```json
{
  "name": "run_command",
  "arguments": { "command": "whoami" }
}
```

---

## Direct Client Examples

### Python (Requests)
```python
import requests
resp = requests.post("http://localhost:5000/chat", json={"message": "Who are you?"})
print(resp.json()["response"])
```

### JavaScript (Fetch Streaming)
```javascript
const response = await fetch('http://localhost:5000/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message: 'Write a long story', stream: true })
});

const reader = response.body.getReader();
while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    const text = new TextDecoder().decode(value);
    console.log(text); // Handle data: prefix and parsing
}
```
