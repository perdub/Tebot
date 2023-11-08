using Tebot;

namespace TebotConsole;

class Program
{
    static void Main(string[] args)
    {
        var tb = new Tebot.Tebot("YOUR_TOKEN", new []{typeof(StateMachine)}, "Hello");
        Console.ReadKey();
    }
}
