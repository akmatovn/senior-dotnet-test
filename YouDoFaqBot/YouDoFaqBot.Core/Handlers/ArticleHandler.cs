using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles callback interactions related to displaying knowledge base articles within a Telegram bot interface.
/// </summary>
/// <remarks>Implements ICallbackHandler to process article-related callbacks, supporting scenarios such as direct
/// article viewing and article access from search results within the Telegram bot. This handler manages content
/// formatting, button actions for user feedback, and appropriate response publication to the Telegram client.</remarks>
/// <param name="knowledgeBaseService">The service used to retrieve articles from the knowledge base.</param>
/// <param name="slugMapping">The service responsible for resolving slug mappings to article identifiers.</param>
/// <param name="publisher">The publisher used to send responses to the Telegram client.</param>
/// <param name="logger">The logger to record operational and diagnostic information for this handler.</param>
public class ArticleHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    IBotResponsePublisher publisher,
    ILogger<ArticleHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData.StartsWith(CallbackPrefixes.ShowArticle);

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            var payload = callbackData[CallbackPrefixes.ShowArticle.Length..];
            var parts = payload.Split(CallbackPrefixes.Separator, StringSplitOptions.RemoveEmptyEntries);
            var articleHash = parts.Length >= 1 ? parts[0] : payload;
            // support:
            // - show_art:{hash}
            // - show_art:{hash}:{subHash}
            // - show_art:{hash}:search:{visible}
            int? searchVisibleCount = null;
            var openedFromSearch = false;
            if (parts.Length >= 3 && parts[1] == "search")
            {
                if (int.TryParse(parts[2], out var parsedVisible))
                {
                    searchVisibleCount = parsedVisible;
                    openedFromSearch = true;
                }
            }
            var subHash = !openedFromSearch && parts.Length >= 2 ? parts[1] : null;

            var slug = slugMapping.GetDataBySlug(articleHash) ?? articleHash;

            logger.LogInformation("Article requested: {Slug} (chat {ChatId})", slug, context.ChatId);

            var article = await knowledgeBaseService.GetBySlugAsync(slug, cancellationToken);
            if (article is null)
            {
                await publisher.AnswerCallbackAsync(context.CallbackQueryId, "Article not found", cancellationToken);
                return;
            }
            var content = article.Content;
            // Be conservative: truncate to avoid Telegram 4096 char limit after HTML encoding
            const int MaxContent = 3000;
            if (content.Length > MaxContent)
                content = content.Substring(0, MaxContent) + "... (read more on website)";

            var html = HtmlUtility.FormatArticleHtml(article.Title, content);
            string rateSuffix;
            if (openedFromSearch && searchVisibleCount.HasValue)
                rateSuffix = CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + searchVisibleCount.Value;
            else if (!string.IsNullOrEmpty(subHash))
                rateSuffix = CallbackPrefixes.Separator + subHash;
            else
                rateSuffix = string.Empty;

            var like = (UiTexts.HelpfulButton, CallbackPrefixes.Rate + "up:" + articleHash + rateSuffix);
            var dislike = (UiTexts.NotHelpfulButton, CallbackPrefixes.Rate + "down:" + articleHash + rateSuffix);
            var backTarget = openedFromSearch && searchVisibleCount.HasValue
                ? CallbackPrefixes.SearchRestore + searchVisibleCount.Value
                : (!string.IsNullOrEmpty(subHash) ? CallbackPrefixes.Subcategory + subHash : CallbackPrefixes.MainMenu);
            var back = (UiTexts.BackButton, backTarget);
            var home = (UiTexts.MainMenuButton, CallbackPrefixes.MainMenu);

            var keyboard = new[]
            {
                new[] { like, dislike },
                new[] { back },
                new[] { home }
            };

            // If we have the original message id (the search/result message), edit it to show the article
            var edit = context.MessageId.HasValue;
            await publisher.PublishAsync(
                context,
                new BotResponse(html, keyboard, EditMessage: edit),
                cancellationToken);
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
