using Microsoft.Extensions.Hosting;
using Tebot;

var builder = TebotHostBuilder.CreateBotApplication(baseSelector: new MySelector());
var host = builder.Build();
host.Run();

public class MySelector : IBaseSelector
{
    public Type SelectType(long id)
    {
        if (id > 1_000_000_000)
            return typeof(States);
        return typeof(States2);
    }
}