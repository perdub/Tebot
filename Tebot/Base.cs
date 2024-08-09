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

    public int? tryToParseInt(){
        int res;
        if(!int.TryParse(Update.Message.Text, out res)){
            return null;
        }
        return res;
    }
}