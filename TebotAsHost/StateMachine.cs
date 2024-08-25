using System.Text.Json;
using Tebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

public class States : CallbackBase{
    [StateId(State = "/start")]
    public async Task HelloWord(){
        var k = new InlineKeyboardMarkup().AddButton("Случайное число...", "a");
        await Bot.SendTextMessageAsync(UserId, "ЖМИ! ЖМИ! ЖМИ!", replyMarkup:k);
        await Bot.SendInvoiceAsync(UserId, "заголовок", "на презервативы.", Random.Shared.NextInt64().ToString(), null, "XTR", new LabeledPrice[]{new LabeledPrice("label", 100)}, startParameter:"iwantittoo");
        NextState = "/start";
    }

    public override async void OnCallback(CallbackQuery callbackQuery)
    {
        await Bot.SendTextMessageAsync(UserId, "Тебе тоже)");
        await Bot.AnswerCallbackQueryAsync(callbackQuery.Id, (Random.Shared.NextInt64()-Random.Shared.NextInt64()).ToString(), true);
    }

    public override async Task OnCommand(string command)
    {
        await Bot.SendTextMessageAsync(UserId, $"Команда: {command}");
    }

    public override async Task OnUpdate(Update update)
    {
        if(update.Type == Telegram.Bot.Types.Enums.UpdateType.PreCheckoutQuery){
            await Bot.SendTextMessageAsync(UserId, JsonSerializer.Serialize(update));
        }
    }
}