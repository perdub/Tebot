using Telegram.Bot;
using System.Reflection;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tebot;

public class Tebot : IDisposable, IUpdateHandler, IHostedService
{
    private ITelegramBotClient _client;
    private ILogger<Tebot>? _logger;
    private Dictionary<string, MethodInfo> _implementations;

    private Dictionary<long, Base> _userStates = new Dictionary<long, Base>();

    private string _startState;
    private Type _stateImplementation;
    private IServiceProvider _serviceProvider;

    private ConstructorInfo _preferedConstructor;
    private ParameterInfo[] _preferedConstructorParams;

    public ITelegramBotClient Client{
        get
        {
            return _client;
        }
    }

    public Tebot(string token, Type stateImplementation, string startState = "/start", HttpClient httpClient = null, IServiceProvider serviceProvider = null)
    {
        if (!stateImplementation.IsClass)
        {
            throw new ArgumentException("statesImplementations should be a class.");
        }
        if (string.IsNullOrEmpty(token))
        {
            throw new NullReferenceException("token can`t be a null or empty");
        }

        if (serviceProvider != null)
        {
            _logger = serviceProvider.GetService<ILogger<Tebot>>();
        }

        _implementations = new Dictionary<string, MethodInfo>();
        _client = new TelegramBotClient(token, null);
        
        _serviceProvider = serviceProvider;
        _startState = startState;
        this._stateImplementation = stateImplementation;

        parseMethods(stateImplementation);
        findConstructor(stateImplementation);
    }

    private void parseMethods(Type type)
    {
        //get all methods in type
        var methods = type.GetMethods();
        foreach (var method in methods)
        {
            //get StateIdAttribute in each method
            var att = method.GetCustomAttribute(typeof(StateIdAttribute));
            if (att != null)
            {
                //cast to StateId and save value with MethodInfo
                var state = (StateIdAttribute)att;
                if (string.IsNullOrWhiteSpace(state.State))
                {
                    throw new NullReferenceException("State value can`t be null or empty.");
                }
                _implementations.Add(state.State, method);
                _logger?.LogDebug("New state method: {} {}", method, state.State);
            }
        }
    }

    private void findConstructor(Type type)
    {
        var constructors = type.GetConstructors();
        ParameterInfo[] parameters = Array.Empty<ParameterInfo>();
        ConstructorInfo constructor = null;
        foreach (var constr in constructors)
        {
            var loc = constr.GetParameters();
            if (loc.Length > parameters.Length)
            {
                constructor = constr;
                parameters = loc;
            }
        }

        _preferedConstructor = constructor;
        _preferedConstructorParams = parameters;
    }
    private Base CreateInstance()
    {
        if (_serviceProvider is not null)
        {
            return (Base)ActivatorUtilities.CreateInstance(_serviceProvider, _stateImplementation);
        }
        return CreateBaseInstance();
    }
    private Base CreateBaseInstance()
    {
        object?[]? parameters = new object[_preferedConstructorParams.Length];
        if (_serviceProvider != null)
        {
            for (int i = 0; i < _preferedConstructorParams.Length; i++)
            {
                var param = _preferedConstructorParams[i];
                Type paramType = param.ParameterType;
                parameters[i] = _serviceProvider.GetRequiredService(paramType);
            }
        }
        else
        {
            for (int i = 0; i < _preferedConstructorParams.Length; i++)
            {
                parameters[i] = null;
            }
        }
        return (Base)Activator.CreateInstance(_stateImplementation, parameters);
    }

    public void Dispose()
    {

    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await MessagesProcess(update);
    }

    private long tryToParseId(Update update)
    {
        if (update.Message is not null)
        {
            return update.Message.Chat.Id;
        }
        if (update.PreCheckoutQuery is not null)
        {
            return update.PreCheckoutQuery.From.Id;
        }

        if (update.MyChatMember is not null)
        {
            return update.MyChatMember.From.Id;
        }
        if (update.ChannelPost is not null)
        {
            return update.ChannelPost.Chat.Id;
        }
        if (update.EditedChannelPost is not null)
        {
            return update.EditedChannelPost.Chat.Id;
        }
        if (update.CallbackQuery is not null)
        {
            return update.CallbackQuery.From.Id;
        }
        if(update.InlineQuery is not null){
            return update.InlineQuery.From.Id;
        }
        //add more in future

        throw new Exception($"fall to parse user id. Json represitaion of update: {System.Text.Json.JsonSerializer.Serialize(update)}");
    }

    private async Task MessagesProcess(Update update)
    {
        try
        {
            var id = tryToParseId(update);
            Base handler;
            bool isExsist = _userStates.TryGetValue(id, out handler);
            _logger?.LogDebug("MessageProcess: id{}, isExsist-{}", id, isExsist);
            if (!isExsist)
            {
                //create
                var instance = CreateInstance();
                if (instance == null)
                    throw new Exception("Something went wrong");
                //cast and set values
                handler = (Base)instance;
                handler.Bot = _client;
                handler.UserId = id;
                await handler.OnCreate(id);
                //add to dict
                _userStates[id] = handler;
            }

            //add some shit
            handler.Update = update;

            try
            {
                await handler.OnUpdate(update);

                if (update.Message?.Text is not null && update.Message.Text.StartsWith('/'))
                {
                    await handler.OnCommand(update.Message.Text);
                }

                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    await handler.OnCallback(update.CallbackQuery);
                    return;
                }

                if(update.Type == Telegram.Bot.Types.Enums.UpdateType.InlineQuery){
                    await handler.OnInlineQuery(update.InlineQuery);
                    return;
                }

                //check and invoke
                var method = _implementations[handler.NextState];
                if (method == null)
                {
                    await handler.ProccessUnknownState(handler.NextState);
                    return;
                }
                method.Invoke(handler, null);

            }
            catch (Exception e)
            {
                await handler.OnException(e);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception: {e.Message} {e.InnerException} {e.StackTrace}");
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        if (source == HandleErrorSource.PollingError)
        {
            //ignore piece of shit(telegram shitty servers)
            return;
        }
        _logger?.LogError("Fatal error: {}", exception);
        Console.WriteLine($"{exception.Message} {exception.Source} {exception.StackTrace} {exception.InnerException}");
    }



    private async Task run(CancellationToken cancellationToken)
    {
        _client.ReceiveAsync(this, cancellationToken: cancellationToken);
    }
    public async Task Stop()
    {
        _logger?.LogDebug("Tebot is stopping");
        //try to stop
        botStopToken.Cancel();
        _client = null;
        _implementations = null;
        _userStates = null;
    }

    public async Task Run()
    {
        botStopToken = new CancellationTokenSource();
        _logger?.LogDebug("Tebot is starting");
        await run(botStopToken.Token);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Run();
    }

    private CancellationTokenSource botStopToken;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Stop();
    }

    public IEnumerable<Base> IterateOverClients(){
        return _userStates.Select(a=> a.Value);
    }
}