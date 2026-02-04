using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace YouDoFaqBot.BackgroundServices;

/// <summary>
/// Hosted service that starts the Telegram bot and listens for updates.
/// </summary>
/// <remarks>
/// Uses long polling to receive all update types and logs bot startup.
/// </remarks>
public class BotBackgroundService(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    ILogger<BotBackgroundService> logger) : BackgroundService
{
    private readonly ITelegramBotClient _botClient = botClient;
    private readonly IUpdateHandler _updateHandler = updateHandler;
    private readonly ILogger<BotBackgroundService> _logger = logger;

    /// <summary>
    /// Starts the Telegram bot receiving loop and keeps the service alive until cancellation.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        _logger.LogInformation("Starting Telegram bot receiving...");

        _botClient.StartReceiving(
            updateHandler: _updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        Console.WriteLine("Bot started and ready to work!");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
