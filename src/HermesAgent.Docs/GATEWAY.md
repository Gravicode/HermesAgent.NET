# Hermes Agent Gateway Guide 🤖

The `HermesAgent.Gateway` project allows Hermes to communicate via popular messaging apps.

## Telegram Setup

1.  Create a bot via [@BotFather](https://t.me/BotFather) and get your **API Token**.
2.  Enable the adapter in your `app.config` or environment:
    ```xml
    <hermesSettings>
      <gateway>
        <telegram enabled="true" botToken="YOUR_TOKEN_HERE" />
      </gateway>
    </hermesSettings>
    ```
3.  (Optional) Restrict access by adding `allowedChatIds`.

## Discord Setup

1.  Create a new Application in the [Discord Developer Portal](https://discord.com/developers/applications).
2.  Go to the **Bot** tab, generate a token, and ensure "Message Content Intent" is enabled.
3.  Invite the bot to your server.
4.  Configure the gateway:
    ```xml
    <hermesSettings>
      <gateway>
        <discord enabled="true" botToken="YOUR_TOKEN" />
      </gateway>
    </hermesSettings>
    ```
5.  Add the target Channel IDs to `AllowedChannelIds`.

## Webhook (Inbound Only)

The Webhook adapter allows external systems (CI/CD, Monitoring) to trigger tasks in Hermes via HTTP POST.

- **Port**: Default `7788`.
- **Endpoint**: `POST http://<server>:7788/webhook/`
- **Security**: Set a `secretKey` in config and send it in the `X-Hermes-Secret` header.

**Payload:**
```json
{
  "message": "Run a system update and report back",
  "chat_id": "monitoring-room",
  "user_id": "github-actions"
}
```

## Running the Gateway

Simply run the project:
```bash
dotnet run --project src/HermesAgent.Gateway
```
The gateway will start polling all enabled adapters concurrently. Each platform-chat combination is assigned a unique, persistent session in the Hermes database.
