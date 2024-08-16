using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using File = System.IO.File;

namespace YouTube_playlist_to_mp3_bot;

internal class Program
{
    private static readonly YoutubeClient Youtube = new();
    
    public static void Main()
    {
        var token = File.ReadAllText(@"..\..\..\token.txt");
        var bot = new Host(token); // Your token in token.txt
        FFmpeg.SetExecutablesPath(@"..\..\..\FFmpeg\bin");
        
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
        var reserveChatId = long.Parse(File.ReadAllText(@"..\..\..\reserveChatId.txt")); // Your reserve chat id in reserveChatId.txt
        var chatId = update.Message?.Chat.Id ?? reserveChatId;
        var settingsPath = @"..\..\..\user settings\" + update.Message?.From?.Username;
        var message = update.Message;
        
        if (!File.Exists(settingsPath))
        {
            await UserRegister(client, update, chatId);
            return;
        }

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
            case "/settings":
                var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Bitrate", "Settings Bitrate"),
                        InlineKeyboardButton.WithCallbackData("Sample Rate", "Settings Sample"),
                        InlineKeyboardButton.WithCallbackData("Channels", "Settings Channels")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Close", "Settings Close")
                    }
                });
                await client.SendTextMessageAsync(chatId, "Select an option you want to edit in playlists download:",
                    replyMarkup: settingsInlineKeyboard);
                break;
            default:
                var sentMessage = await client.SendTextMessageAsync(chatId, "Getting playlist info...");
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
                    var videos = await Youtube.Playlists.GetVideosAsync(playlist.Id);
                    await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                        $"Are you sure you want to download audio from " +
                        $"{videos.Count} videos from \"{playlist.Title}\" playlist?",
                        replyMarkup: inlineKeyboard);
                    break;
                }
                catch (PlaylistUnavailableException)
                {
                    await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                }
                catch (ArgumentException)
                {
                    await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                }
                catch (HttpRequestException)
                {
                    await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                await client.DeleteMessageAsync(chatId, sentMessage.MessageId);
                break;
        }
    }

    private static async void CallbackQueryReceived(ITelegramBotClient client, Update update)
    {
        var reserveChatId = long.Parse(File.ReadAllText(@"..\..\..\reserveChatId.txt")); // Your reserve chat id in reserveChatId.txt
        var chatId = update.CallbackQuery?.Message?.Chat.Id ?? reserveChatId;
        var messageId = update.CallbackQuery.Message.MessageId;
        var settingsPath = @"..\..\..\user settings\" + update.CallbackQuery?.From?.Username;
        var data = update.CallbackQuery.Data;
        
        if (data.Contains("Language"))
        {
            data = data.Replace(" Language", "");
            await using (var writer = new StreamWriter(settingsPath))
            {
                if (data == "English")
                {
                    writer.WriteLine("en");
                }
                else
                {
                    writer.WriteLine("ua");
                }
                writer.WriteLine("192k\n44100\n2");
            }

            await client.DeleteMessageAsync(chatId, messageId);
            return;
        }
        
        var userSettings = File.ReadAllLines(settingsPath);
        
        if (data.Contains("Settings"))
        {
            data = data.Replace("Settings ", "");
            switch (data)
            {
                case "Bitrate":
                    Settings.BitrateSettings(client, chatId, messageId, userSettings[1].Replace("k", ""));
                    break;
                case "Sample":
                    Settings.SampleRateSettings(client, chatId, messageId, userSettings[2]);
                    break;
                case "Channels":
                    Settings.ChannelsSettings(client, chatId, messageId, userSettings[3]);
                    break;
                case "Back":
                    var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Bitrate", "Settings Bitrate"),
                            InlineKeyboardButton.WithCallbackData("Sample Rate", "Settings Sample"),
                            InlineKeyboardButton.WithCallbackData("Channels", "Settings Channels")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Close", "Settings Close")
                        }
                    });
                    await client.EditMessageTextAsync(chatId, messageId, "Select an option you want to edit during playlists download:",
                        replyMarkup: settingsInlineKeyboard);
                    break;
                case "Close":
                    await client.DeleteMessageAsync(chatId, messageId);
                    break;
            }
            return;
        }
        
        if (data.Contains("Change"))
        {
            int userSettingsIndex;
            data = data.Replace("Change ", "");

            if (data.Contains("Bitrate"))
            {
                data = data.Replace("Bitrate ", "");
                userSettingsIndex = 1;
            }
            else if (data.Contains("Sample"))
            {
                data = data.Replace("Sample ", "");
                userSettingsIndex = 2;
            }
            else
            {
                data = data.Replace("Channel ", "");
                userSettingsIndex = 3;
            }
            
            File.Delete(settingsPath);
            await using (var writer = new StreamWriter(settingsPath))
            {
                userSettings[userSettingsIndex] = data;
                foreach (var userSetting in userSettings)
                {
                    await writer.WriteLineAsync(userSetting);
                }
            }
            
            var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Bitrate", "Settings Bitrate"),
                    InlineKeyboardButton.WithCallbackData("Sample Rate", "Settings Sample"),
                    InlineKeyboardButton.WithCallbackData("Channels", "Settings Channels")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Close", "Settings Close")
                }
            });
            await client.EditMessageTextAsync(chatId, messageId, "Done! Select an option you want to edit in playlists download:",
                replyMarkup: settingsInlineKeyboard);
        }
        else
        {
            await client.DeleteMessageAsync(chatId, messageId);

            if (data == "No")
            {
                await client.SendTextMessageAsync(chatId,
                    "As you say so! You can send another playlist link to download");
            }
            else
            {
                var filePath = @"..\..\..\temp\" + update.CallbackQuery.From.Username + update.CallbackQuery.From.Id
                               + update.CallbackQuery.Message.MessageId;

                await DownloadPlaylist((PlaylistId)update.CallbackQuery.Data, client, chatId, filePath,
                    update.CallbackQuery.From.Username);

                var audios = Directory.GetFiles(filePath);
                foreach (var audio in audios)
                {
                    var stream = File.OpenRead(audio);
                    var audioName = audio.Split('\\').Last();
                    var inputFile = InputFile.FromStream(stream, audioName);
                    var sent = true;

                    do
                    {
                        try
                        {
                            await client.SendAudioAsync(chatId, inputFile);
                            await Task.Delay(1000);
                            sent = true;
                        }
                        catch (ApiRequestException ex)
                        {
                            if (ex.Message.Contains("Too Many Requests"))
                            {
                                var retryAfter = ex.Parameters?.RetryAfter ?? 60;
                                Console.WriteLine($"Rate limit exceeded. Retrying after {retryAfter} seconds...");
                                await Task.Delay(retryAfter * 1000);
                                sent = false;
                            }
                            else if (ex.Message.Contains("Request Entity Too Large"))
                            {
                                await client.SendTextMessageAsync(chatId,
                                    $"File \"{audioName}\" is too big. Skipping...");
                            }
                            else
                            {
                                Console.WriteLine($"An error occurred: {ex.Message}");
                            }
                        }
                    } while (!sent);

                    stream.Close();
                    File.Delete(audio);
                }

                Directory.Delete(filePath);
                await client.SendTextMessageAsync(chatId, "Done! Enjoy!");
            }
        }
    }

    private static async Task DownloadPlaylist(PlaylistId playlistId, ITelegramBotClient client, ChatId chatId, string filePath, 
        string username)
    {
        var sentMessage = await client.SendTextMessageAsync(chatId, "Downloading... It may take some time");
        var videos = await Youtube.Playlists.GetVideosAsync(playlistId);
        var userSettings = File.ReadAllLines(@"..\..\..\user settings\" + username);
        var downloadedCount = 0;
        await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
            "Downloading... It may take some time\n" +
            "Downloaded 0% " +
            $"({downloadedCount}/{videos.Count})");
        
        Directory.CreateDirectory(filePath);
        
        await foreach (var video in Youtube.Playlists.GetVideosAsync(playlistId))
        {
            try
            {
                var streamManifest = await Youtube.Videos.Streams.GetManifestAsync(video.Id);
                var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var stream = await Youtube.Videos.Streams.GetAsync(audioStream);
                var audioPath = filePath + SongNameValidate(video.Title);
                
                var filestream = new FileStream(audioPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(filestream);
                filestream.Close();

                var conversion = FFmpeg.Conversions.New().AddParameter($"-i \"{audioPath}\"")
                    .AddParameter("-ab " + userSettings[1])
                    .AddParameter("-ar " + userSettings[2])
                    .AddParameter("-ac " + userSettings[3])
                    .SetOutput(audioPath.Replace(".opus", ".mp3"));
                await conversion.Start();
                File.Delete(audioPath);
            }
            catch (HttpRequestException)
            {
                await client.SendTextMessageAsync(chatId, $"Can`t access video \"{video.Title}\", skipping...");
                File.Delete(SongNameValidate(video.Title));
            }
            
            downloadedCount++;
            
            await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                "Downloading... It may take some time\n" +
                $"Downloaded {downloadedCount * 100 / videos.Count}% " +
                $"({downloadedCount}/{videos.Count})");
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
        
        return $@"\{songName}.opus";
    }

    private static async Task UserRegister(ITelegramBotClient client, Update update, ChatId chatId)
    {
        var languageInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("\ud83c\uddec\ud83c\udde7", "English Language"),
                InlineKeyboardButton.WithCallbackData("\ud83c\uddfa\ud83c\udde6", "Ukrainian Language")
            }
        });
        await client.SendTextMessageAsync(chatId, "sometext", replyMarkup: languageInlineKeyboard);
        await Task.CompletedTask;
    }
}