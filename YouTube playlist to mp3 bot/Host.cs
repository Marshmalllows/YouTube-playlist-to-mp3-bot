using Telegram.Bot;
using Telegram.Bot.Types;

namespace YouTube_playlist_to_mp3_bot;

public class Host(string token)
{
    private TelegramBotClient BotClient { get; } = new (token);

    public Action<ITelegramBotClient, Update>? OnMessage;

    public void Start()
    {
        BotClient.StartReceiving(UpdateHandler, ErrorHandler);
        Console.WriteLine("Bot is running...");
    }

    private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        OnMessage?.Invoke(client, update);
        await Task.CompletedTask;
    }
    
    private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken cancellationtoken)
    {
        Console.WriteLine("Error: " + exception.Message);
        await Task.CompletedTask;
    }
}