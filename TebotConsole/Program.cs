using Tebot;

namespace TebotConsole;

class Program
{
    static void Main(string[] args)
    {
        var tb = new Tebot.Tebot("6510000000:AAGCKkSY***", typeof(CallbackStateMachine), StateLoader.Empty());
        tb.Run();
        Console.ReadKey();
        tb.Stop();
    }
}
