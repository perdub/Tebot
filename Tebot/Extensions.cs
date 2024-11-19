using System;
using Telegram.Bot.Types.ReplyMarkups;

public static class Extensions
{
    public static IReplyMarkup OneTimeKeyboard(this IReplyMarkup keyboard, bool isOneTime = true)
    {
        if (keyboard is ReplyKeyboardMarkup replyKeyboardMarkup)
        {
            replyKeyboardMarkup.OneTimeKeyboard = isOneTime;
            return replyKeyboardMarkup;
        }
        throw new Exception("Not ReplayKeyboardMarkup.");
    }
    public static IReplyMarkup OneTimeKeyboard(this IReplyMarkup keyboard){
        return OneTimeKeyboard(keyboard, true);
    }
}