using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tebot;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddJsonFile("config.json");
builder.Services.AddHostedService<Tebot.Tebot>((provider)=>{
    return new Tebot.Tebot(builder.Configuration.GetValue<string>("token"), typeof(States), serviceProvider: provider);
});
var host = builder.Build();
host.Run();