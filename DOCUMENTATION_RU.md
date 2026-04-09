# Tebot - Подробная документация

## Содержание

1. [Введение](#введение)
2. [Архитектура](#архитектура)
3. [Установка и настройка](#установка-и-настройка)
4. [Создание бота](#создание-бота)
5. [Состояния (States)](#состояния-states)
6. [Команды (Commands)](#команды-commands)
7. [Работа с данными](#работа-с-данными)
8. [Хранилище данных](#хранилище-данных)
9. [Обработка событий](#обработка-событий)
10. [API Reference](#api-reference)
11. [Примеры использования](#примеры-использования)

---

## Введение

**Tebot** — это библиотека-обёртка над Telegram.Bot, построенная на базе Microsoft Orleans. Она позволяет создавать сложную логику Telegram-ботов с использованием паттерна "конечный автомат" (State Machine), где каждый пользователь представлен отдельным grain'ом Orleans.

### Основные преимущества

- **Изоляция пользователей**: каждый пользователь — отдельный экземпляр класса (grain)
- **Автоматическое управление состоянием**: встроенная персистентность через Orleans
- **Декларативный подход**: состояния и команды описываются атрибутами
- **Масштабируемость**: Orleans позволяет горизонтально масштабировать бота
- **Простота**: избавляет от больших switch/if-else конструкций в обработчике обновлений

---

## Архитектура

### Основные компоненты

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

### Ключевые классы

- **`Bot<TImplementation, TState>`** — базовый класс для вашего бота (Orleans Grain)
- **`BotState`** — базовый класс для состояния пользователя
- **`TebotHostBuilder`** — билдер для настройки и запуска приложения
- **`UpdateReceiver`** — сервис, принимающий обновления от Telegram
- **`ImplMap<T>`** — внутренний класс для парсинга атрибутов состояний и команд

---

## Установка и настройка

### Установка NuGet пакета

```bash
dotnet add package Tebot
```

### Минимальная настройка

Создайте файл `config.json`:

```json
{
  "botToken": "YOUR_BOT_TOKEN",
  "botBaseUrl": "https://api.telegram.org/bot",
  "dataStorage": "memory",
  "botClusterId": "dev-cluster",
  "botServiceId": "my-bot"
}
```

### Настройка с PostgreSQL

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

**Важно**: Для работы с PostgreSQL необходимо предварительно создать таблицы Orleans. Используйте скрипты из документации Orleans.

---

## Создание бота

### Шаг 1: Определите класс состояния

```csharp
public class MyBotState : BotState
{
    public int Counter { get; set; } = 0;
    public string? UserName { get; set; }
    public DateTime LastVisit { get; set; }
}
```

**Важно**: Класс состояния должен наследоваться от `BotState` и иметь публичный конструктор без параметров.

### Шаг 2: Создайте класс бота

```csharp
public class MyBot : Bot<MyBot, MyBotState>
{
    [State("start")]
    public async Task Start()
    {
        await BotClient.SendMessage(ChatId, "Привет! Я бот.");
    }
}
```

### Шаг 3: Настройте и запустите приложение

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

## Состояния (States)

### Основы

Состояние — это метод, помеченный атрибутом `[State("name")]`. Когда пользователь отправляет сообщение, вызывается метод, соответствующий текущему состоянию.

```csharp
[State("greeting")]
public async Task Greeting()
{
    await BotClient.SendMessage(ChatId, "Как тебя зовут?");
    await GoToState("wait_name");
}

[State("wait_name")]
public async Task WaitName()
{
    Data.UserName = Text;
    await BotClient.SendMessage(ChatId, $"Приятно познакомиться, {Data.UserName}!");
    await GoToState("start");
}
```

### Переход между состояниями

```csharp
// Переход с автоматическим сохранением
await GoToState("new_state");

// Переход без сохранения (нужно вызвать SaveAsync() вручную)
await GoToState("new_state", perfomSaving: false);
```

### Состояние по умолчанию

По умолчанию начальное состояние — `"start"`. Вы можете изменить его в классе состояния:

```csharp
public class MyBotState : BotState
{
    public MyBotState()
    {
        State = "welcome"; // Кастомное начальное состояние
    }
}
```

### Доступ к данным обновления

В методах состояний доступны следующие свойства:

- **`ChatId`** — ID чата (long)
- **`Text`** — текст сообщения или caption
- **`currentUpdate`** — полный объект Update от Telegram.Bot
- **`BotClient`** — экземпляр ITelegramBotClient

```csharp
[State("echo")]
public async Task Echo()
{
    await BotClient.SendMessage(ChatId, $"Вы написали: {Text}");
}
```

---

## Команды (Commands)

### Основы

Команды — это методы, помеченные атрибутом `[Command("/name", "description")]`. Они вызываются, когда пользователь отправляет команду (текст, начинающийся с `/`).

```csharp
[Command("/start", "Начать работу с ботом")]
public async Task StartCommand()
{
    await BotClient.SendMessage(ChatId, "Добро пожаловать!");
    await GoToState("main_menu");
}

[Command("/help", "Показать справку")]
public async Task HelpCommand()
{
    await BotClient.SendMessage(ChatId, "Доступные команды:\n/start - начать\n/help - справка");
}
```

### Автоматическая регистрация команд

Все команды с описанием автоматически регистрируются в Telegram через `SetMyCommands`. Пользователи увидят их в меню команд.

### Приватные команды

Команды без описания не регистрируются в Telegram:

```csharp
[Command("/admin", "")]
public async Task AdminCommand()
{
    // Эта команда не появится в меню, но будет работать
}
```

### Обработка команд с @username

Библиотека автоматически обрабатывает команды вида `/start@botname`, отсекая `@botname`.

---

## Работа с данными

### Доступ к состоянию

Состояние пользователя доступно через свойство `Data`:

```csharp
[State("increment")]
public async Task Increment()
{
    Data.Counter++;
    await BotClient.SendMessage(ChatId, $"Счётчик: {Data.Counter}");
    await SaveAsync();
}
```

### Сохранение состояния

```csharp
// Явное сохранение
await SaveAsync();

// GoToState автоматически сохраняет (по умолчанию)
await GoToState("next_state");

// GoToState без сохранения
await GoToState("next_state", perfomSaving: false);
await SaveAsync(); // Сохраняем вручную
```

### Очистка состояния

```csharp
await ClearStateAsync();
```

Это удаляет состояние пользователя из хранилища. При следующем обращении будет создано новое состояние.

---

## Хранилище данных

### Memory Storage (для разработки)

```json
{
  "dataStorage": "memory"
}
```

Данные хранятся в памяти и теряются при перезапуске.

### PostgreSQL (для продакшена)

```json
{
  "dataStorage": "adonet",
  "botClusterConnectionString": "Host=localhost;Database=orleans;Username=user;Password=pass",
  "botClusterInvariant": "Npgsql"
}
```

**Подготовка базы данных:**

1. Установите пакет Orleans SQL скриптов
2. Выполните скрипты создания таблиц для PostgreSQL
3. Настройте connection string

### Кастомное хранилище

Вы можете использовать любое хранилище, поддерживаемое Orleans (SQL Server, Azure Table Storage, Redis и т.д.). Измените конфигурацию в `TebotHostBuilder.cs`.

---

## Обработка событий

### Переопределяемые методы

Класс `Bot<TImplementation, TState>` предоставляет виртуальные методы для обработки различных типов обновлений:

```csharp
public class MyBot : Bot<MyBot, MyBotState>
{
    protected override async Task OnUpdateReceived(Update update)
    {
        // Вызывается для каждого обновления
        Logger?.LogInformation($"Получено обновление типа {update.Type}");
    }

    protected override async Task OnMessageReceived(Message message)
    {
        // Вызывается для каждого сообщения
        Data.LastVisit = DateTime.UtcNow;
        await SaveAsync();
    }

    protected override async Task OnInlineQueryRequest(InlineQuery inlineQuery)
    {
        // Обработка inline-запросов
        var results = new List<InlineQueryResult>
        {
            new InlineQueryResultArticle(
                id: "1",
                title: "Результат",
                inputMessageContent: new InputTextMessageContent("Текст")
            )
        };
        await BotClient.AnswerInlineQuery(inlineQuery.Id, results);
    }

    protected override async Task OnInlineChosenResult(ChosenInlineResult result)
    {
        // Вызывается, когда пользователь выбрал inline-результат
        Logger?.LogInformation($"Выбран результат: {result.ResultId}");
    }
}
```

### Порядок вызова

При получении обновления методы вызываются в следующем порядке:

1. `OnUpdateReceived(Update)`
2. `OnMessageReceived(Message)` (если это сообщение)
3. `OnInlineQueryRequest(InlineQuery)` (если это inline-запрос)
4. `OnInlineChosenResult(ChosenInlineResult)` (если выбран inline-результат)
5. Проверка на команду → вызов метода с `[Command]`
6. Если не команда → вызов метода с `[State]` для текущего состояния

---

## API Reference

### TebotConfig

```csharp
public record TebotConfig
{
    // Имя хранилища Orleans для grain storage
    public string StorageName { get; set; } = "bot-data";
    
    // Имя типа состояния для персистентности
    public string StateName { get; set; } = "my-state";
    
    // Аргументы командной строки
    public string[] ConsoleArguments { get; set; } = Array.Empty<string>();
    
    // Callback для настройки конфигурации
    public Action<ConfigurationManager>? ProcessConfigurationManager { get; set; } = null;
}
```

### Bot<TImplementation, TState>

#### Свойства

```csharp
protected ITelegramBotClient BotClient;           // Клиент Telegram Bot API
protected IPersistentState<TState>? State;        // Персистентное состояние
protected TState Data;                            // Данные состояния пользователя
protected ILogger<TImplementation>? Logger;       // Логгер
protected Update? currentUpdate;                  // Текущее обновление
protected long ChatId;                            // ID чата
protected string Text;                            // Текст сообщения
```

#### Методы

```csharp
// Переход к новому состоянию
protected Task GoToState(string state, bool perfomSaving = true);

// Сохранение состояния
protected Task SaveAsync();

// Очистка состояния
protected Task ClearStateAsync();

// Переопределяемые методы обработки событий
protected virtual Task OnMessageReceived(Message message);
protected virtual Task OnUpdateReceived(Update update);
protected virtual Task OnInlineQueryRequest(InlineQuery inlineQuery);
protected virtual Task OnInlineChosenResult(ChosenInlineResult result);
```

#### Статические свойства

```csharp
public static string DbStateName { get; set; }    // Имя состояния в БД
public static string DbStorageName { get; set; }  // Имя хранилища в БД
```

### Атрибуты

#### StateAttribute

```csharp
[State("state_name")]
public async Task MyState() { }
```

Помечает метод как обработчик состояния.

#### CommandAttribute

```csharp
[Command("/command", "Описание команды")]
public async Task MyCommand() { }
```

Помечает метод как обработчик команды. Описание используется для регистрации в Telegram.

---

## Примеры использования

### Пример 1: Простой эхо-бот

```csharp
public class EchoBotState : BotState { }

public class EchoBot : Bot<EchoBot, EchoBotState>
{
    [State("start")]
    public async Task Echo()
    {
        await BotClient.SendMessage(ChatId, $"Вы написали: {Text}");
    }

    [Command("/start", "Начать")]
    public async Task Start()
    {
        await BotClient.SendMessage(ChatId, "Отправьте мне любое сообщение!");
    }
}
```

### Пример 2: Счётчик с персистентностью

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
        await BotClient.SendMessage(ChatId, $"Вы написали мне {Data.Count} раз");
        await SaveAsync();
    }

    [Command("/reset", "Сбросить счётчик")]
    public async Task Reset()
    {
        Data.Count = 0;
        await BotClient.SendMessage(ChatId, "Счётчик сброшен");
        await SaveAsync();
    }
}
```

### Пример 3: Регистрация пользователя

```csharp
public class RegistrationState : BotState
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public bool IsRegistered { get; set; } = false;
}

public class RegistrationBot : Bot<RegistrationBot, RegistrationState>
{
    [Command("/start", "Начать регистрацию")]
    public async Task Start()
    {
        if (Data.IsRegistered)
        {
            await BotClient.SendMessage(ChatId, $"Вы уже зарегистрированы как {Data.Name}, {Data.Age} лет");
            return;
        }

        await BotClient.SendMessage(ChatId, "Как вас зовут?");
        await GoToState("wait_name");
    }

    [State("wait_name")]
    public async Task WaitName()
    {
        Data.Name = Text;
        await BotClient.SendMessage(ChatId, "Сколько вам лет?");
        await GoToState("wait_age");
    }

    [State("wait_age")]
    public async Task WaitAge()
    {
        if (int.TryParse(Text, out int age))
        {
            Data.Age = age;
            Data.IsRegistered = true;
            await BotClient.SendMessage(ChatId, $"Регистрация завершена!\nИмя: {Data.Name}\nВозраст: {Data.Age}");
            await GoToState("start");
        }
        else
        {
            await BotClient.SendMessage(ChatId, "Пожалуйста, введите число");
        }
    }

    [State("start")]
    public async Task MainMenu()
    {
        await BotClient.SendMessage(ChatId, $"Привет, {Data.Name}! Чем могу помочь?");
    }
}
```

### Пример 4: Inline-клавиатура

```csharp
public class MenuBot : Bot<MenuBot, BotState>
{
    [Command("/start", "Главное меню")]
    public async Task Start()
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Кнопка 1", "btn1"),
                InlineKeyboardButton.WithCallbackData("Кнопка 2", "btn2")
            }
        });

        await BotClient.SendMessage(ChatId, "Выберите действие:", replyMarkup: keyboard);
    }

    protected override async Task OnUpdateReceived(Update update)
    {
        if (update.CallbackQuery != null)
        {
            var data = update.CallbackQuery.Data;
            await BotClient.AnswerCallbackQuery(update.CallbackQuery.Id);
            await BotClient.SendMessage(ChatId, $"Вы нажали: {data}");
        }
    }
}
```

### Пример 5: Логирование и обработка ошибок

```csharp
public class RobustBot : Bot<RobustBot, BotState>
{
    [State("start")]
    public async Task Start()
    {
        try
        {
            Logger?.LogInformation($"Пользователь {ChatId} отправил: {Text}");
            await BotClient.SendMessage(ChatId, "Обработано успешно");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Ошибка обработки сообщения");
            await BotClient.SendMessage(ChatId, "Произошла ошибка. Попробуйте позже.");
        }
    }

    protected override async Task OnUpdateReceived(Update update)
    {
        Logger?.LogDebug($"Получено обновление: {update.Type}");
    }
}
```

---

## Лучшие практики

### 1. Всегда сохраняйте состояние

```csharp
// Плохо
Data.Counter++;

// Хорошо
Data.Counter++;
await SaveAsync();
```

### 2. Используйте try-catch для критичных операций

```csharp
[State("payment")]
public async Task ProcessPayment()
{
    try
    {
        // Критичная логика
    }
    catch (Exception ex)
    {
        Logger?.LogError(ex, "Ошибка платежа");
        await BotClient.SendMessage(ChatId, "Ошибка обработки платежа");
    }
}
```

### 3. Не храните секреты в config.json

Используйте переменные окружения или Azure Key Vault:

```csharp
ProcessConfigurationManager = (manager) => {
    manager.AddJsonFile("config.json");
    manager.AddEnvironmentVariables();
}
```

### 4. Используйте логирование

```csharp
Logger?.LogInformation($"Пользователь {ChatId} перешёл в состояние {Data.State}");
```

### 5. Валидируйте пользовательский ввод

```csharp
[State("wait_number")]
public async Task WaitNumber()
{
    if (!int.TryParse(Text, out int number))
    {
        await BotClient.SendMessage(ChatId, "Пожалуйста, введите число");
        return;
    }
    
    // Обработка числа
}
```

---

## Troubleshooting

### Бот не отвечает

1. Проверьте токен в `config.json`
2. Убедитесь, что бот запущен (`app.Build().Run()`)
3. Проверьте логи на наличие ошибок

### Состояние не сохраняется

1. Убедитесь, что вызываете `await SaveAsync()`
2. Проверьте настройки хранилища в `config.json`
3. Для PostgreSQL убедитесь, что таблицы созданы

### Orleans ошибки кластеризации

1. Проверьте `botClusterId` и `botServiceId` — они должны быть уникальными
2. Для PostgreSQL проверьте connection string
3. Убедитесь, что таблицы Orleans созданы в БД

### Команды не регистрируются в Telegram

1. Команды регистрируются при первом запуске бота
2. Убедитесь, что у команды есть описание: `[Command("/cmd", "описание")]`
3. Перезапустите бота и проверьте меню команд в Telegram

---

## Дополнительные ресурсы

- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Telegram.Bot библиотека](https://github.com/TelegramBots/Telegram.Bot)
- [Microsoft Orleans документация](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [Orleans Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/)

---

## Лицензия

См. файл LICENSE в корне репозитория.
