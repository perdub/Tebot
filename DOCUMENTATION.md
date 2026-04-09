# Tebot - Comprehensive Documentation

## Table of Contents

1. [Introduction](#introduction)
2. [Architecture](#architecture)
3. [Installation and Setup](#installation-and-setup)
4. [Creating a Bot](#creating-a-bot)
5. [States](#states)
6. [Commands](#commands)
7. [Working with Data](#working-with-data)
8. [Data Storage](#data-storage)
9. [Event Handling](#event-handling)
10. [API Reference](#api-reference)
11. [Usage Examples](#usage-examples)
12. [Distribution](#distribution)

---

## Introduction

**Tebot** is a wrapper library for Telegram.Bot built on top of Microsoft Orleans. It enables you to create complex Telegram bot logic using the State Machine pattern, where each user is represented by a separate Orleans grain.

### Key Benefits

- **User Isolation**: Each user is a separate class instance (grain)
- **Automatic State Management**: Built-in persistence through Orleans
- **Declarative Approach**: States and commands are defined using attributes
- **Scalability**: Orleans enables horizontal scaling of your bot
- **Simplicity**: Eliminates large switch/if-else constructs in update handlers

---

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Telegram Bot API                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    UpdateReceiver                            │
│              (IHostedService + IUpdateHandler)               │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   Orleans Grain Factory                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│          Bot<TImplementation, TState> Grain                  │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   States     │  │   Commands   │  │  Callbacks   │      │
│  │  [State()]   │  │ [Command()]  │  │ OnMessage()  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────────────────────────────────────────┐       │
│  │         IPersistentState<TState>                 │       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              Storage (Memory / PostgreSQL)                   │
└─────────────────────────────────────────────────────────────┘
```

### Key Classes

- **`Bot<TImplementation, TState>`** — Base class for your bot (Orleans Grain)
- **`BotState`** — Base class for user state
- **`TebotHostBuilder`** — Builder for configuring and running the application
- **`UpdateReceiver`** — Service that receives updates from Telegram
- **`ImplMap<T>`** — Internal class for parsing state and command attributes

---

## Installation and Setup

### Install NuGet Package

```bash
dotnet add package Tebot
```

### Minimal Configuration

Create a `config.json` file:

```json
{
  "botToken": "YOUR_BOT_TOKEN",
  "botBaseUrl": "https://api.telegram.org/bot",
  "dataStorage": "memory",
  "botClusterId": "dev-cluster",
  "botServiceId": "my-bot"
}
```

### PostgreSQL Configuration

```json
{
  "botToken": "YOUR_BOT_TOKEN",
  "botBaseUrl": "https://api.telegram.org/bot",
  "dataStorage": "adonet",
  "botClusterId": "prod-cluster",
  "botServiceId": "my-bot",
  "botClusterConnectionString": "Host=localhost;Database=orleans;Username=user;Password=pass",
  "botClusterInvariant": "Npgsql"
}
```

**Important**: For PostgreSQL, you must create Orleans tables beforehand. Use scripts from Orleans documentation.

---

## Creating a Bot

### Step 1: Define State Class

```csharp
public class MyBotState : BotState
{
    public int Counter { get; set; } = 0;
    public string? UserName { get; set; }
    public DateTime LastVisit { get; set; }
}
```

**Important**: State class must inherit from `BotState` and have a public parameterless constructor.

### Step 2: Create Bot Class

```csharp
public class MyBot : Bot<MyBot, MyBotState>
{
    [State("start")]
    public async Task Start()
    {
        await BotClient.SendMessage(ChatId, "Hello! I'm a bot.");
    }
}
```

### Step 3: Configure and Run Application

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

---

## States

### Basics

A state is a method marked with the `[State("name")]` attribute. When a user sends a message, the method corresponding to the current state is invoked.

```csharp
[State("greeting")]
public async Task Greeting()
{
    await BotClient.SendMessage(ChatId, "What's your name?");
    await GoToState("wait_name");
}

[State("wait_name")]
public async Task WaitName()
{
    Data.UserName = Text;
    await BotClient.SendMessage(ChatId, $"Nice to meet you, {Data.UserName}!");
    await GoToState("start");
}
```

### State Transitions

```csharp
// Transition with automatic save
await GoToState("new_state");

// Transition without save (must call SaveAsync() manually)
await GoToState("new_state", perfomSaving: false);
```

### Default State

The default initial state is `"start"`. You can change it in your state class:

```csharp
public class MyBotState : BotState
{
    public MyBotState()
    {
        State = "welcome"; // Custom initial state
    }
}
```

### Accessing Update Data

The following properties are available in state methods:

- **`ChatId`** — Chat ID (long)
- **`Text`** — Message text or caption
- **`currentUpdate`** — Full Update object from Telegram.Bot
- **`BotClient`** — ITelegramBotClient instance

```csharp
[State("echo")]
public async Task Echo()
{
    await BotClient.SendMessage(ChatId, $"You wrote: {Text}");
}
```

---

## Commands

### Basics

Commands are methods marked with the `[Command("/name", "description")]` attribute. They are invoked when a user sends a command (text starting with `/`).

```csharp
[Command("/start", "Start working with the bot")]
public async Task StartCommand()
{
    await BotClient.SendMessage(ChatId, "Welcome!");
    await GoToState("main_menu");
}

[Command("/help", "Show help")]
public async Task HelpCommand()
{
    await BotClient.SendMessage(ChatId, "Available commands:\n/start - start\n/help - help");
}
```

### Automatic Command Registration

All commands with descriptions are automatically registered in Telegram via `SetMyCommands`. Users will see them in the command menu.

### Private Commands

Commands without descriptions are not registered in Telegram:

```csharp
[Command("/admin", "")]
public async Task AdminCommand()
{
    // This command won't appear in the menu but will work
}
```

### Handling Commands with @username

The library automatically handles commands like `/start@botname`, stripping the `@botname` part.

---

## Working with Data

### Accessing State

User state is accessible via the `Data` property:

```csharp
[State("increment")]
public async Task Increment()
{
    Data.Counter++;
    await BotClient.SendMessage(ChatId, $"Counter: {Data.Counter}");
    await SaveAsync();
}
```

### Saving State

```csharp
// Explicit save
await SaveAsync();

// GoToState automatically saves (by default)
await GoToState("next_state");

// GoToState without save
await GoToState("next_state", perfomSaving: false);
await SaveAsync(); // Save manually
```

### Clearing State

```csharp
await ClearStateAsync();
```

This removes the user's state from storage. On the next interaction, a new state will be created.

---

## Data Storage

### Memory Storage (for development)

```json
{
  "dataStorage": "memory"
}
```

Data is stored in memory and lost on restart.

### PostgreSQL (for production)

```json
{
  "dataStorage": "adonet",
  "botClusterConnectionString": "Host=localhost;Database=orleans;Username=user;Password=pass",
  "botClusterInvariant": "Npgsql"
}
```

**Database Preparation:**

1. Install Orleans SQL scripts package
2. Execute table creation scripts for PostgreSQL
3. Configure connection string

### Custom Storage

You can use any storage supported by Orleans (SQL Server, Azure Table Storage, Redis, etc.). Modify the configuration in `TebotHostBuilder.cs`.

---

## Event Handling

### Overridable Methods

The `Bot<TImplementation, TState>` class provides virtual methods for handling different update types:

```csharp
public class MyBot : Bot<MyBot, MyBotState>
{
    protected override async Task OnUpdateReceived(Update update)
    {
        // Called for every update
        Logger?.LogInformation($"Received update type {update.Type}");
    }

    protected override async Task OnMessageReceived(Message message)
    {
        // Called for every message
        Data.LastVisit = DateTime.UtcNow;
        await SaveAsync();
    }

    protected override async Task OnInlineQueryRequest(InlineQuery inlineQuery)
    {
        // Handle inline queries
        var results = new List<InlineQueryResult>
        {
            new InlineQueryResultArticle(
                id: "1",
                title: "Result",
                inputMessageContent: new InputTextMessageContent("Text")
            )
        };
        await BotClient.AnswerInlineQuery(inlineQuery.Id, results);
    }

    protected override async Task OnInlineChosenResult(ChosenInlineResult result)
    {
        // Called when user selects an inline result
        Logger?.LogInformation($"Chosen result: {result.ResultId}");
    }
}
```

### Invocation Order

When an update is received, methods are called in the following order:

1. `OnUpdateReceived(Update)`
2. `OnMessageReceived(Message)` (if it's a message)
3. `OnInlineQueryRequest(InlineQuery)` (if it's an inline query)
4. `OnInlineChosenResult(ChosenInlineResult)` (if inline result chosen)
5. Check for command → invoke method with `[Command]`
6. If not a command → invoke method with `[State]` for current state

---

## API Reference

### TebotConfig

```csharp
public record TebotConfig
{
    // Orleans storage name for grain storage
    public string StorageName { get; set; } = "bot-data";
    
    // State type name for persistence
    public string StateName { get; set; } = "my-state";
    
    // Command line arguments
    public string[] ConsoleArguments { get; set; } = Array.Empty<string>();
    
    // Callback for configuration setup
    public Action<ConfigurationManager>? ProcessConfigurationManager { get; set; } = null;
}
```

### Bot<TImplementation, TState>

#### Properties

```csharp
protected ITelegramBotClient BotClient;           // Telegram Bot API client
protected IPersistentState<TState>? State;        // Persistent state
protected TState Data;                            // User state data
protected ILogger<TImplementation>? Logger;       // Logger
protected Update? currentUpdate;                  // Current update
protected long ChatId;                            // Chat ID
protected string Text;                            // Message text
```

#### Methods

```csharp
// Transition to new state
protected Task GoToState(string state, bool perfomSaving = true);

// Save state
protected Task SaveAsync();

// Clear state
protected Task ClearStateAsync();

// Overridable event handlers
protected virtual Task OnMessageReceived(Message message);
protected virtual Task OnUpdateReceived(Update update);
protected virtual Task OnInlineQueryRequest(InlineQuery inlineQuery);
protected virtual Task OnInlineChosenResult(ChosenInlineResult result);
```

#### Static Properties

```csharp
public static string DbStateName { get; set; }    // State name in DB
public static string DbStorageName { get; set; }  // Storage name in DB
```

### Attributes

#### StateAttribute

```csharp
[State("state_name")]
public async Task MyState() { }
```

Marks a method as a state handler.

#### CommandAttribute

```csharp
[Command("/command", "Command description")]
public async Task MyCommand() { }
```

Marks a method as a command handler. Description is used for Telegram registration.

---

## Usage Examples

### Example 1: Simple Echo Bot

```csharp
public class EchoBotState : BotState { }

public class EchoBot : Bot<EchoBot, EchoBotState>
{
    [State("start")]
    public async Task Echo()
    {
        await BotClient.SendMessage(ChatId, $"You wrote: {Text}");
    }

    [Command("/start", "Start")]
    public async Task Start()
    {
        await BotClient.SendMessage(ChatId, "Send me any message!");
    }
}
```

### Example 2: Counter with Persistence

```csharp
public class CounterState : BotState
{
    public int Count { get; set; } = 0;
}

public class CounterBot : Bot<CounterBot, CounterState>
{
    [State("start")]
    public async Task Start()
    {
        Data.Count++;
        await BotClient.SendMessage(ChatId, $"You've messaged me {Data.Count} times");
        await SaveAsync();
    }

    [Command("/reset", "Reset counter")]
    public async Task Reset()
    {
        Data.Count = 0;
        await BotClient.SendMessage(ChatId, "Counter reset");
        await SaveAsync();
    }
}
```

### Example 3: User Registration

```csharp
public class RegistrationState : BotState
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public bool IsRegistered { get; set; } = false;
}

public class RegistrationBot : Bot<RegistrationBot, RegistrationState>
{
    [Command("/start", "Start registration")]
    public async Task Start()
    {
        if (Data.IsRegistered)
        {
            await BotClient.SendMessage(ChatId, $"You're already registered as {Data.Name}, {Data.Age} years old");
            return;
        }

        await BotClient.SendMessage(ChatId, "What's your name?");
        await GoToState("wait_name");
    }

    [State("wait_name")]
    public async Task WaitName()
    {
        Data.Name = Text;
        await BotClient.SendMessage(ChatId, "How old are you?");
        await GoToState("wait_age");
    }

    [State("wait_age")]
    public async Task WaitAge()
    {
        if (int.TryParse(Text, out int age))
        {
            Data.Age = age;
            Data.IsRegistered = true;
            await BotClient.SendMessage(ChatId, $"Registration complete!\nName: {Data.Name}\nAge: {Data.Age}");
            await GoToState("start");
        }
        else
        {
            await BotClient.SendMessage(ChatId, "Please enter a number");
        }
    }

    [State("start")]
    public async Task MainMenu()
    {
        await BotClient.SendMessage(ChatId, $"Hello, {Data.Name}! How can I help?");
    }
}
```

### Example 4: Inline Keyboard

```csharp
public class MenuBot : Bot<MenuBot, BotState>
{
    [Command("/start", "Main menu")]
    public async Task Start()
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Button 1", "btn1"),
                InlineKeyboardButton.WithCallbackData("Button 2", "btn2")
            }
        });

        await BotClient.SendMessage(ChatId, "Choose an action:", replyMarkup: keyboard);
    }

    protected override async Task OnUpdateReceived(Update update)
    {
        if (update.CallbackQuery != null)
        {
            var data = update.CallbackQuery.Data;
            await BotClient.AnswerCallbackQuery(update.CallbackQuery.Id);
            await BotClient.SendMessage(ChatId, $"You clicked: {data}");
        }
    }
}
```

### Example 5: Logging and Error Handling

```csharp
public class RobustBot : Bot<RobustBot, BotState>
{
    [State("start")]
    public async Task Start()
    {
        try
        {
            Logger?.LogInformation($"User {ChatId} sent: {Text}");
            await BotClient.SendMessage(ChatId, "Processed successfully");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error processing message");
            await BotClient.SendMessage(ChatId, "An error occurred. Please try later.");
        }
    }

    protected override async Task OnUpdateReceived(Update update)
    {
        Logger?.LogDebug($"Received update: {update.Type}");
    }
}
```

---

## Distribution

### Using in Other Projects

#### Option 1: NuGet Package (Recommended)

The library is published to NuGet.org. Simply install it in any project:

```bash
dotnet add package Tebot
```

#### Option 2: Local NuGet Package

Build and pack the library locally:

```bash
cd Tebot
dotnet pack -c Release -o ./nupkg
```

This creates a `.nupkg` file in the `nupkg` folder. To use it in another project:

1. Add local NuGet source:
```bash
dotnet nuget add source /path/to/Tebot/nupkg --name LocalTebot
```

2. Install the package:
```bash
dotnet add package Tebot --version <version>
```

#### Option 3: Project Reference

If both projects are on the same machine, use a project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\Tebot\Tebot\Tebot.csproj" />
</ItemGroup>
```

#### Option 4: Private NuGet Feed

For team/organization use, set up a private NuGet feed:

- **Azure Artifacts**
- **GitHub Packages**
- **MyGet**
- **ProGet**
- Self-hosted NuGet server

Push your package to the private feed:

```bash
dotnet nuget push Tebot.0.3.x.nupkg --source https://your-feed-url --api-key YOUR_API_KEY
```

Team members can then install from the private feed.

---

## Best Practices

### 1. Always Save State

```csharp
// Bad
Data.Counter++;

// Good
Data.Counter++;
await SaveAsync();
```

### 2. Use try-catch for Critical Operations

```csharp
[State("payment")]
public async Task ProcessPayment()
{
    try
    {
        // Critical logic
    }
    catch (Exception ex)
    {
        Logger?.LogError(ex, "Payment error");
        await BotClient.SendMessage(ChatId, "Payment processing error");
    }
}
```

### 3. Don't Store Secrets in config.json

Use environment variables or Azure Key Vault:

```csharp
ProcessConfigurationManager = (manager) => {
    manager.AddJsonFile("config.json");
    manager.AddEnvironmentVariables();
}
```

### 4. Use Logging

```csharp
Logger?.LogInformation($"User {ChatId} transitioned to state {Data.State}");
```

### 5. Validate User Input

```csharp
[State("wait_number")]
public async Task WaitNumber()
{
    if (!int.TryParse(Text, out int number))
    {
        await BotClient.SendMessage(ChatId, "Please enter a number");
        return;
    }
    
    // Process number
}
```

---

## Troubleshooting

### Bot Not Responding

1. Check token in `config.json`
2. Ensure bot is running (`app.Build().Run()`)
3. Check logs for errors

### State Not Persisting

1. Ensure you're calling `await SaveAsync()`
2. Check storage settings in `config.json`
3. For PostgreSQL, verify tables are created

### Orleans Clustering Errors

1. Check `botClusterId` and `botServiceId` — they must be unique
2. For PostgreSQL, verify connection string
3. Ensure Orleans tables are created in DB

### Commands Not Registering in Telegram

1. Commands are registered on first bot startup
2. Ensure command has description: `[Command("/cmd", "description")]`
3. Restart bot and check command menu in Telegram

---

## Additional Resources

- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Telegram.Bot Library](https://github.com/TelegramBots/Telegram.Bot)
- [Microsoft Orleans Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [Orleans Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/)

---

## License

See LICENSE file in repository root.
