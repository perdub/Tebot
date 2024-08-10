using Telegram.Bot;
using Telegram.Bot.Types;

namespace Tebot;

public abstract class Base{
    //this two propertys are change after each call
    public string NextState {get;set;} = "/start";
    public Update Update {get; internal set;}

    //this props are const
    public ITelegramBotClient Bot {get;internal set;}
    public long UserId{get; internal set;}

    /// <summary>
    /// Method which helps you get user input as int32
    /// </summary>
    /// <returns>Number which user send, null if fall to parse</returns>
    public int? tryToParseInt(){
        int res;
        if(!int.TryParse(Update.Message.Text, out res)){
            return null;
        }
        return res;
    }
}

public abstract class CallbackBase : Base
{
    public CallbackManager CallbackManager {get;set;}
    /// <summary>
    /// please remember about AnswerCallbackQueryAsync, you should call it yourself
    /// </summary>
    /// <param name="callbackQuery">Info about callback</param>
    public abstract void OnCallback(CallbackQuery callbackQuery);
}