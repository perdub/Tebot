using Tebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TebotConsole;

public class StateMachine : Base{
    [StateId(State = "/start")]
    public async Task HelloWord(){
        await Bot.SendTextMessageAsync(UserId, "Hello world");
        NextState = "Bye";
    }
    [StateId(State = "Bye")]
    public async Task ByeWord(){
        await Bot.SendTextMessageAsync(UserId, "Bye world");
        NextState = "/start";
    }
}

public class CallbackStateMachine : CallbackBase{

    [StateId(State = "/start")]
    public async Task HelloWord(){
        var k = new InlineKeyboardMarkup().AddButton("a", "a").AddButton("b", "b");
        await Bot.SendTextMessageAsync(UserId, "Hello world", replyMarkup:k);
        NextState = "Bye";
    }
    [StateId(State = "Bye")]
    public async Task ByeWord(){
        await Bot.SendTextMessageAsync(UserId, "Bye world", replyMarkup: new ReplyKeyboardRemove());
        NextState = "/start";
    }

    public override async void OnCallback(CallbackQuery callbackQuery)
    {
        await Bot.AnswerCallbackQueryAsync(callbackQuery.Id, "БОЛЬШИЕ ПИСЬКИ ААААААА!!!!", true);

    }
}