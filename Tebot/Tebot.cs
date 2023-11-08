using Telegram.Bot;
using System.Reflection;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Tebot;

public class Tebot : IDisposable, IUpdateHandler
{
    private ITelegramBotClient _client;

    private Dictionary<string, MethodInfo> _implementations;

    private Dictionary<long, string> _userStates;

    private string _startState;

    public Tebot(string token, Type[] statesImplementations, string startState)
    {
        _client = new TelegramBotClient(token);

        _client.StartReceiving(this, new ReceiverOptions{}, CancellationToken.None);
        
        _implementations = new Dictionary<string, MethodInfo>();
        _userStates = new Dictionary<long, string>();

        _startState = startState;

        ProcessStatesImplementations(statesImplementations);
    }

    private void ProcessStatesImplementations(Type[] implementations){
        foreach (var item in implementations)
        {
            var methods = item.GetMethods();
            foreach (var method in methods)
            {
                var customAttributes = method.GetCustomAttributes(typeof(StateIdAttribute), false);
                foreach (var att in customAttributes)
                {
                    var a = (StateIdAttribute)att;
                    _implementations.Add(a.State, method);
                }
            }
        }
    }

    public void CallMethod(string path, Update update){
        var method = _implementations[path];
        var _class = Activator.CreateInstance(method.ReflectedType);

        var baseClass = (Base)_class;
        baseClass.Bot = _client;
        baseClass.Update = update;
        baseClass.UserId = update.Message.Chat.Id;

        method.Invoke(_class, null);

        _userStates[baseClass.UserId] = baseClass.NextState;
    }

    public void Dispose()
    {
        
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if(update.Message is null)
            return;

        long userId = update.Message.Chat.Id;
        string? nowState = null;
        if(_userStates.TryGetValue(userId, out nowState)){
            CallMethod(nowState, update);
        }
        else{
            CallMethod(_startState, update);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw exception;
    }
}