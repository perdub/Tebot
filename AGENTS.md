# AGENTS.md

## Project Structure

.NET solution with two projects:
- `Tebot/` - NuGet library (multi-target: net8.0, net9.0, net10.0)
- `TebotAsHost/` - Example console app (net10.0)

## Build & Pack

```bash
dotnet build
dotnet pack Tebot/Tebot.csproj -c Release /p:Version=<version>
```

CI builds on push (except .md files) and publishes to NuGet with version `0.3.${{ github.run_number }}-beta`.

## Architecture

Orleans-based Telegram bot framework. Each user is a grain (`Bot<TImplementation, TState>`). State machine pattern with:
- States: methods with `[State("name")]` attribute
- Commands: methods with `[Command("/cmd", "description")]` attribute
- Persistent state via Orleans grain storage (memory or ADO.NET/PostgreSQL)

## Configuration

TebotAsHost example uses `config.json` with:
- `botToken`, `botBaseUrl` - Telegram credentials
- `dataStorage` - "memory" or "adonet"
- `botClusterId`, `botServiceId` - Orleans cluster config
- `botClusterConnectionString`, `botClusterInvariant` - DB connection (if adonet)

**WARNING**: `TebotAsHost/config.json` contains live credentials and DB connection strings. Never commit secrets.

## Key Files

- `Tebot/Grains/Bot.cs` - Core grain implementation with state machine logic
- `Tebot/TebotHostBuilder.cs` - Host builder with Orleans + DI setup
- `TebotAsHost/Program.cs` - Example usage pattern

## Dependencies

- Telegram.Bot 22.9.5.3
- Microsoft.Orleans.* 10.0.1
- .NET 10 preview (11.0.100-preview.1.26104.118)

## No Tests

No test projects in solution.
