using Telegram.Bot;
using System.Reflection;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Linq;
using System.Collections.Concurrent;
using Telegram.Bot.Types.Enums;
using System.Threading.Channels;

namespace Tebot
{

    public class Tebot : IDisposable, IUpdateHandler, IHostedService, IBaseSelector
    {
        private ITelegramBotClient _client;

        private ILogger<Tebot>? _logger;

        /// <summary>
        /// словарь который содержит префиксы для состояний. так же использвется для различия какие типы были пропаршены а какие нет.
        /// </summary>
        private Dictionary<Type, string> prefixes = new Dictionary<Type, string>();

        private Dictionary<string, MethodInfo> _implementations;
        private Dictionary<string, MethodInfo> _commands;
        private List<BotCommand> _botCommands = new List<BotCommand>();
        private Dictionary<long, Base> _userStates = new Dictionary<long, Base>();

        private string _startState;
        private Type _stateImplementation;
        private IServiceProvider _serviceProvider;

        private StateLoader _stateLoader;

        private IBaseSelector baseSelector;
        private Type baseSelectorDefaultType;
        public ITelegramBotClient Client
        {
            get
            {
                return _client;
            }
        }

        private string _token = string.Empty;
        internal string Token{
            get{
                return _token;
            }
        }

        private User _thisBot = null;
        public User BotInfo{
            get{
                return _thisBot;
            }
        }

        private Channel<Update> updateChannel;

        private AutoResetEvent stopListenEvent = new AutoResetEvent(false);

        public Tebot SetBaseSelector(IBaseSelector baseSelector)
        {
            this.baseSelector = baseSelector;
            return this;
        }

        public Tebot(string token, Type stateImplementation, StateLoader stateLoader, string localApiUrl = null, string startState = "/start", HttpClient httpClient = null, IServiceProvider serviceProvider = null)
        {
            baseSelector = this;
            baseSelectorDefaultType = stateImplementation;
            if (string.IsNullOrEmpty(token))
            {
                throw new NullReferenceException("token can`t be a null or empty");
            }

            if (serviceProvider != null)
            {
                _logger = serviceProvider.GetService<ILogger<Tebot>>();
            }

            _token = token;

            _implementations = new Dictionary<string, MethodInfo>();
            _commands = new Dictionary<string, MethodInfo>();
            if (!string.IsNullOrEmpty(localApiUrl))
            {
                _logger.LogInformation("Instead connecting to api.telegram.org, we will be made request to local bot api server based on {0}", localApiUrl);
            }
            TelegramBotClientOptions telegramBotClientOptions = new TelegramBotClientOptions(token, localApiUrl);
            _client = new TelegramBotClient(telegramBotClientOptions, httpClient);

            var botInfo = _client.GetMe();
            botInfo.Wait();
            _thisBot = botInfo.Result;

            updateChannel = Channel.CreateBounded<Update>(
                new BoundedChannelOptions(1) 
                    { SingleReader = true, Capacity = 16*1024,FullMode=BoundedChannelFullMode.DropOldest },
                (update) => { this._logger.LogWarning("Update was dropped from channel. Update id: {0}", update.Id); }
            );

            _logger.LogInformation($"Tebot instanse starter:\n\tBot: @{_thisBot.Username} - {_thisBot.Id} - https://t.me/{_thisBot.Username}");

            _serviceProvider = serviceProvider;
            _startState = startState;
            _stateLoader = stateLoader;
            this._stateImplementation = stateImplementation;

            var commandTask = setMyCommands();
            commandTask.Wait();
        }


        private static UpdateType[] AllowedUpdates = new UpdateType[]{
            UpdateType.Unknown, UpdateType.Message, UpdateType.InlineQuery, UpdateType.ChosenInlineResult, UpdateType.CallbackQuery, UpdateType.EditedMessage, UpdateType.ChannelPost,
            UpdateType.EditedChannelPost, UpdateType.ShippingQuery, UpdateType.PreCheckoutQuery, UpdateType.Poll, UpdateType.PollAnswer, UpdateType.MyChatMember, UpdateType.ChatMember,
            UpdateType.ChatJoinRequest, UpdateType.MessageReaction, UpdateType.MessageReactionCount, UpdateType.ChatBoost, UpdateType.RemovedChatBoost, UpdateType.BusinessConnection,
            UpdateType.BusinessMessage, UpdateType.EditedBusinessMessage, UpdateType.DeletedBusinessMessages, UpdateType.PurchasedPaidMedia
        };
        private async Task getUpdates(object obj)
        {
            int offset = 0;
            while(!stopListenEvent.WaitOne(30)){
                Update[]? updates = null;

                try{
                    updates = await _client.GetUpdates(offset, timeout: 40, allowedUpdates: AllowedUpdates);
                }
                catch(Exception e){
                    _logger?.LogError("Fall to GetUpdates: {0}", e);
                }
                if(updates == null){
                    continue;
                }

                foreach (var upd in updates) {
                    offset = upd.Id + 1;
                    await updateChannel.Writer.WriteAsync(upd);              
                }

                
            }
        }

        private async Task ProcessUpdates(){
            await foreach (var upd in updateChannel.Reader.ReadAllAsync())
            {
                await this.MessagesProcess(upd);              
            }
        }

        private BehaviourAfterCommand GetBehaviour(MethodInfo methodInfo){
            var comm = methodInfo.GetCustomAttribute<CommandAttribute>();
            return comm.Behaviour;
        }
        private InvokeMode getInvokeAttributeValue(MethodInfo methodInfo)
        {
            var comm = methodInfo.GetCustomAttribute<CommandAttribute>();
            if (comm != null)
            {
                return comm.InvokeMode;
            }
            var state = methodInfo.GetCustomAttribute<StateIdAttribute>();
            if (state != null)
            {
                return state.InvokeMode;
            }
            return InvokeMode.Sync;
        }

        private void parseMethods(Type type)
        {
            //get all methods in type
            var methods = type.GetMethods();
            string prefix = type.FullName;
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
                    _implementations.Add(prefix+state.State, method);
                    _logger?.LogDebug("New state method: {} {}", method, state.State);
                }

                //get CommandAtribute in each method
                var command = method.GetCustomAttribute(typeof(CommandAttribute));
                if (command != null)
                {
                    //cast to StateId and save value with MethodInfo
                    var comm = (CommandAttribute)command;
                    if (string.IsNullOrWhiteSpace(comm.Command))
                    {
                        throw new NullReferenceException("Command name can`t be null or empty.");
                    }

                    _commands.Add(prefix+comm.Command, method);
                    if(!string.IsNullOrWhiteSpace(comm.Description)){
                        _botCommands.Add(new BotCommand{
                            Command = comm.Command,
                            Description = comm.Description
                        });
                    }

                    _logger?.LogDebug("New command method: {} {}", method, comm.Command);
                }
            }
            prefixes.Add(type, prefix);
            setMyCommands();
        }
        private Base CreateInstance(long userId)
        {
            if (_serviceProvider != null)
            {
                var type = baseSelector.SelectType(userId);
                if (!prefixes.ContainsKey(type))
                {
                    parseMethods(type);
                }
                return (Base)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            }
            throw new NullReferenceException("ServiceProvider is null.");
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

        /// <summary>
        /// метод который пытается получить id юзера/чата с которым связанно обновление
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private long tryToParseId(Update update)
        {
            if (update.Message != null)
            {
                return update.Message.Chat.Id;
            }
            if (update.PreCheckoutQuery != null)
            {
                return update.PreCheckoutQuery.From.Id;
            }

            if (update.MyChatMember != null)
            {
                return update.MyChatMember.From.Id;
            }
            if (update.ChannelPost != null)
            {
                return update.ChannelPost.Chat.Id;
            }
            if (update.EditedChannelPost != null)
            {
                return update.EditedChannelPost.Chat.Id;
            }
            if (update.CallbackQuery != null)
            {
                return update.CallbackQuery.From.Id;
            }
            if (update.InlineQuery != null)
            {
                return update.InlineQuery.From.Id;
            }
            if (update.EditedMessage != null)
            {
                return update.EditedMessage.Chat.Id;
            }
            if(update.ChosenInlineResult != null){
                return update.ChosenInlineResult.From.Id;
            }

            if(update.RemovedChatBoost != null){
                return update.RemovedChatBoost.Chat.Id;
            }

            if(update.ChatMember != null){
                return update.ChatMember.Chat.Id;
            }
            if(update.MessageReactionCount != null){
                return update.MessageReactionCount.Chat.Id;
            }
            if(update.MessageReaction != null){
                return update.MessageReaction.Chat.Id;
            }
            //add more in future

            throw new Exception($"fall to parse user id. Json represitaion of update: {System.Text.Json.JsonSerializer.Serialize(update)}");
        }

        /// <summary>
        /// call for every Update
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        private async Task MessagesProcess(Update update)
        {
            try
            {   
                //пытаемся найти id и связанный инстанс
                var id = tryToParseId(update);
                Base handler;
                bool isExsist = _userStates.TryGetValue(id, out handler);
                _logger?.LogDebug("MessageProcess: id{}, isExsist-{}", id, isExsist);
                if (!isExsist)
                {
                    //create
                    var instance = CreateInstance(id);
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
                handler.Tebot = this;

                //вызываем методы инстанса
                try
                {
                    string prefix = this.prefixes[handler.GetType()];
                    //OnUpdate
                    await handler.OnUpdate(update);

                    //работа с командами
                    if (update.Message?.Text != null && update.Message.Text.StartsWith("/"))
                    {
                        var actualCommand = update.Message.Text.Split(' ').FirstOrDefault();


                        //попытка спарсить команду
                        if (actualCommand != null)
                        {
                            //если мы в групповом чате, команды могут быть отправленны как /split@MySuperBot, так что при нахождении вконце команды юзернейма текущего бота, отбрасываем его
                            if(actualCommand.EndsWith('@' + BotInfo.Username)){
                                actualCommand = actualCommand.Replace('@'+BotInfo.Username, "");
                            }

                        MethodInfo? commandMethod;
                            bool isSuss = _commands.TryGetValue(prefix+actualCommand, out commandMethod);
                            if (isSuss && commandMethod != null)
                            {
                                //todo: optimize this
                                var inv = getInvokeAttributeValue(commandMethod);
                                var behaviour = GetBehaviour(commandMethod);

                                var map = mapParams(commandMethod, update.Message.Text);
                                if (map.Item1)
                                {
                                    var res = commandMethod.Invoke(handler, map.Item2);
                                    if (res is Task tsk && tsk != null)
                                    {
                                        //добавим задачу для проверки того как завершается задача
                                        tsk = tsk.ContinueWith(a=>CheckTaskForFall(a));
                                        if(inv == InvokeMode.Sync){
                                            #if NETSTANDARD2_0
                                            tsk.Wait();
                                            #else
                                            await tsk.WaitAsync(CancellationToken.None);
                                            #endif

                                        }
                                        //если команда помечена аттрибутом бреак мы выходим для того что бы не передать выполнение в стейт
                                        if(behaviour == BehaviourAfterCommand.Break){
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        await handler.OnCommand(update.Message.Text);
                    }

                    //для каллбеков
                    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                    {
                        await handler.OnCallback(update.CallbackQuery);
                        return;
                    }

                    //для инлайн-запросов
                    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.InlineQuery)
                    {
                        await handler.OnInlineQuery(update.InlineQuery);
                        return;
                    }

                    //check and invoke
                    var method = _implementations[prefix+handler.NextState];
                    if (method == null)
                    {
                        await handler.ProccessUnknownState(handler.NextState);
                        return;
                    }
                    var invk = getInvokeAttributeValue(method);
                    var rslt = method.Invoke(handler, null);
                    if (rslt is Task tsk1 && tsk1 != null)
                    {
                        tsk1.ContinueWith(a=>CheckTaskForFall(a));
                        if(invk == InvokeMode.Sync){
                            tsk1.Wait();
                        }
                    }

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

        public Base GetRepresentationById(long id = -1)
        {
            bool isExsist = this._userStates.TryGetValue(id, out Base b);
            if (isExsist)
            {
                return b;
            }
            return null;
        }

        private void CheckTaskForFall(Task task){
            if(task.IsFaulted){
                var exception = task.Exception;
                this._logger.LogError(exception, "Error when invoke task:");
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

        private (bool, object?[]?) mapParams(MethodInfo method, string str)
        {
            var _params = method.GetParameters();
            var arr = str.Split(' ');
            if (_params.Length != arr.Length - 1)
            {
                _logger?.LogWarning($"{method.Name}() params count dosent match with count of parced params");
            }
            object?[] res = new object[_params.Length];
            for (int i = 0; i < _params.Length; i++)
            {
                var param = _params[i];
                //если размер массива введеных значений меньше чем количество параметров подставляем значение по умолчанию или null
                if (arr.Length <= i + 1)
                {
                    var deflt = param.DefaultValue;
                    res[i] = deflt;
                    continue;
                }

                if (param.ParameterType == typeof(System.Int32))
                {
                    res[i] = Convert.ToInt32(arr[i + 1]);
                }
                else if (param.ParameterType == typeof(System.Int64))
                {
                    res[i] = Convert.ToInt64(arr[i + 1]);
                }
                else if (param.ParameterType == typeof(System.Double))
                {
                    res[i] = Convert.ToDouble(arr[i + 1]);
                }
                else if (param.ParameterType == typeof(System.String))
                {
                    res[i] = arr[i + 1];
                }
                else
                {
                    return (false, null);
                }
            }
            return (true, res);
        }

        private void linkLoadUsers()
        {
            safeNullableLogDebug("Start add loaded users...");
            if (_stateLoader.Strategy == LoaderStrategy.None)
            {
                safeNullableLogDebug("LoaderStrategy is None, skip...");
                return;
            }
            var loaded = _stateLoader.asTuptes();
            long i = 0;
            foreach (var state in loaded)
            {
                i++;
                state.Item2.Bot = _client;
                _userStates.Add(state.Item1, state.Item2);
            }
            safeNullableLogInfo($"Load {i} client states.");
            safeNullableLogDebug("Add all loaded states in dictionary.");
        }



        private async Task setMyCommands(){
            await Client.SetMyCommands(
                _botCommands, new BotCommandScopeDefault()
            );
        }

        private async Task run(CancellationToken cancellationToken)
        {
            linkLoadUsers();
            //_client.ReceiveAsync(this, cancellationToken: cancellationToken);
/*
            updateReciverThread = new Thread(new ParameterizedThreadStart(getUpdates));
            updateReciverThread.Name = "Tebot Update Reciver";
            updateReciverThread.Start();*/

            Task.Factory.StartNew(
                async ()=>{await getUpdates(0);},
                TaskCreationOptions.LongRunning
            );
            Task.Factory.StartNew(
                async() => { await ProcessUpdates(); },
                TaskCreationOptions.LongRunning
            );
        }
        public async Task Stop()
        {
            _logger?.LogDebug("Tebot is stopping");
            //try to stop
            this.stopListenEvent.Set();
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

        public IEnumerable<Base> IterateOverClients()
        {
            return _userStates.Select(a => a.Value);
        }

        private void safeNullableLogDebug(string debug){
            if(_logger != null){
                _logger.LogDebug(debug);
            }
        }
        private void safeNullableLogInfo(string info){
            if(_logger != null){
                _logger.LogInformation(info);
            }
        }

        public Type SelectType(long id)
        {
            return baseSelectorDefaultType;
        }
    }
}