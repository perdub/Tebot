using Tebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class States : CallbackBase{
    [StateId(State = "/start")]
    public async Task HelloWord(){
        var k = new InlineKeyboardMarkup().AddButton("Случайное число...", "a");
        await Bot.SendTextMessageAsync(UserId, "ЖМИ! ЖМИ! ЖМИ!", replyMarkup:k);
        NextState = "/start";
    }

    public override async void OnCallback(CallbackQuery callbackQuery)
    {
        await Bot.AnswerCallbackQueryAsync(callbackQuery.Id, (Random.Shared.NextInt64()-Random.Shared.NextInt64()).ToString(), true);
    }
}