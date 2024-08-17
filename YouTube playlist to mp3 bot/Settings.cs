using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace YouTube_playlist_to_mp3_bot;

public static class Settings
{
    public static async void BitrateSettings(ITelegramBotClient client, ChatId chatId, int messageId, string userBitrate, string language)
    {
        var checks = new string[5];
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        var checkIndex = int.Parse(userBitrate) / 64000 - 1;
        checks[checkIndex] = "✅";

        if (language == "en")
        {
            var bitrateInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "64 kbps", "Change Bitrate 64000"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "128 kbps", "Change Bitrate 128000"),
                    InlineKeyboardButton.WithCallbackData(checks[2] + "192 kbps", "Change Bitrate 192000")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[3] + "256 kbps", "Change Bitrate 256000"),
                    InlineKeyboardButton.WithCallbackData(checks[4] + "320 kbps", "Change Bitrate 320000")
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Back", "Settings Back")
                }
            });
            await client.EditMessageTextAsync(chatId, messageId, "Select bitrate option you want to use:",
                replyMarkup: bitrateInlineKeyboard);
        }
        else
        {
            var bitrateInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "64 kbps", "Change Bitrate 64000"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "128 kbps", "Change Bitrate 128000"),
                    InlineKeyboardButton.WithCallbackData(checks[2] + "192 kbps", "Change Bitrate 192000")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[3] + "256 kbps", "Change Bitrate 256000"),
                    InlineKeyboardButton.WithCallbackData(checks[4] + "320 kbps", "Change Bitrate 320000")
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "Settings Back")
                }
            });
            await client.EditMessageTextAsync(chatId, messageId, "Виберіть налаштування бітрейту, які ви хочете використовувати:",
                replyMarkup: bitrateInlineKeyboard);
        }
    }
    
    public static async void SampleRateSettings(ITelegramBotClient client, ChatId chatId, int messageId,
        string userSampleRate, string language)
    {
        var checks = new string[3];
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        var checkIndex = userSampleRate switch
        {
            "22050" => 0,
            "44100" => 1,
            "48000" => 2,
            _ => 0
        };
        
        checks[checkIndex] = "✅";

        if (language == "en")
        {
            var sampleInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "22,050 Hz", "Change Sample 22050")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[1] + "44,100 Hz", "Change Sample 44100"),
                    InlineKeyboardButton.WithCallbackData(checks[2] + "48,000 Hz", "Change Sample 48000")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Back", "Settings Back"),
                }
            });
        
            await client.EditMessageTextAsync(chatId, messageId, "Select sample rate option you want to use:",
                replyMarkup: sampleInlineKeyboard);
        }
        else
        {
            var sampleInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "22,050 Hz", "Change Sample 22050")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[1] + "44,100 Hz", "Change Sample 44100"),
                    InlineKeyboardButton.WithCallbackData(checks[2] + "48,000 Hz", "Change Sample 48000")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "Settings Back"),
                }
            });
        
            await client.EditMessageTextAsync(chatId, messageId, "Виберіть налаштування частоти дискретизації, які ви хочете використовувати:",
                replyMarkup: sampleInlineKeyboard);
        }
    }
    
    public static async void ChannelsSettings(ITelegramBotClient client, ChatId chatId, int messageId, string userChannel, 
        string language)
    {
        var checks = new string[2];
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        var checkIndex = userChannel == "1" ? 0 : 1;
        
        checks[checkIndex] = "✅";

        if (language == "en")
        {
            var channelsInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "Mono", "Change Channel 1"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "Stereo", "Change Channel 2")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Back", "Settings Back")
                }
            });
            await client.EditMessageTextAsync(chatId, messageId, "Select channels option you want to use:",
                replyMarkup: channelsInlineKeyboard);
        }
        else
        {
            var channelsInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "Mono", "Change Channel 1"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "Stereo", "Change Channel 2")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "Settings Back")
                }
            });
            await client.EditMessageTextAsync(chatId, messageId, "Виберіть налаштування каналів, які ви хочете використовувати:",
                replyMarkup: channelsInlineKeyboard);
        }
    }

    public static async void LanguageSettings(ITelegramBotClient client, ChatId chatId, Update update)
    {
        var checks = new string[2];
        var language = File.ReadLines(@"..\..\..\user settings\" + update.Message?.From?.Username).First();

        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        var checkIndex = language == "en" ? 0 : 1;

        checks[checkIndex] = "✅";

        if (checkIndex == 0)
        {
            var languageInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "\ud83c\uddec\ud83c\udde7", "English Language"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "\ud83c\uddfa\ud83c\udde6", "Ukrainian Language")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Close", "Settings Close")
                }
            });
            await client.SendTextMessageAsync(chatId, "Select the language You want to use:",
                replyMarkup: languageInlineKeyboard);
        }
        else
        {
            var languageInlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(checks[0] + "\ud83c\uddec\ud83c\udde7", "English Language"),
                    InlineKeyboardButton.WithCallbackData(checks[1] + "\ud83c\uddfa\ud83c\udde6", "Ukrainian Language")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Закрити", "Settings Close")
                }
            });
            await client.SendTextMessageAsync(chatId, "Оберіть мову, яку Ви хочете використовувати:",
                replyMarkup: languageInlineKeyboard);
        }
    }
}