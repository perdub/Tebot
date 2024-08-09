using Telegram.Bot;
using System.Reflection;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Tebot;

public class Tebot: IDisposable, IUpdateHandler
{
    private ITelegramBotClient _client;

    private Dictionary<string, MethodInfo> _implementations;

    private Dictionary<long, Base> _userStates = new Dictionary<long, Base>();

    private string _startState;
    private Type stateImplementation;

    public Tebot(string token, Type stateImplementation, string startState="/start", HttpClient httpClient = null)
    {
        if(!stateImplementation.IsClass){
            throw new ArgumentException("statesImplementations should be a class.");
        }
        if(string.IsNullOrEmpty(token)){
            throw new NullReferenceException("token can`t be a null or empty");
        }

        _implementations = new Dictionary<string, MethodInfo>();
        _client = new TelegramBotClient(token, null);

        _startState = startState;
        this.stateImplementation = stateImplementation;

        parseMethods(stateImplementation);

        _client.StartReceiving(this);
    }

    private void parseMethods(Type type){
        //get all methods in type
        var methods = type.GetMethods();
        foreach(var method in methods){
            //get StateIdAttribute in each method
            var att = method.GetCustomAttribute(typeof(StateIdAttribute));
            if(att != null){
                //cast to StateId and save value with MethodInfo
                var state = (StateIdAttribute)att;
                if(string.IsNullOrWhiteSpace(state.State)){
                    throw new NullReferenceException("State value can`t be null or empty.");
                }
                _implementations.Add(state.State, method);
            }
        }
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
        if(update.Message == null){
            return;
        }

        var id = update.Message.Chat.Id;
        Base handler;
        bool isExsist = _userStates.TryGetValue(id, out handler);
        if(!isExsist){
            //create
            var instance = Activator.CreateInstance(stateImplementation);
            if(instance == null)
                throw new Exception("Something went wrong");
            //cast and set values
            handler = (Base)instance;
            handler.Bot = _client;
            handler.UserId = id;
            //add to dict
            _userStates[id] = handler;
        }

        //add some shit
        handler.Update = update;

        //check and invoke
        var method = _implementations[handler.NextState];
        if(method == null){
            throw new NotImplementedException($"Member with state {handler.NextState} are not found.");
        }
        method.Invoke(handler, null);

        
    }

    
}