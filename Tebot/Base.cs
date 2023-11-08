using Telegram.Bot;
using Telegram.Bot.Types;

namespace Tebot;

public class Base{
    public string NextState {get;set;}
    public ITelegramBotClient Bot {get;internal set;}
    public Update Update {get; internal set;}
    public long UserId{get; internal set;}
}