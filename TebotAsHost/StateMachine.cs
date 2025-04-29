using System.Text.Json;
using Tebot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

public class States : Base{
    [StateId(State = "/start")]
    public async Task HelloWord(){
        var k = new InlineKeyboardMarkup().AddButton("Случайное число...", "a");
        //await Task.Delay(350);
        await Bot.SendMessage(UserId, $"ЖМИ! ЖМИ! ЖМИ!\n\n{Update.Id} ", replyMarkup:k);
        await Bot.SendInvoice(UserId, "заголовок", "на презервативы.", Random.Shared.NextInt64().ToString(),  "XTR", new LabeledPrice[]{new LabeledPrice("label", 100)}, startParameter:"iwantittoo");
        
    }

    public override async Task OnCallback(CallbackQuery callbackQuery)
    {
        await Bot.SendMessage(UserId, "Тебе тоже)");
        await Bot.AnswerCallbackQuery(callbackQuery.Id, (Random.Shared.NextInt64()-Random.Shared.NextInt64()).ToString(), true);
    }

    public override async Task OnCommand(string command)
    {
        await Bot.SendMessage(UserId, $"Команда: {command}");
    }

    public override async Task OnUpdate(Update update)
    {
        if(update.Type == Telegram.Bot.Types.Enums.UpdateType.PreCheckoutQuery){
            await Bot.SendMessage(UserId, JsonSerializer.Serialize(update));
        }
    }
    [Command("/bebra")]
    public async Task Bebrachka(int abb = 150){
        await Bot.SendMessage(UserId, $"БЕБРАЧКУ ПОНЮХАЙ x{abb}");
    }
}