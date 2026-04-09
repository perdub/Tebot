# Tebot

[![NuGet Version](https://img.shields.io/nuget/v/Tebot?color=blue)](https://www.nuget.org/packages/Tebot/)

Orleans-based wrapper library for Telegram.Bot that simplifies complex bot logic using the State Machine pattern. Each user is represented as a separate Orleans grain with persistent state.

## Documentation

- **[Full Documentation](docs/DOCUMENTATION.md)** - Complete API reference, examples, and best practices
- **[AGENTS.md](docs/AGENTS.md)** - Quick reference for AI assistants and developers

> **For AI Assistants**: Read `docs/DOCUMENTATION.md` for complete API documentation before working with this library.

## Why?

When building Telegram bots with Telegram.Bot, you typically end up with one large `OnUpdate(Update update, ...)` method filled with if/else statements and switch cases. This approach quickly becomes unmaintainable as your bot grows.

Tebot solves this by representing each user as a separate class instance (Orleans grain), where all updates and actions for a specific user are isolated to that instance.

## Quick Start

### Installation

```bash
dotnet add package Tebot
```

### 1. Define Your State

```csharp
public class MyBotState : BotState
{
    public int Counter { get; set; } = 0;
    public string? UserName { get; set; }
}
```

### 2. Create Your Bot

```csharp
public class MyBot : Bot<MyBot, MyBotState>
{
    [State("start")]
    public async Task Start()
    {
        Data.Counter++;
        await BotClient.SendMessage(ChatId, $"Hello! Message count: {Data.Counter}");
        await SaveAsync();
    }

    [Command("/reset", "Reset counter")]
    public async Task Reset()
    {
        Data.Counter = 0;
        await BotClient.SendMessage(ChatId, "Counter reset!");
        await SaveAsync();
    }
}
```

### 3. Configure and Run

```csharp
var app = TebotHostBuilder.Build<MyBot, MyBotState>(new TebotConfig
{
    ConsoleArguments = args,
    StorageName = "my-bot-storage",
    StateName = "my-bot-state",
    ProcessConfigurationManager = (manager) => {
        manager.AddJsonFile("config.json");
    }
});

app.Build().Run();
```

### 4. Configuration File

Create `config.json`:

```json
{
  "botToken": "YOUR_BOT_TOKEN",
  "botBaseUrl": "https://api.telegram.org/bot",
  "dataStorage": "memory",
  "botClusterId": "dev-cluster",
  "botServiceId": "my-bot"
}
```

## State Machine Example

```csharp
public class RegistrationBot : Bot<RegistrationBot, RegistrationState>
{
    [Command("/start", "Start registration")]
    public async Task Start()
    {
        await BotClient.SendMessage(ChatId, "What's your name?");
        await GoToState("wait_name");
    }

    [State("wait_name")]
    public async Task WaitName()
    {
        Data.UserName = Text;
        await BotClient.SendMessage(ChatId, "How old are you?");
        await GoToState("wait_age");
    }

    [State("wait_age")]
    public async Task WaitAge()
    {
        if (int.TryParse(Text, out int age))
        {
            Data.Age = age;
            await BotClient.SendMessage(ChatId, $"Registration complete!\nName: {Data.UserName}\nAge: {Data.Age}");
            await GoToState("start");
        }
        else
        {
            await BotClient.SendMessage(ChatId, "Please enter a valid number");
        }
    }
}
```

## Key Features

- **State Machine Pattern**: Define states using `[State("name")]` attributes
- **Commands**: Register bot commands with `[Command("/cmd", "description")]`
- **Persistent State**: Automatic state persistence via Orleans (Memory or PostgreSQL)
- **User Isolation**: Each user gets their own grain instance
- **Scalability**: Built on Orleans for horizontal scaling
- **Event Hooks**: Override methods like `OnMessageReceived()`, `OnUpdateReceived()`, etc.

## Storage Options

### Memory (Development)
```json
{
  "dataStorage": "memory"
}
```

### PostgreSQL (Production)
```json
{
  "dataStorage": "adonet",
  "botClusterConnectionString": "Host=localhost;Database=orleans;Username=user;Password=pass",
  "botClusterInvariant": "Npgsql"
}
```

## Available Properties in Bot Methods

- `ChatId` - Current chat ID (long)
- `Text` - Message text or caption
- `Data` - User's persistent state
- `BotClient` - Telegram Bot API client
- `currentUpdate` - Full Update object
- `Logger` - ILogger instance

## Override Methods

```csharp
protected override async Task OnMessageReceived(Message message)
{
    // Called for every message
}

protected override async Task OnUpdateReceived(Update update)
{
    // Called for every update
}

protected override async Task OnInlineQueryRequest(InlineQuery inlineQuery)
{
    // Handle inline queries
}
```

## More Examples

See [TebotAsHost/Program.cs](https://github.com/perdub/Tebot/blob/main/TebotAsHost/Program.cs) for a complete working example.

For detailed documentation, examples, and best practices, see [docs/DOCUMENTATION.md](docs/DOCUMENTATION.md).
