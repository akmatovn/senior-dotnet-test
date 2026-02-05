using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles Telegram callback queries related to subcategory selection and displays associated articles to the user.
/// </summary>
/// <remarks>This handler is intended for use within Telegram bots that support hierarchical navigation through
/// categories and subcategories. It presents articles in the selected subcategory and provides navigation options for
/// returning to the main menu or previous category.</remarks>
/// <param name="knowledgeBaseService">The service used to retrieve articles from the knowledge base by subcategory.</param>
/// <param name="slugMapping">The service used to map between slugs and their corresponding data representations.</param>
/// <param name="publisher">The publisher responsible for sending bot responses and answering callback queries.</param>
/// <param name="logger">The logger used to record informational and diagnostic messages for this handler.</param>
public class SubcategoryHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    IBotResponsePublisher publisher,
    ILogger<SubcategoryHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData.StartsWith(CallbackPrefixes.Subcategory);

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            var payload = callbackData[CallbackPrefixes.Subcategory.Length..];
            var parts = payload.Split(CallbackPrefixes.Separator, StringSplitOptions.RemoveEmptyEntries);
            var subHash = parts.Length >= 1 ? parts[0] : payload;
            var catHash = parts.Length >= 2 ? parts[1] : null;

            var subSlug = slugMapping.GetDataBySlug(subHash) ?? subHash;
            logger.LogInformation("Subcategory selected: {SubcategorySlug} (chat {ChatId})", subSlug, context.ChatId);

            var articles = await knowledgeBaseService.GetArticlesBySubcategoryAsync(subSlug, cancellationToken);
            var articleButtons = articles
                .Select(a =>
                {
                    var articleHash = slugMapping.GetOrCreateSlug(a.Slug);
                    var data = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + subHash;
                    return (Text: a.Title, CallbackData: data);
                })
                .ToList();

            var keyboardRows = articleButtons
                .Chunk(2)
                .Select(chunk => (IEnumerable<(string Text, string CallbackData)>)chunk)
                .ToList();

            var backTarget = catHash is { Length: > 0 }
                ? CallbackPrefixes.Category + (slugMapping.GetDataBySlug(catHash) ?? catHash)
                : CallbackPrefixes.MainMenu;
            keyboardRows.Add([(UiTexts.BackButton, backTarget)]);
            keyboardRows.Add([(UiTexts.MainMenuButton, CallbackPrefixes.MainMenu)]);

            await publisher.PublishAsync(
                context,
                new BotResponse(
                    HtmlText: "<b>Articles</b>\n\nChoose an article:",
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
