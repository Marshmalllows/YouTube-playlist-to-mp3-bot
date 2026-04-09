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
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using File = System.IO.File;

namespace YouTube_playlist_to_mp3_bot;

internal class Program
{
    private static readonly YoutubeClient Youtube = new();
    private static readonly string BaseDir = AppContext.BaseDirectory;

    public static void Main()
    {
        var envFile = Path.Combine(BaseDir, ".env");
        if (File.Exists(envFile))
        {
            foreach (var line in File.ReadAllLines(envFile))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }

        var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
                    ?? throw new Exception("BOT_TOKEN environment variable is not set");

        var ffmpegLocalPath = Path.Combine(BaseDir, "FFmpeg", "bin");
        if (Directory.Exists(ffmpegLocalPath))
            FFmpeg.SetExecutablesPath(ffmpegLocalPath);
        Directory.CreateDirectory(Path.Combine(BaseDir, "user settings"));
        Directory.CreateDirectory(Path.Combine(BaseDir, "temp"));

        string? exit;
        do
        {
            var bot = new Host(token);
            bot.Start();
            bot.OnMessage += MessageHandler;
            exit = Console.ReadLine();

        } while (exit != "/exit");
    }

    private static void MessageHandler(ITelegramBotClient client, Update update)
    {
        try
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
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async void MessageReceived(ITelegramBotClient client, Update update)
    {
        try
        {
            var reserveChatId = long.Parse(Environment.GetEnvironmentVariable("RESERVE_CHAT_ID") ?? "0");
            var chatId = update.Message?.Chat.Id ?? reserveChatId;
            var settingsPath = Path.Combine(BaseDir, "user settings", update.Message?.From?.Username ?? "unknown");
            var message = update.Message;

            if (!File.Exists(settingsPath))
            {
                await UserRegister(client, chatId);
                return;
            }

            var language = File.ReadLines(settingsPath).First();

            switch (message?.Text)
            {
                case null:
                    await client.SendMessage(chatId, "The message must be either a text or a link");
                    break;
                case "/start":
                    if (language == "en")
                    {
                        await client.SendMessage(chatId, "Hello!\n" +
                                                         "I am a bot for downloading MP3 files from all videos in your playlist.\n" +
                                                         "To start the download, please send me the link to the playlist You want to " +
                                                         "convert to audio format!");
                    }
                    else
                    {
                        await client.SendMessage(chatId, "Привіт!\n" +
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
                        await client.SendMessage(chatId,
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
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                            }
                        });
                        await client.SendMessage(chatId,
                            "Виберіть опцію, яку Ви хочете змінити в завантаженні плейлистів:",
                            replyMarkup: settingsInlineKeyboard);
                    }

                    break;
                case "/change_language":
                    Settings.LanguageSettings(client, chatId, update);
                    break;
                case "/help":
                    if (language == "en")
                    {
                        await client.SendMessage(chatId, "To download a playlist, just send me the link, and " +
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
                        await client.SendMessage(chatId,
                            "Щоб завантажити плейлист просто мені надішліть на нього посилання, " +
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
                        await client.SendMessage(
                            chatId,
                            "This bot was created to help you download MP3 files " +
                            "from YouTube playlists\\. It processes the " +
                            "playlist link you provide and sends you the audio files\\.\n\n" +
                            "If you would like to learn more or contribute to the project, " +
                            "you can visit the [GitHub repository]" +
                            "(https://github\\.com/Marshmalllows/YouTube\\-playlist\\-to\\-mp3\\-bot)\\.\n\n" +
                            "Author: [@Marshmallllows](https://t\\.me/Marshmallllows)",
                            parseMode: ParseMode.MarkdownV2);
                    }
                    else
                    {
                        await client.SendMessage(
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
                        ? await client.SendMessage(chatId, "Getting info...")
                        : await client.SendMessage(chatId, "Отримання інформації...");
                    var confirmed = false;
                    try
                    {
                        // Try playlist first
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
                                await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                                    $"Are you sure you want to download audio from {videos.Count} videos from \"{playlist.Title}\" playlist?",
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
                                await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                                    $"Ви впевнені, що хочете завантажити аудіо з {videos.Count} відео з плейлиста \"{playlist.Title}\"?",
                                    replyMarkup: inlineKeyboard);
                            }
                            confirmed = true;
                        }
                        catch (PlaylistUnavailableException) { }
                        catch (ArgumentException) { }

                        // Fallback: try single video
                        if (!confirmed)
                        {
                            try
                            {
                                var video = await Youtube.Videos.GetAsync(message.Text);
                                if (language == "en")
                                {
                                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                    {
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Yes", "video:" + video.Id),
                                            InlineKeyboardButton.WithCallbackData("No", "No")
                                        }
                                    });
                                    await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                                        $"Are you sure you want to download \"{video.Title}\"?",
                                        replyMarkup: inlineKeyboard);
                                }
                                else
                                {
                                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                    {
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Так", "video:" + video.Id),
                                            InlineKeyboardButton.WithCallbackData("Ні", "No")
                                        }
                                    });
                                    await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                                        $"Ви впевнені, що хочете завантажити \"{video.Title}\"?",
                                        replyMarkup: inlineKeyboard);
                                }
                                confirmed = true;
                            }
                            catch (VideoUnavailableException) { }
                            catch (ArgumentException) { }
                        }
                    }
                    catch (HttpRequestException) { }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }

                    if (confirmed) break;

                    if (language == "en")
                        await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                            "The link is invalid or the video/playlist is unavailable");
                    else
                        await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                            "Посилання неправильне або відео/плейлист недоступний");
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[MessageReceived] {e}");
        }
    }

    private static async void CallbackQueryReceived(ITelegramBotClient client, Update update)
    {
        try
        {
            var reserveChatId = long.Parse(Environment.GetEnvironmentVariable("RESERVE_CHAT_ID") ?? "0");
            var chatId = update.CallbackQuery?.Message?.Chat.Id ?? reserveChatId;
            var messageId = update.CallbackQuery!.Message!.MessageId;
            var settingsPath = Path.Combine(BaseDir, "user settings", update.CallbackQuery?.From.Username ?? "unknown");

            var data = update.CallbackQuery!.Data;

            if (data!.Contains("Language"))
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

                await client.DeleteMessage(chatId, messageId);

                if (registered)
                {
                    if (data == "English")
                    {
                        await client.SendMessage(chatId, "Done! Interface language changed to English!");
                    }
                    else
                    {
                        await client.SendMessage(chatId, "Готово! Мова інтерфейсу змінена на українську!");
                    }
                }
                else
                {
                    if (data == "English")
                    {
                        await client.SendMessage(chatId, "Great! Now you can download audio files from videos" +
                                                         " in playlists. Just provide me with the link!");
                    }
                    else
                    {
                        await client.SendMessage(chatId, "Дуже добре! Тепер Ви можете завантажувати аудіо " +
                                                         "файли з відео з плейлистів. Просто надайте мені посилання!");
                    }
                }

                return;
            }

            var language = File.ReadLines(settingsPath).First();
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
                            await client.EditMessageText(chatId, messageId,
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
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                                }
                            });
                            await client.EditMessageText(chatId, messageId,
                                "Виберіть опцію, яку Ви хочете змінити в завантаженні плейлистів:",
                                replyMarkup: settingsInlineKeyboard);
                        }

                        break;
                    case "Close":
                        await client.DeleteMessage(chatId, messageId);
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
                    await client.EditMessageText(chatId, messageId,
                        "Done! Select an option You want to edit in playlists download:",
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
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Частота дискретизації", "Settings Sample"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                        }
                    });
                    await client.EditMessageText(chatId, messageId, "Готово! Виберіть опцію, яку Ви хочете змінити" +
                                                                    " в завантаженні плейлистів:",
                        replyMarkup: settingsInlineKeyboard);
                }
            }
            else
            {
                await client.DeleteMessage(chatId, messageId);

                if (data == "No")
                {
                    if (language == "en")
                    {
                        await client.SendMessage(chatId,
                            "As you say so! You can send another playlist link to download");
                    }
                    else
                    {
                        await client.SendMessage(chatId,
                            "Як скажете! Ви можете надіслати посилання на інший плейлист для завантаження");
                    }

                }
                else
                {
                    var filePath = Path.Combine(BaseDir, "temp", update.CallbackQuery.From.Username +
                                                                 update.CallbackQuery.From.Id
                                                                 + update.CallbackQuery.Message.MessageId);

                    if (data.StartsWith("video:"))
                    {
                        var videoId = (VideoId)data["video:".Length..];
                        await DownloadSingleVideo(videoId, client, chatId, filePath, update.CallbackQuery.From.Username!);
                    }
                    else
                    {
                        await DownloadPlaylist((PlaylistId)data, client, chatId, filePath, update.CallbackQuery.From.Username!);
                    }

                    Directory.Delete(filePath, recursive: true);
                    if (language == "en")
                    {
                        await client.SendMessage(chatId, "Done! Enjoy!");
                    }
                    else
                    {
                        await client.SendMessage(chatId, "Готово! Насолоджуйтесь!");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CallbackQueryReceived] {e}");
        }
    }

    private static async Task DownloadPlaylist(PlaylistId playlistId, ITelegramBotClient client, ChatId chatId,
        string filePath, string username)
    {
        var userSettings = File.ReadAllLines(Path.Combine(BaseDir, "user settings", username));
        var language = userSettings[0];
        var sentMessage = language == "en"
            ? await client.SendMessage(chatId, "Downloading... It may take some time")
            : await client.SendMessage(chatId, "Завантаження... Це може зайняти якийсь час");
        var videos = await Youtube.Playlists.GetVideosAsync(playlistId);
        var downloadedCount = 0;

        if (language == "en")
            await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                $"Downloading... It may take some time\nDownloaded 0% ({downloadedCount}/{videos.Count})");
        else
            await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                $"Завантаження... Це може зайняти якийсь час\nЗавантажено 0% ({downloadedCount}/{videos.Count})");

        Directory.CreateDirectory(filePath);

        await foreach (var video in Youtube.Playlists.GetVideosAsync(playlistId))
        {
            try
            {
                await DownloadAndSendVideo(video, client, chatId, filePath, userSettings, language);
            }
            catch (VideoUnplayableException)
            {
                if (language == "en")
                    await client.SendMessage(chatId, $"Can't access video \"{video.Title}\", skipping...");
                else
                    await client.SendMessage(chatId, $"Невдалось отримати доступ до відео \"{video.Title}\", пропускаю...");
            }
            catch (HttpRequestException)
            {
                if (language == "en")
                    await client.SendMessage(chatId, $"Can't access video \"{video.Title}\", skipping...");
                else
                    await client.SendMessage(chatId, $"Невдалось отримати доступ до відео \"{video.Title}\", пропускаю...");
            }

            downloadedCount++;

            if (language == "en")
                await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                    $"Downloading... It may take some time\nDownloaded {downloadedCount * 100 / videos.Count}% ({downloadedCount}/{videos.Count})");
            else
                await client.EditMessageText(sentMessage.Chat.Id, sentMessage.MessageId,
                    $"Завантаження... Це може зайняти якийсь час\nЗавантажено {downloadedCount * 100 / videos.Count}% ({downloadedCount}/{videos.Count})");
        }

        await client.DeleteMessage(chatId, sentMessage.MessageId);
    }

    private static async Task DownloadSingleVideo(VideoId videoId, ITelegramBotClient client, ChatId chatId,
        string filePath, string username)
    {
        var userSettings = File.ReadAllLines(Path.Combine(BaseDir, "user settings", username));
        var language = userSettings[0];
        var sentMessage = language == "en"
            ? await client.SendMessage(chatId, "Downloading... It may take some time")
            : await client.SendMessage(chatId, "Завантаження... Це може зайняти якийсь час");

        Directory.CreateDirectory(filePath);

        var video = await Youtube.Videos.GetAsync(videoId);
        try
        {
            await DownloadAndSendVideo(video, client, chatId, filePath, userSettings, language);
        }
        catch (VideoUnplayableException)
        {
            if (language == "en")
                await client.SendMessage(chatId, $"Can't access \"{video.Title}\", skipping...");
            else
                await client.SendMessage(chatId, $"Невдалось отримати доступ до \"{video.Title}\", пропускаю...");
        }
        catch (HttpRequestException)
        {
            if (language == "en")
                await client.SendMessage(chatId, $"Can't access \"{video.Title}\", skipping...");
            else
                await client.SendMessage(chatId, $"Невдалось отримати доступ до \"{video.Title}\", пропускаю...");
        }

        await client.DeleteMessage(chatId, sentMessage.MessageId);
    }

    private static async Task DownloadAndSendVideo(IVideo video, ITelegramBotClient client, ChatId chatId,
        string filePath, string[] userSettings, string language)
    {
        var streamManifest = await Youtube.Videos.Streams.GetManifestAsync(video.Id);
        var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        if (audioStream.Size.Bytes > 100 * 1024 * 1024)
        {
            if (language == "en")
                await client.SendMessage(chatId, $"File \"{video.Title}\" is too big (>{audioStream.Size.MegaBytes:F0} MB), skipping...");
            else
                await client.SendMessage(chatId, $"Файл \"{video.Title}\" завеликий (>{audioStream.Size.MegaBytes:F0} МБ), пропускаю...");
            return;
        }

        var stream = await Youtube.Videos.Streams.GetAsync(audioStream);
        var audioPath = filePath + SongNameValidate(video.Title);
        var mp3Path = audioPath.Replace(".opus", ".mp3");

        var filestream = new FileStream(audioPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(filestream);
        filestream.Close();

        var conversion = FFmpeg.Conversions.New().AddParameter($"-i \"{audioPath}\"")
            .AddParameter("-codec:a libmp3lame")
            .AddParameter("-qscale:a 2")
            .AddParameter("-ab " + userSettings[1])
            .AddParameter("-ar " + userSettings[2])
            .AddParameter("-ac " + userSettings[3])
            .SetOutput(mp3Path);
        await conversion.Start();
        File.Delete(audioPath);

        // Download thumbnail
        MemoryStream? thumbnailStream = null;
        var thumbInfo = video.Thumbnails.OrderBy(t => t.Resolution.Width).FirstOrDefault();
        if (thumbInfo != null)
        {
            try
            {
                using var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(thumbInfo.Url);
                thumbnailStream = new MemoryStream(bytes);
            }
            catch { thumbnailStream = null; }
        }

        // Send audio with metadata
        var mp3Stream = File.OpenRead(mp3Path);
        var sent = false;
        do
        {
            try
            {
                if (thumbnailStream != null) thumbnailStream.Position = 0;
                mp3Stream.Position = 0;
                await client.SendAudio(chatId,
                    InputFile.FromStream(mp3Stream, video.Title + ".mp3"),
                    title: video.Title,
                    performer: video.Author.ChannelTitle,
                    thumbnail: thumbnailStream != null ? InputFile.FromStream(thumbnailStream, "thumb.jpg") : null);
                await Task.Delay(1000);
                sent = true;
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("Too Many Requests"))
            {
                var retryAfter = ex.Parameters?.RetryAfter ?? 60;
                Console.WriteLine($"Rate limit exceeded. Retrying after {retryAfter} seconds...");
                await Task.Delay(retryAfter * 1000);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("Request Entity Too Large"))
            {
                if (language == "en")
                    await client.SendMessage(chatId, $"File \"{video.Title}\" is too big to send. Skipping...");
                else
                    await client.SendMessage(chatId, $"Файл \"{video.Title}\" завеликий для відправки. Пропускаю...");
                sent = true;
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"Error sending audio: {ex.Message}");
                sent = true;
            }
        } while (!sent);

        mp3Stream.Close();
        thumbnailStream?.Dispose();
        File.Delete(mp3Path);
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
        await client.SendMessage(chatId,
            "Hello! I can download and send You MP3 files from videos in a playlist if you send me the link!\n" +
            "To start, just press the start button or use the /start command.\n" +
            "Before starting, please, choose the language You want to use.\n\n" +
            "Привіт! Я можу завантажити та надіслати вам MP3 файли з відео в плейлисті, якщо Ви надішлете мені посилання!\n" +
            "Щоб почати, просто натисніть кнопку розпочати або використайте команду /start.\n" +
            "Перед початком роботи, будь ласка, оберіть мову, якою Ви хочете користуватись.",
            replyMarkup: languageInlineKeyboard);
        await Task.CompletedTask;
    }
}