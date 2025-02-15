using System;
using Tebot;
using System.Linq;
using System.Net.Http;
using Telegram.Bot;
using System.IO;
using System.Threading.Tasks;

using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;

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

    /// <summary>
    /// This method are not safe, because its return link with bot token. 
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public static async Task<string> ExtractUrl(this Base client)
    {
        var Update = client.Update;
        var bot = client.Bot;
        var Tebot =client.Tebot;
        string url = $"https://api.telegram.org/file/bot{Tebot.Token}/";
        if(Update == null && Update.Type != Telegram.Bot.Types.Enums.UpdateType.Message){
            throw new NullReferenceException("Update are not found.");
        }
        var mess = Update.Message;
        string fileId = string.Empty;

        if(mess.Photo != null){
            var photos = mess.Photo;
            var photo = photos.OrderByDescending(a=> a.Height*a.Width).FirstOrDefault();
            if(photo != null){
                fileId = photo.FileId;
            }
        }
        else if(mess.Document != null){
            fileId = mess.Document.FileId;
        }
        else if(mess.Video != null){
            fileId = mess.Video.FileId;
        }
        else if(mess.Sticker != null){
            fileId = mess.Sticker.FileId;
        }
        //add other types process
        else{
            throw new Exception("file id are not found");
        }

        var fileTask = await bot.GetFile(fileId);
        url+=fileTask.FilePath;
        return url;
    }

    /// <summary>
    /// download item which we get in update. its can be photo, video, document and other
    /// </summary>
    /// <param name="base"></param>
    /// <param name="httpClient"></param>
    /// <returns></returns>
    public static async Task<Stream> DownloadItemFromUpdate(this Base @base, HttpClient httpClient){
        MemoryStream mem = new MemoryStream();
        string url = await @base.ExtractUrl();
        var cachedStream = await httpClient.GetStreamAsync(url);
        cachedStream.CopyTo(mem);
        mem.Position = 0;
        return mem;
    }
    public static Task<Stream> DownloadItemFromUpdate(this Base @base){
        return @base.DownloadItemFromUpdate(new HttpClient());
    }

    public static bool IsMediaMessage(this Update update){
        return (update.Message != null) && (update.Message.Photo != null || update.Message.Video != null || update.Message.Document != null || update.Message.Sticker != null);
    }
}