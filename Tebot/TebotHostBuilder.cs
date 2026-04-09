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
    /// <summary>
    /// Builder class for configuring and creating a Tebot application with Orleans and Telegram Bot integration.
    /// </summary>
    public class TebotHostBuilder
    {
        /// <summary>
        /// Builds and configures a host application for a Telegram bot with Orleans grain storage.
        /// </summary>
        /// <typeparam name="TIntstance">The bot implementation type that inherits from <see cref="Bot{TImplementation, TState}"/>.</typeparam>
        /// <typeparam name="TState">The state type that inherits from <see cref="BotState"/>.</typeparam>
        /// <param name="tebotConfig">Configuration options for the bot.</param>
        /// <returns>A configured <see cref="HostApplicationBuilder"/> ready to build and run.</returns>
        /// <example>
        /// <code>
        /// var app = TebotHostBuilder.Build&lt;MyBot, MyBotState&gt;(new TebotConfig
        /// {
        ///     ConsoleArguments = args,
        ///     StorageName = "my-bot-storage",
        ///     StateName = "my-bot-state",
        ///     ProcessConfigurationManager = (manager) => {
        ///         manager.AddJsonFile("config.json");
        ///     }
        /// });
        /// app.Build().Run();
        /// </code>
        /// </example>
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

            Bot<TIntstance, TState>.DbStateName = tebotConfig.StateName;
            Bot<TIntstance, TState>.DbStorageName = tebotConfig.StorageName;

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
