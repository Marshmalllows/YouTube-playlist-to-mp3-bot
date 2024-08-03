using System.Collections.Concurrent;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using File = System.IO.File;

namespace YouTube_playlist_to_mp3_bot;

internal class Program
{
    private static readonly YoutubeClient Youtube = new YoutubeClient();
    
    public static void Main()
    {
        var token = File.ReadAllText(@"..\..\..\token.txt");
        var bot = new Host(token);
        bot.Start();
        bot.OnMessage += MessageHandler;
        Console.ReadKey();
    }

    private static void MessageHandler(ITelegramBotClient client, Update update)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                MessageReceived(client, update);
                break;
            case UpdateType.CallbackQuery:
                CallbackQueryReceived(client, update);
                break;
            default:
                Console.WriteLine("Unknown update type");
                break;
        }
    }

    private static async void MessageReceived(ITelegramBotClient client, Update update)
    {
        var reserveChatId = long.Parse(File.ReadAllText(@"..\..\..\reserveChatId.txt"));
        var chatId = update.Message?.Chat.Id ?? reserveChatId;
        var message = update.Message;
        var user = update.Message?.From?.Username;

        switch (message?.Text)
        {
            case null:
                await client.SendTextMessageAsync(chatId, "The message must be either a text or a link");
                break;
            case "/start":
                await client.SendTextMessageAsync(chatId, "Hello!\n" +
                                                          "I am a bot for downloading MP3 files from all videos in your playlist.\n" +
                                                          "To start the download, please send me the link to the playlist you want to " +
                                                          "convert to audio format.");
                break;
            default:
                try
                {
                    var playlist = await Youtube.Playlists.GetAsync(message.Text);
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Yes", playlist.Id.ToString()),
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
                break;
        }
    }

    private static async void CallbackQueryReceived(ITelegramBotClient client, Update update)
    {
        var reserveChatId = long.Parse(File.ReadAllText(@"..\..\..\reserveChatId.txt"));
        var chatId = update.CallbackQuery?.Message?.Chat.Id ?? reserveChatId;
        var messageId = update.CallbackQuery.Message.MessageId;
        await client.DeleteMessageAsync(chatId, messageId);

        if (update.CallbackQuery.Data == "No")
        {
            await client.SendTextMessageAsync(chatId, "As you say so! You can send another playlist link to download");
        }
        else
        {
            await client.SendTextMessageAsync(chatId, "Downloading... It may take some time");
            var filePath = @"..\..\..\temp\" + update.CallbackQuery.From.Username;
            await DownloadPlaylist((PlaylistId)update.CallbackQuery.Data, filePath);
            var audios = Directory.GetFiles(filePath);
            foreach (var audio in audios)
            {
                var stream = File.OpenRead(audio);
                var audioName = audio.Split('\\').Last();
                var inputFile = InputFile.FromStream(stream, audioName.Split('.').First());
                await client.SendAudioAsync(chatId, inputFile);
                stream.Close();
            }
            await client.SendTextMessageAsync(chatId, "Done! Enjoy!");
            foreach (var file in Directory.GetFiles(filePath))
            {
                File.Delete(file);
            }
            Directory.Delete(filePath);
        }
    }

    private static async Task DownloadPlaylist(PlaylistId playlistId, string filePath)
    {
        await foreach (var video in Youtube.Playlists.GetVideosAsync(playlistId))
        {
            var streamManifest = await Youtube.Videos.Streams.GetManifestAsync(video.Id);
            var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = await Youtube.Videos.Streams.GetAsync(audioStream);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            
            await using var filestream = new FileStream(filePath + SongNameValidate(video.Title), FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(filestream);
        }

        await Task.CompletedTask;
    }

    private static string SongNameValidate(string title)
    {
        var songName = title.Replace('/', '_');
        songName = songName.Replace('\\', '_');
        songName = songName.Replace(':', '_');
        songName = songName.Replace('*', '_');
        songName = songName.Replace('?', '_');
        songName = songName.Replace('"', '_');
        songName = songName.Replace('<', '_');
        songName = songName.Replace('>', '_');
        songName = songName.Replace('|', '_');
        
        if (songName.ToLower() == "con" ||
            songName.ToLower() == "prn" ||
            songName.ToLower() == "aux" ||
            songName.ToLower() == "nul")
        {
            songName = "badname";
        }
        
        return $@"\{songName}.mp3";
    }
}