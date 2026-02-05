using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles callback queries related to category selection and displays available subcategories in the Telegram user
/// interface.
/// </summary>
/// <remarks>This handler processes callback data prefixed with the category indicator, retrieves the relevant
/// subcategories, and updates the user interface to prompt further navigation. It is typically used within a callback
/// query handling pipeline in a Telegram bot framework.</remarks>
/// <param name="knowledgeBaseService">The knowledge base service used to retrieve category and subcategory information.</param>
/// <param name="slugMapping">The service that manages slug mappings for categories and subcategories.</param>
/// <param name="publisher">The publisher used to send bot responses and answer callback queries.</param>
/// <param name="logger">The logger used to record informational and diagnostic messages for this handler.</param>
public class CategoryHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    IBotResponsePublisher publisher,
    ILogger<CategoryHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData.StartsWith(CallbackPrefixes.Category);

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            var categorySlug = callbackData[CallbackPrefixes.Category.Length..];
            logger.LogInformation("Category selected: {CategorySlug} (chat {ChatId})", categorySlug, context.ChatId);

            var categoryHash = slugMapping.GetOrCreateSlug(categorySlug);

            var subcategories = await knowledgeBaseService.GetSubcategoriesByCategoryAsync(categorySlug, cancellationToken);
            var subcategoryButtons = subcategories
                .Select(sc =>
                {
                    var subHash = slugMapping.GetOrCreateSlug(sc.Slug);
                    var data = CallbackPrefixes.Subcategory + subHash + CallbackPrefixes.Separator + categoryHash;
                    return (Text: sc.Title, CallbackData: data);
                })
                .ToList();

            List<IEnumerable<(string Text, string CallbackData)>> keyboardRows = subcategoryButtons
                .Chunk(2)
                .Select(chunk => (IEnumerable<(string Text, string CallbackData)>)chunk)
                .ToList();

            keyboardRows.Add([(UiTexts.BackButton, CallbackPrefixes.MainMenu)]);
            keyboardRows.Add([(UiTexts.MainMenuButton, CallbackPrefixes.MainMenu)]);

            await publisher.PublishAsync(
                context,
                new BotResponse(
                    HtmlText: "<b>Subcategories</b>\n\nChoose a subcategory:",
                    InlineKeyboard: keyboardRows,
                    EditMessage: context.MessageId.HasValue),
                cancellationToken);
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
