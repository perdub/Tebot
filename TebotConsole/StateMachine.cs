using Tebot;
using Telegram.Bot;

namespace TebotConsole;

public class StateMachine : Base{
    [StateId(State = "Hello")]
    public async Task HelloWord(){
        await Bot.SendTextMessageAsync(UserId, "Hello world");
        NextState = "Bye";
    }
    [StateId(State = "Bye")]
    public async Task ByeWord(){
        await Bot.SendTextMessageAsync(UserId, "Bye world");
        NextState = "Hello";
    }
}