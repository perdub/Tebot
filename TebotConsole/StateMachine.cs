using Tebot;
using Telegram.Bot;

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