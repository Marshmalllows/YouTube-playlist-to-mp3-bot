using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace YouTube_playlist_to_mp3_bot;

public class Settings
{
    public static async void BitrateSettings(ITelegramBotClient client, ChatId chatId, int messageId, string userBitrate)
    {
        var checks = new string[5];
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        var checkIndex = int.Parse(userBitrate) / 64 - 1;
        checks[checkIndex] = "✅";
        
        var bitrateInlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(checks[0] + "64 kbps", "Change Bitrate 64"),
                InlineKeyboardButton.WithCallbackData(checks[1] + "128 kbps", "Change Bitrate 128"),
                InlineKeyboardButton.WithCallbackData(checks[2] + "192 kbps", "Change Bitrate 192")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(checks[3] + "256 kbps", "Change Bitrate 256"),
                InlineKeyboardButton.WithCallbackData(checks[4] + "320 kbps", "Change Bitrate 320")
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("Back", "Settings Back")
            }
        });
        await client.EditMessageTextAsync(chatId, messageId, "Select bitrate option you want to use:",
            replyMarkup: bitrateInlineKeyboard);
    }
    
    public static async void SampleRateSettings(ITelegramBotClient client, ChatId chatId, int messageId,
        string userSampleRate)
    {
        var checks = new string[3];
        int checkIndex;
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        if (userSampleRate == "22050")
        {
            checkIndex = 0;
        }
        else if (userSampleRate == "44100")
        {
            checkIndex = 1;
        }
        else
        {
            checkIndex = 2;
        }
        
        checks[checkIndex] = "✅";
        
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
    
    public static async void ChannelsSettings(ITelegramBotClient client, ChatId chatId, int messageId, string userChannel)
    {
        var checks = new string[2];
        int checkIndex;
        
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i] = "";
        }

        if (userChannel == "1")
        {
            checkIndex = 0;
        }
        else
        {
            checkIndex = 1;
        }
        
        checks[checkIndex] = "✅";
        
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
}