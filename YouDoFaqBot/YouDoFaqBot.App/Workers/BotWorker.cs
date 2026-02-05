using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using YouDoFaqBot.Core.Handlers;

namespace YouDoFaqBot.App.Workers;

/// <summary>
/// Background service that manages the lifecycle and polling of the Telegram bot.
/// Handles incoming updates and delegates processing to the <see cref="UpdateDispatcher"/>.
/// </summary>
/// <param name="botClient">The Telegram bot client instance.</param>
/// <param name="dispatcher">The update dispatcher responsible for handling updates.</param>
/// <param name="logger">The logger instance for logging events and errors.</param>
public class BotWorker(
    ITelegramBotClient botClient,
    UpdateDispatcher dispatcher,
    ILogger<BotWorker> logger)
    : BackgroundService
{
    /// <summary>
    /// Executes the background service, starting the Telegram bot long polling loop.
    /// Handles updates and errors, and supports graceful shutdown via <paramref name="stoppingToken"/>.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that signals when the service should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BotWorker started");
        var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        botClient.StartReceiving(
            async (bot, update, token) =>
            {
                try
                {
                    await dispatcher.DispatchAsync(update, token);
                }
                catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
                {
                    logger.LogWarning("Ignored Telegram API error: {Message}", ex.Message);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("BotWorker cancelled");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Fatal error handling update");
                    throw;
                }
            },
            (bot, exception, token) =>
            {
                logger.LogError(exception, "Polling error");
                return Task.CompletedTask;
            },
            receiverOptions,
            cancellationToken: stoppingToken
        );
        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("BotWorker stopped");
        }
    }
}
