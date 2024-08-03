using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using File = System.IO.File;

namespace YouTube_playlist_to_mp3_bot;

internal class Program
{
    public static void Main()
    {
        var token = File.ReadAllText(@"..\..\..\token.txt");
        var bot = new Host(token);
        bot.Start();
        bot.OnMessage += MessageHandler;
        Console.ReadKey();
    }

    private static async void MessageHandler(ITelegramBotClient client, Update update)
    {
        var reserveChatId = long.Parse(File.ReadAllText(@"..\..\..\reserveChatId.txt"));
        var chatId = update.Message?.Chat.Id ?? reserveChatId;
        var message = update.Message;
        var youtube = new YoutubeClient();

        if (message?.Text is null)
        {
            await client.SendTextMessageAsync(chatId, "The message must be either a text or a link");
            return;
        }

        if (message.Text == "/start")
        {
            await client.SendTextMessageAsync(chatId, "Hello!\n" +
                                                      "I am a bot for downloading MP3 files from all videos in your playlist.\n" +
                                                      "To start the download, please send me the link to the playlist you want to " +
                                                      "convert to audio format.");
        }
        else
        {
            try
            {
                var playlist = await youtube.Playlists.GetAsync(message.Text);
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Yes", "Yes"),
                        InlineKeyboardButton.WithCallbackData("No", "No")
                    }
                });
                await client.SendTextMessageAsync(chatId, $"Are you sure you want to download audio from " + 
                                                          $"{playlist.Count} videos from \"{playlist.Title}\" playlist?", 
                    replyMarkup: inlineKeyboard);
            }
            catch (PlaylistUnavailableException)
            {
                await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
            }
            catch (ArgumentException)
            {
                await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
}