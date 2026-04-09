using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Tebot;
using Tebot.Attributes;
using Tebot.Grains;
using Tebot.Model;
using Telegram.Bot;

var app = TebotHostBuilder.Build<MyBot, MyBotState>(new Tebot.Model.TebotConfig
{
    ConsoleArguments = args,
    StorageName = "my-test-storage",
    StateName = "my-test-type",
    ProcessConfigurationManager = (manager) => {
        manager.AddJsonFile("config.json");
    }
});

app.Build().Run();


public class MyBotState : BotState
{
    public int Be = 42;
}
public class MyBot : Bot<MyBot, MyBotState>
{
    [State("start")]
    public async Task Start()
    {
        Data.Be++;
        await BotClient.SendMessage(ChatId, $"pong{Data.Be}");

        if(Data.Be > 1000)
        {
            await GoToState("вшколе", false);
        }

        await SaveAsync();
    }

    [State("вшколе")]
    public async Task Stop() {
        await BotClient.SendMessage(ChatId, "Рё-чан в школе!");
        await GoToState("дома");
    }

    [State("дома")]
    public async Task дома()
    {
        await BotClient.SendMessage(ChatId, "Рё-чан дома!");
        await GoToState("вшколе");
    }

    [Command("/mul10", "Умножить на 10")]
    public async Task Mul()
    {
        Data.Be *= 10;
        await SaveAsync();
    }
}