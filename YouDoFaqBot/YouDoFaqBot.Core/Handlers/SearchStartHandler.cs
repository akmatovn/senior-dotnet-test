using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles the initiation of search mode in the bot, prompting the user to enter a search query.
/// When the user presses the Search button, this handler sets the chat into search mode and sends a prompt message.
/// </summary>
/// <param name="searchMode">Service for managing per-chat search mode state.</param>
/// <param name="publisher">Service for publishing bot responses to Telegram.</param>
/// <param name="logger">Logger for diagnostic and operational messages.</param>
public class SearchStartHandler(
    ISearchModeService searchMode,
    IBotResponsePublisher publisher,
    ILogger<SearchStartHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData == CallbackPrefixes.SearchStart;

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Search start requested in chat {ChatId}", context.ChatId);
            // Enter search mode for this chat
            searchMode.Set(context.ChatId, true);

            var keyboard = new List<IEnumerable<(string Text, string CallbackData)>>
            {
                new[] { (UiTexts.MainMenuButton, CallbackPrefixes.MainMenu) }
            };

            await publisher.PublishAsync(
                context,
                new BotResponse(HtmlText: BotMessages.SearchPrompt, InlineKeyboard: keyboard, EditMessage: context.MessageId.HasValue),
                cancellationToken);
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
