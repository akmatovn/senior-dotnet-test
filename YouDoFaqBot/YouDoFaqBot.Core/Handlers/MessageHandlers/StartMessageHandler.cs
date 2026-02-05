using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers.MessageHandlers;

/// <summary>
/// Handles start command and main menu navigation messages sent by users in the chat bot.
/// </summary>
/// <param name="publisher">The response publisher used to send replies to users.</param>
/// <param name="logger">The logger used to record diagnostic and operational information.</param>
public class StartMessageHandler(
    IBotResponsePublisher publisher,
    ILogger<StartMessageHandler> logger) : IMessageHandler
{
    ///<inheritdoc/>
    public bool CanHandle(MessageContext context, string messageText)
        => messageText is "/start" or UiTexts.BrowseFaqButton;

    ///<inheritdoc/>
    public async Task HandleAsync(MessageContext context, string messageText, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling start/browse message: {Text}", messageText);

        var keyboard = new[]
        {
            new[] { (UiTexts.BrowseFaqButton, CallbackPrefixes.MainMenu) }
        };

        var response = messageText == "/start"
            ? new BotResponse(
                HtmlText: $"Welcome! Tap <b>{UiTexts.BrowseFaqButton}</b> to browse the FAQ.",
                InlineKeyboard: keyboard)
            : new BotResponse(
                HtmlText: "Opening main menu...",
                InlineKeyboard: keyboard);

        await publisher.PublishAsync(
            new CallbackContext(context.ChatId, context.MessageId, CallbackQueryId: string.Empty),
            response,
            cancellationToken);
    }
}
