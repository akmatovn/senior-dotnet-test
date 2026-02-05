using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles main menu callback interactions within the Telegram bot, displaying available FAQ categories and managing
/// corresponding user interface updates.
/// </summary>
/// <remarks>This handler responds to main menu callback requests from users, presenting them with a list of FAQ
/// categories via an inline keyboard. It ensures the main menu is updated appropriately and that callback queries are
/// acknowledged to provide a responsive user experience.</remarks>
/// <param name="knowledgeBaseService">The service used to retrieve FAQ categories and related knowledge base content.</param>
/// <param name="publisher">The publisher responsible for sending bot responses and answering callback queries.</param>
/// <param name="logger">The logger instance used to record operational and diagnostic information.</param>
public class MainMenuHandler(
    IKnowledgeBaseService knowledgeBaseService,
    IBotResponsePublisher publisher,
    ILogger<MainMenuHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData == CallbackPrefixes.MainMenu;

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Main menu requested in chat {ChatId}", context.ChatId);

            var categories = await knowledgeBaseService.GetCategoriesAsync(cancellationToken);
            var categoryButtons = categories
                .Select(c => (Text: c.Title, CallbackData: CallbackPrefixes.Category + c.Slug))
                .ToList();

            var keyboard = categoryButtons
                .Chunk(2)
                .Select(chunk => (IEnumerable<(string Text, string CallbackData)>)chunk)
                .ToList();

            // add search button alongside main menu button
            keyboard.Add([(UiTexts.SearchButton, CallbackPrefixes.SearchStart)]);
            keyboard.Add([(UiTexts.MainMenuButton, CallbackPrefixes.MainMenu)]);

            await publisher.PublishAsync(
                context,
                new BotResponse(
                    HtmlText: BotMessages.MainMenuHeader,
                    InlineKeyboard: keyboard,
                    EditMessage: context.MessageId.HasValue),
                cancellationToken);
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
