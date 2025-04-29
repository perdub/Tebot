using Tebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TebotConsole;

public class StateMachine : Base{
    [StateId(State = "/start")]
    public async Task HelloWord(){
        await Bot.SendMessage(UserId, "Hello world");
        NextState = "Bye";
    }
    [StateId(State = "Bye")]
    public async Task ByeWord(){
        await Bot.SendMessage(UserId, "Bye world");
        NextState = "/start";
    }
}

public class CallbackStateMachine : Base{

    [StateId(State = "/start")]
    public async Task HelloWord(){
        var k = new InlineKeyboardMarkup().AddButton("a", "a").AddButton("b", "b");
        await Bot.SendMessage(UserId, "Hello world", replyMarkup:k);
        NextState = "Bye";
    }
    [StateId(State = "Bye")]
    public async Task ByeWord(){
        await Bot.SendMessage(UserId, "Bye world", replyMarkup: new ReplyKeyboardRemove());
        NextState = "/start";
    }

    public override async Task OnCallback(CallbackQuery callbackQuery)
    {
        await Bot.AnswerCallbackQuery(callbackQuery.Id, "БОЛЬШИЕ ПИСЬКИ ААААААА!!!!", true);

    }
}