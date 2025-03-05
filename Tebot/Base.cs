using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Threading.Tasks;

namespace Tebot
{

    public abstract class Base
    {
        //this two propertys are change after each call
        public string NextState { get; set; } = "/start";
        public Update Update { get; internal set; }

        //this props are const
        public ITelegramBotClient Bot { get; internal set; }
        public long UserId { get; internal set; }

        public Tebot Tebot {get; internal set;}

        /// <summary>
        /// method which allows to get other Base class instance
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Base GetOtherInstance(long id)
        {
            return Tebot.GetRepresentationById(id);
        }

        public string Text
        {
            get
            {
                return Update.Message.Text;
            }
        }

        private bool _isLoaded = false;

        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
            internal set
            {
                _isLoaded = value;
            }
        }

        public User BotInfo{
            get{
                return Tebot.BotInfo;
            }
        }

        public string BotUsername{
            get{
                return BotInfo.Username;
            }
        }

        /// <summary>
        /// this method calls when exceptions happends at state method
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public virtual Task OnException(Exception exception)
        {
            throw exception;
        }

        /// <summary>
        /// Method which helps you get user input as int32
        /// </summary>
        /// <returns>Number which user send, null if fall to parse</returns>
        public int? tryToParseInt()
        {
            int res;
            if (!int.TryParse(Update.Message.Text, out res))
            {
                return null;
            }
            return res;
        }

        /// <summary>
        /// you should override this method if you want to process possible unknnown states
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual async Task ProccessUnknownState(string State)
        {
            throw new NotImplementedException($"Member with state {State} are not found.");
        }

        /// <summary>
        /// call after each update with this user
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public virtual Task OnUpdate(Update update)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// call if user input starts with / (for example: /start /help /clear)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public virtual Task OnCommand(string command)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// method to generate keyboard
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected IReplyMarkup fastKeyboardBuilder(params string[] values)
        {
            var km = new ReplyKeyboardMarkup(true);
            foreach (var v in values)
            {
                km.AddButton(v);
            }
            return km;
        }
        
        /// <summary>
        /// clear keyboard
        /// </summary>
        /// <returns></returns>
        protected IReplyMarkup clearKeyboard()
        {
            return new ReplyKeyboardRemove();
        }
        /// <summary>
        /// method witch called before Tebot instance will be stopped
        /// </summary>
        public virtual void BeforeStop()
        {

        }

        /// <summary>
        /// call when Tebot recive new update for this user and user not use bot yet. for example, you can load some data from database
        /// </summary>
        /// <param name="id">chat new id</param>
        /// <returns></returns>
        public virtual Task OnCreate(long id)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// please remember about AnswerCallbackQueryAsync, you should call it yourself
        /// </summary>
        /// <param name="callbackQuery">Info about callback</param>
        public virtual Task OnCallback(CallbackQuery callbackQuery)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// recive all inline querys from user
        /// </summary>
        /// <param name="inlineQuery"></param>
        /// <returns></returns>
        public virtual Task OnInlineQuery(InlineQuery inlineQuery)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// event when this Base instance create to load exsist client, for example, get client state from database
        /// </summary>
        /// <param name="id">Chat id</param>
        /// <returns></returns>
        public virtual Task OnLoad(long id)
        {
            return Task.CompletedTask;
        }
    }
}