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
        var language = "";
        
        if (!File.Exists(settingsPath))
        {
            await UserRegister(client, chatId);
            return;
        }
        
        language = File.ReadLines(settingsPath).First();

        switch (message?.Text)
        {
            case null:
                await client.SendTextMessageAsync(chatId, "The message must be either a text or a link");
                break;
            case "/start":
                if (language == "en")
                {
                    await client.SendTextMessageAsync(chatId, "Hello!\n" +
                                                              "I am a bot for downloading MP3 files from all videos in your playlist.\n" +
                                                              "To start the download, please send me the link to the playlist You want to " +
                                                              "convert to audio format!");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Привіт!\n" +
                                                              "Я - бот для завантаження МР3 файлів з усіх відео з вашого плейлиста.\n" +
                                                              "Для початку завантаження, будь ласка, надішліть мені посилання на плейлист," +
                                                              " який Ви хочете конвертувати в аудіо формат!");
                }
                break;
            case "/settings":
                if (language == "en")
                {
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
                    await client.SendTextMessageAsync(chatId,
                        "Select an option You want to edit in playlists download:",
                        replyMarkup: settingsInlineKeyboard);
                }
                else
                {
                    var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Бітрейт", "Settings Bitrate"),
                            InlineKeyboardButton.WithCallbackData("Канали", "Settings Channels")
                        },
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                        }
                    });
                    await client.SendTextMessageAsync(chatId, "Виберіть опцію, яку Ви хочете змінити в завантаженні плейлистів:",
                        replyMarkup: settingsInlineKeyboard);
                }
                break;
            case "/change_language":
                Settings.LanguageSettings(client, chatId, update);
                break;
            case "/help":
                if (language == "en")
                {
                    await client.SendTextMessageAsync(chatId, "To download a playlist, just send me the link, and " +
                                                              "I'll automatically respond with further instructions.\n\n" +
                                                              "Here’s my list of commands:\n" +
                                                              "/start - start the bot\n" +
                                                              "/settings - open audio quality settings\n" +
                                                              "/change_language - open the interface language change menu\n" +
                                                              "/help - view the bot's command list\n" +
                                                              "/info - view information about the author and the project");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Щоб завантажити плейлист просто мені надішліть на нього посилання, " +
                                                              "і я автоматично відповім та надам вам подальші інструкції.\n\n" +
                                                              "Мій перелік команд:\n" +
                                                              "/start - розпочати роботу бота\n" +
                                                              "/settings - відкрити налаштування якості завантажуваних аудіо\n" +
                                                              "/change_language - відкрити меню зміни мови інтерфейсу\n" +
                                                              "/help - переглянути перелік команд бота\n" +
                                                              "/info - переглянути інформацію про авто та проєкт");
                }
                break;
            case "/info":
                if (language == "en")
                {
                    await client.SendTextMessageAsync(
                        chatId, 
                        "This bot was created to help you download MP3 files " +
                        "from YouTube playlists\\. It processes the " + 
                        "playlist link you provide and sends you the audio files\\.\n\n" + 
                        "If you would like to learn more or contribute to the project, " + 
                        "you can visit the [GitHub repository]" + 
                        "(https://github\\.com/Marshmalllows/YouTube\\-playlist\\-to\\-mp3\\-bot)\\.\n\n" + 
                        "Author: [@Marshmallllows](https://t\\.me/Marshmallllows)", parseMode: ParseMode.MarkdownV2);
                }
                else
                {
                    await client.SendTextMessageAsync(
                        chatId, 
                        "Цей бот створений для того, щоб допомогти вам завантажити MP3 " +
                        "файли з плейлистів YouTube\\. Він обробляє посилання на плейлист, " + 
                        "яке ви надаєте, і відправляє вам аудіофайли\\.\n\n" + 
                        "Якщо ви хочете дізнатися більше або зробити свій внесок у проєкт, ви можете відвідати " + 
                        "[репозиторій на GitHub]" + 
                        "(https://github\\.com/Marshmalllows/YouTube\\-playlist\\-to\\-mp3\\-bot)\\.\n\n" + 
                        "Автор: [@Marshmallllows](https://t\\.me/Marshmallllows)", parseMode: ParseMode.MarkdownV2);
                }
                break;
            default:
                var sentMessage = language == "en" 
                    ? await client.SendTextMessageAsync(chatId, "Getting playlist info...") 
                    : await client.SendTextMessageAsync(chatId, "Отримання інформації про плейлист...");
                try
                {
                    var playlist = await Youtube.Playlists.GetAsync(message.Text);
                    var videos = await Youtube.Playlists.GetVideosAsync(playlist.Id);
                    if (language == "en")
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Yes", playlist.Id.ToString()),
                                InlineKeyboardButton.WithCallbackData("No", "No")
                            }
                        });
                        await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                            $"Are you sure you want to download audio from " +
                            $"{videos.Count} videos from \"{playlist.Title}\" playlist?",
                            replyMarkup: inlineKeyboard);
                    }
                    else
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Так", playlist.Id.ToString()),
                                InlineKeyboardButton.WithCallbackData("Ні", "No")
                            }
                        });
                        await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                            $"Ви впевнені, що хочете завантажити аудіо з " +
                            $"{videos.Count} відео з плейлиста \"{playlist.Title}\"?",
                            replyMarkup: inlineKeyboard);
                    }
                    break;
                }
                catch (PlaylistUnavailableException)
                {
                    if (language == "en")
                    {
                        await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Посилання неправильне або плейлист недоступний");
                    }
                }
                catch (ArgumentException)
                {
                    if (language == "en")
                    {
                        await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Посилання неправильне або плейлист недоступний");
                    }
                }
                catch (HttpRequestException)
                {
                    if (language == "en")
                    {
                        await client.SendTextMessageAsync(chatId, "The link is invalid or the playlist is unavailable");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Посилання неправильне або плейлист недоступний");
                    }
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
        var settingsPath = @"..\..\..\user settings\" + update.CallbackQuery?.From.Username;
        var language = File.ReadLines(settingsPath).First();
        var data = update.CallbackQuery.Data;
        
        if (data.Contains("Language"))
        {
            var registered = false;
            var oldUserSettings = new string[4];
            data = data.Replace(" Language", "");
            
            if (File.Exists(settingsPath))
            {
                registered = true;
                oldUserSettings = File.ReadAllLines(settingsPath);
                File.Delete(settingsPath);
            }

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

                if (registered)
                {
                    for (var i = 1; i < 4; i++)
                    {
                        writer.WriteLine(oldUserSettings[i]);
                    }
                }
                else
                {
                    writer.WriteLine("192000\n44100\n2");
                }
            }

            await client.DeleteMessageAsync(chatId, messageId);
            
            if (registered)
            {
                if (data == "English")
                {
                    await client.SendTextMessageAsync(chatId, "Done! Interface language changed to English!");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Готово! Мова інтерфейсу змінена на українську!");
                }
            }
            else
            {
                if (data == "English")
                {
                    await client.SendTextMessageAsync(chatId, "Great! Now you can download audio files from videos" +
                                                              " in playlists. Just provide me with the link!");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Дуже добре! Тепер Ви можете замантажувати аудіо " +
                                                              "файли з відео з плейлистів. Просто надайте мені посилання!");
                }
            }
            return;
        }
        
        var userSettings = File.ReadAllLines(settingsPath);
        
        if (data.Contains("Settings"))
        {
            data = data.Replace("Settings ", "");
            switch (data)
            {
                case "Bitrate":
                    Settings.BitrateSettings(client, chatId, messageId, userSettings[1], language);
                    break;
                case "Sample":
                    Settings.SampleRateSettings(client, chatId, messageId, userSettings[2], language);
                    break;
                case "Channels":
                    Settings.ChannelsSettings(client, chatId, messageId, userSettings[3], language);
                    break;
                case "Back":
                    if (language == "en")
                    {
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
                        await client.EditMessageTextAsync(chatId, messageId,
                            "Select an option You want to edit during playlists download:",
                            replyMarkup: settingsInlineKeyboard);
                    }
                    else
                    {
                        var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Бітрейт", "Settings Bitrate"),
                                InlineKeyboardButton.WithCallbackData("Канали", "Settings Channels")
                            },
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                            }
                        });
                        await client.SendTextMessageAsync(chatId, "Виберіть опцію, яку Ви хочете змінити в завантаженні плейлистів:",
                            replyMarkup: settingsInlineKeyboard);
                    }
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

            if (language == "en")
            {
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
                await client.EditMessageTextAsync(chatId, messageId, "Done! Select an option You want to edit in playlists download:",
                    replyMarkup: settingsInlineKeyboard);
            }
            else
            {
                var settingsInlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Бітрейт", "Settings Bitrate"),
                        InlineKeyboardButton.WithCallbackData("Канали", "Settings Channels")
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                    }
                });
                await client.SendTextMessageAsync(chatId, "Готово! Виберіть опцію, яку Ви хочете змінити в завантаженні плейлистів:",
                    replyMarkup: settingsInlineKeyboard);
            }
        }
        else
        {
            await client.DeleteMessageAsync(chatId, messageId);

            if (data == "No")
            {
                if (language == "en")
                {
                    await client.SendTextMessageAsync(chatId,
                        "As you say so! You can send another playlist link to download");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId,
                        "Як скажете! Ви можете надіслати посилання на інший плейлист для завантаження");
                }
                
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
                if (language == "en")
                {
                    await client.SendTextMessageAsync(chatId, "Done! Enjoy!");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Готово! Насолоджуйтесь!");
                }
            }
        }
    }

    private static async Task DownloadPlaylist(PlaylistId playlistId, ITelegramBotClient client, ChatId chatId, string filePath, 
        string username)
    {
        var sentMessage = await client.SendTextMessageAsync(chatId, "Downloading... It may take some time");
        var videos = await Youtube.Playlists.GetVideosAsync(playlistId);
        var userSettings = File.ReadAllLines(@"..\..\..\user settings\" + username);
        var language = File.ReadLines(@"..\..\..\user settings\" + username).First();
        var downloadedCount = 0;

        if (language == "en")
        {
            await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                "Downloading... It may take some time\n" +
                "Downloaded 0% " +
                $"({downloadedCount}/{videos.Count})");
        }
        else
        {
            await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                "Завантаження... Це може зайняти якийсь час\n" +
                "Завантажено 0% " +
                $"({downloadedCount}/{videos.Count})");
        }
        
        
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
                    .AddParameter("-codec:a libmp3lame")
                    .AddParameter("-qscale:a 2")
                    .AddParameter("-ab " + userSettings[1])
                    .AddParameter("-ar " + userSettings[2])
                    .AddParameter("-ac " + userSettings[3])
                    .SetOutput(audioPath.Replace(".opus", ".mp3"));
                await conversion.Start();
                File.Delete(audioPath);
            }
            catch (HttpRequestException)
            {
                if (language == "en")
                {
                    await client.SendTextMessageAsync(chatId, $"Can`t access video \"{video.Title}\", skipping...");
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, $"Невдалось отримати доступ до відео \"{video.Title}\", пропускаю...");
                }
                
                File.Delete(SongNameValidate(video.Title));
            }
            
            downloadedCount++;

            if (language == "en")
            {
                await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                    "Downloading... It may take some time\n" +
                    $"Downloaded {downloadedCount * 100 / videos.Count}% " +
                    $"({downloadedCount}/{videos.Count})");
            }
            else
            {
                await client.EditMessageTextAsync(sentMessage.Chat.Id, sentMessage.MessageId,
                    "Завантаження... Це може зайняти якийсь час\n" +
                    $"Завантажено {downloadedCount * 100 / videos.Count}% " +
                    $"({downloadedCount}/{videos.Count})");
            }
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

    private static async Task UserRegister(ITelegramBotClient client, ChatId chatId)
    {
        var languageInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("\ud83c\uddec\ud83c\udde7", "English Language"),
                InlineKeyboardButton.WithCallbackData("\ud83c\uddfa\ud83c\udde6", "Ukrainian Language")
            }
        });
        await client.SendTextMessageAsync(chatId, 
            "Hello! I can download and send You MP3 files from videos in a playlist if you send me the link!\n" +
            "To start, just press the start button or use the /start command.\n" +
            "Before starting, please, choose the language You want to use.\n\n" +
            "Привіт! Я можу завантажити та надіслати вам MP3 файли з відео в плейлисті, якщо Ви надішлете мені посилання!\n" +
            "Щоб почати, просто натисніть кнопку розпочати або використайте команду /start.\n" +
            "Перед початком роботи, будь ласка, оберіть мову, якою Ви хочете користуватись.", replyMarkup: languageInlineKeyboard);
        await Task.CompletedTask;
    }
}