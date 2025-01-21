using Microsoft.Extensions.Hosting;
using Tebot;

var builder = TebotHostBuilder.CreateBotApplication();
var host = builder.Build();
host.Run();