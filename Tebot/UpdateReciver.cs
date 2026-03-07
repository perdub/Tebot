using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tebot.Grains;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Tebot
{
    internal class UpdateReciver(IGrainFactory grainFactory, ITelegramBotClient telegramBotClient, ILogger<UpdateReciver> logger) : IHostedService, IUpdateHandler
    {
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Handle error.");
            return Task.CompletedTask;
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var grain = grainFactory.GetGrain<IBotGrain>(Helper.GetUpdateId(update));
            grain.SendUpdate(update);

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            telegramBotClient.StartReceiving(this);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
