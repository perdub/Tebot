using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Tebot
{
    //smells like shit
    internal static class Helper
    {
        internal static long GetUpdateId(Update update)
        {
            if (update.Message != null)
            {
                return update.Message.Chat.Id;
            }
            if (update.PreCheckoutQuery != null)
            {
                return update.PreCheckoutQuery.From.Id;
            }

            if (update.MyChatMember != null)
            {
                return update.MyChatMember.From.Id;
            }
            if (update.ChannelPost != null)
            {
                return update.ChannelPost.Chat.Id;
            }
            if (update.EditedChannelPost != null)
            {
                return update.EditedChannelPost.Chat.Id;
            }
            if (update.CallbackQuery != null)
            {
                return update.CallbackQuery.Message.Chat.Id;
            }
            if (update.InlineQuery != null)
            {
                return update.InlineQuery.From.Id;
            }
            if (update.EditedMessage != null)
            {
                return update.EditedMessage.Chat.Id;
            }
            if (update.ChosenInlineResult != null)
            {
                return update.ChosenInlineResult.From.Id;
            }

            if (update.RemovedChatBoost != null)
            {
                return update.RemovedChatBoost.Chat.Id;
            }

            if (update.ChatMember != null)
            {
                return update.ChatMember.Chat.Id;
            }
            if (update.MessageReactionCount != null)
            {
                return update.MessageReactionCount.Chat.Id;
            }
            if (update.MessageReaction != null)
            {
                return update.MessageReaction.Chat.Id;
            }
            //add more in future

            throw new Exception($"fall to parse user id. Json represitaion of update: {System.Text.Json.JsonSerializer.Serialize(update)}");
        }
    }
    }
}
