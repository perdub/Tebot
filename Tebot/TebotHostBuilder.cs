using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tebot.Grains;
using Tebot.Model;
using Telegram.Bot;

namespace Tebot
{
    public class TebotHostBuilder
    {
        public static HostApplicationBuilder Build
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TIntstance,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TState>
            (TebotConfig tebotConfig) 
            where TIntstance : Bot<TIntstance, TState>
            where TState : BotState, new()
        {
            var app = Host.CreateApplicationBuilder();

            app.Configuration.AddEnvironmentVariables();
            app.Configuration.AddCommandLine(tebotConfig.ConsoleArguments);
            if(tebotConfig.ProcessConfigurationManager is not null)
            {
                tebotConfig.ProcessConfigurationManager(app.Configuration);
            }

            app.Services.AddSingleton<ITelegramBotClient>(static (prov) =>
            {
                var config = prov.GetRequiredService<IConfiguration>();
                var clientOptions = new TelegramBotClientOptions(
                    config.GetValue<string>("botToken")!,
                    config.GetValue<string>("botBaseUrl"));

                var b = new TelegramBotClient(clientOptions);

                return b;
            });

            Bot<TIntstance, TState>.StateName = tebotConfig.StateName;
            Bot<TIntstance, TState>.StorageName = tebotConfig.StorageName;

            app.UseOrleans((builder) => {
                {
                    var storageType = builder.Configuration.GetValue<string>("dataStorage");
                    if (storageType == "memory")
                    {
                        builder.UseLocalhostClustering();
                        builder.AddMemoryGrainStorage(tebotConfig.StorageName);
                    }
                    else
                    {
                        builder.UseAdoNetClustering((options) =>
                        {
                            options.ConnectionString = builder.Configuration.GetValue<string>("botClusterConnectionString");
                            options.Invariant = builder.Configuration.GetValue<string>("botClusterInvariant");
                        });

                        builder.AddAdoNetGrainStorage(tebotConfig.StorageName, options =>
                        {
                            options.ConnectionString = builder.Configuration.GetValue<string>("botClusterConnectionString");
                            options.Invariant = builder.Configuration.GetValue<string>("botClusterInvariant");
                        });
                    }

                    builder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = builder.Configuration.GetValue<string>("botClusterId");
                        options.ServiceId = builder.Configuration.GetValue<string>("botServiceId");
                    });
                }
            });

            app.Services.AddHostedService<UpdateReciver>();

            return app;
        }

        

    }
}
