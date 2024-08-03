using Telegram.Bot;
using Telegram.Bot.Types;
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
        //Some code
        await Task.CompletedTask;
    }
}