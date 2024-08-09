using Tebot;

namespace TebotConsole;

class Program
{
    static void Main(string[] args)
    {
        var tb = new Tebot.Tebot("6510109654:AAGCKkSYDlyS6GVU-Y4UFZu3oWAw8rgGNLI", typeof(StateMachine));
        Console.ReadKey();
    }
}
