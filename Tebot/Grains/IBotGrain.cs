using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Tebot.Grains
{
    internal interface IBotGrain : IGrainWithIntegerKey
    {
        ValueTask SendUpdate(Update update);
    }
}
